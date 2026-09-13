using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：用卡的「數值計算核心」，由 A_Good_Ink 使用 AI 生成的重構版本。
    ///
    /// 取代舊 BattleAction 的三份暫存副本模型（temp_UserHp / temp_TargetHp / temp_*StatusEffects...）。
    /// 這裡直接吃兩個 <see cref="BattleUnit"/>、算完把結果直接寫回單位，沒有任何暫存同步、沒有事件乒乓。
    ///
    /// 純運算部分「完全重用」已驗證的 <see cref="CardCalculation"/>（不重寫數學）；
    /// 本類別只負責「編排」：算基礎值 → 套受擊修正 → 寫回 HP → 施加狀態。
    ///
    /// 對照舊碼：
    ///   ApplyCard        ← BattleAction.UseCard + OnUseCard + AllUnitCalculate
    ///   受擊修正管線      ← BattleAction.Calculation.cs 的 GetModifiedValue / CalculateValue
    ///   AddEffect        ← BattleAction.AddEffect / ApplyEffectToTarget
    /// </summary>
    public class BattleCombat
    {
        private readonly CardCalculation calc = new CardCalculation();
        private readonly StatusValueSetter valueSetter = new StatusValueSetter();

        /// <summary>用一張卡的結果，供橋接層更新骰子視覺（骰子功能卡才需要）。</summary>
        public struct CardApplyResult
        {
            public CardType CardType;
            public bool AffectedDice;     // 是否為骰子功能卡（拆解/複製/顛倒/重骰）
            public int OriUserDiceCount;  // 用卡前的骰子顆數
            public int NewUserDiceValue;  // 運算後的骰數
            public int NewUserDiceCount;  // 運算後的骰子顆數
        }

        /// <summary>
        /// 使用一張卡：user 為行動者、target 為對象、diceValue 為投放到卡上的骰數（累積值）。
        /// 直接把傷害/治療寫回雙方 HP、把狀態加到對應單位，並回傳骰子功能卡所需的視覺更新資訊。
        /// </summary>
        public CardApplyResult ApplyCard(BattleUnit user, BattleUnit target, CardType cardType, int diceValue)
        {
            var result = new CardApplyResult { CardType = cardType };
            if (user == null || target == null) return result;

            BattleCardInfo info = BattleCard.SendCardInfo(cardType);

            int userDiceCount = user.DiceCount;
            int targetDiceCount = target.DiceCount;
            result.OriUserDiceCount = userDiceCount;

            int baseToRival = 0;   // 對對象（多為負＝傷害）
            int baseToUser = 0;    // 對自己（治療為正 / 反傷為負）
            int userDiceValue = diceValue;

            // 先施加卡片本身要附加的狀態（對照舊 UseCard 的 AddEffect 段）
            if (info.IsAddEffectStatus)
                AddEffect(user, target, info.EffectType);

            // 依卡片種類算基礎值（完全重用 CardCalculation）
            if (info.IsUsedToAttack && info.IsFunctional)
            {
                (baseToRival, baseToUser) = calc.BothTypeCard(cardType, diceValue, 0, 0, 0, userDiceCount, targetDiceCount);
            }
            else if (info.IsUsedToAttack && !info.IsFunctional)
            {
                (baseToRival, baseToUser) = calc.AttackedCard(cardType, diceValue, 0, 0);
            }
            else if (!info.IsUsedToAttack && info.IsFunctional)
            {
                switch (info.FunctionalType)
                {
                    case BattleFunctionalCardType.Heal:
                        (baseToRival, baseToUser, userDiceValue, userDiceCount, targetDiceCount) =
                            calc.HealFunctionalCard(cardType, diceValue, userDiceCount, targetDiceCount);
                        break;

                    case BattleFunctionalCardType.EffectStatus:
                        {
                            var userStatus = user.StatusEffects;
                            var targetStatus = target.StatusEffects;
                            (userStatus, targetStatus) =
                                calc.EffectStatusFunctionalCard(cardType, diceValue, userDiceCount, targetDiceCount, userStatus, targetStatus);
                            user.SetStatusEffects(userStatus);
                            target.SetStatusEffects(targetStatus);
                            break;
                        }

                    case BattleFunctionalCardType.Dice:
                        (baseToRival, baseToUser, userDiceValue, userDiceCount, targetDiceCount) =
                            calc.DiceFunctionalCard(cardType, diceValue, userDiceCount, targetDiceCount);
                        result.AffectedDice = true;
                        break;

                    case BattleFunctionalCardType.ValueCalculation:
                        (baseToRival, baseToUser, userDiceValue, userDiceCount, targetDiceCount) =
                            calc.ValueCalculationFunctionalCard(cardType, diceValue, userDiceCount, targetDiceCount);
                        break;
                }
            }

            // 骰子功能卡：更新骰子顆數與回傳視覺資訊（實際場上骰子由橋接層更新）
            if (result.AffectedDice)
            {
                result.NewUserDiceValue = userDiceValue;
                result.NewUserDiceCount = userDiceCount;
                user.SetDiceCount(userDiceCount);
            }

            // 套受擊修正後寫回 HP（骰子功能卡本身不造成 HP 變化，對照舊 OnUseCard 的 FunctionalType != Dice 判斷）
            int finalToRival = 0, finalToUser = 0;
            if (info.FunctionalType != BattleFunctionalCardType.Dice)
            {
                finalToRival = ApplyReceivedModifiers(baseToRival, target.StatusEffects);
                finalToUser = ApplyReceivedModifiers(baseToUser, user.StatusEffects);

                if (finalToRival != 0)
                {
                    target.SetCurrentHp(target.CurrentHp + finalToRival);
                    target.RequestHpPopup(finalToRival); // 彈出傷害/治療數值
                }
                if (finalToUser != 0)
                {
                    user.SetCurrentHp(user.CurrentHp + finalToUser);
                    user.RequestHpPopup(finalToUser);
                }
            }

            // 特效（對照舊 OnUseCard 的 DisplayEffectOnUseCard 決策）
            PlayCardVfx(user, target, info, finalToRival, finalToUser);

            return result;
        }

        #region 特效觸發（對照 BattleAction.Presentation.cs）
        /// <summary>依卡片種類/作用對象決定要在誰身上播什麼特效（傷害/治療/施加狀態）。</summary>
        private void PlayCardVfx(BattleUnit user, BattleUnit target, BattleCardInfo info, int finalToRival, int finalToUser)
        {
            CardType cardType = info.CardType;

            if (info.FunctionalType != BattleFunctionalCardType.Dice)
            {
                if (info.IsUsedToAttack)
                {
                    RequestVfx(target, cardType, info.EffectType, finalToRival);
                    if (cardType == CardType.HeavyAttack || cardType == CardType.LazerGun || cardType == CardType.Oath)
                        RequestVfx(user, cardType, info.EffectType, finalToUser); // 反傷/自身也吃到的卡
                }
                if (info.IsFunctional)
                {
                    BattleStatusEffect eff = new BattleStatusEffect(info.EffectType);
                    if (eff.GetIsRivalAffecting()) RequestVfx(target, cardType, info.EffectType, finalToRival);
                    if (eff.GetIsSelfAffecting()) RequestVfx(user, cardType, info.EffectType, finalToUser);
                    if (info.EffectType == BattleStatusEffectType.None) RequestVfx(user, cardType, info.EffectType, finalToUser); // 例如純治療
                }
            }
            else
            {
                if (info.IsAddEffectStatus) RequestVfx(target, cardType, info.EffectType, finalToRival); // 骰子功能卡附帶狀態
            }
        }

        /// <summary>value≠0 視為傷害(&lt;0)/治療；value=0 視為施加狀態。對照舊 DisplaySE 的判斷。</summary>
        private static void RequestVfx(BattleUnit unit, CardType cardType, BattleStatusEffectType effectType, int value)
        {
            if (unit == null) return;
            bool isDamage = false, isApplyState = false;
            if (value != 0) isDamage = value < 0; else isApplyState = true;
            unit.RequestVfx(cardType, isDamage, isApplyState, effectType);
        }
        #endregion

        #region 受擊修正管線（對照 BattleAction.Calculation.cs）
        /// <summary>
        /// 依「接收方」身上的狀態，對一個基礎值做加減乘除修正。
        /// 目前實際會用到的：HolyProtect（受擊傷害/2）、Oath（受擊傷害x2）。
        /// baseValue &lt; 0 視為傷害、&gt;= 0 視為治療，分別查對應的影響類型。
        /// </summary>
        private int ApplyReceivedModifiers(int baseValue, List<BattleStatusEffect> receiverStatus)
        {
            if (receiverStatus == null || receiverStatus.Count == 0) return baseValue;

            bool isDamage = baseValue < 0;
            BattleValueAffectType affectType = isDamage
                ? BattleValueAffectType.AFFECT_RECEIVED_DAMAGE
                : BattleValueAffectType.AFFECT_RECEIVED_HEAL;
            int typeIndex = (int)affectType;

            // 找出「會影響計算、且作用於這個影響類型」的狀態
            List<BattleStatusEffect> affecting = receiverStatus.FindAll(e =>
                e != null && e.GetAffectedCalculation() &&
                e.GetAffectValueTypeList() != null &&
                typeIndex < e.GetAffectValueTypeList().Count &&
                e.GetAffectValueTypeList()[typeIndex]);

            if (affecting.Count == 0) return baseValue;

            int value = baseValue;
            // 先乘除後加減（與舊版 CalculateValue 相同順序）
            value = FoldOperation(value, affecting, BattleValueOperation.Multiply);
            value = FoldOperation(value, affecting, BattleValueOperation.Divide);
            value = FoldOperation(value, affecting, BattleValueOperation.Add);
            value = FoldOperation(value, affecting, BattleValueOperation.Subtract);
            return value;
        }

        private int FoldOperation(int value, List<BattleStatusEffect> effects, BattleValueOperation op)
        {
            foreach (var e in effects)
            {
                if (e.GetOperationType() != op) continue;
                int num = e.GetEffectValue();
                switch (op)
                {
                    case BattleValueOperation.Multiply: value *= num; break;
                    case BattleValueOperation.Divide:
                        if (num != 0) value = Mathf.RoundToInt(value * (1.0f / num));
                        break;
                    case BattleValueOperation.Add: value += num; break;
                    case BattleValueOperation.Subtract: value += num; break; // 沿用舊版：Subtract 也是 +=（數值本身帶負號）
                }
            }
            return value;
        }
        #endregion

        #region 施加狀態（對照 BattleAction.AddEffect / ApplyEffectToTarget）
        /// <summary>依卡片的狀態類型，把狀態加到（或疊加到）作用對象身上。</summary>
        private void AddEffect(BattleUnit user, BattleUnit target, BattleStatusEffectType effectType)
        {
            if (effectType == BattleStatusEffectType.None) return;

            // 用狀態自己的 SO/預設資訊判斷作用對象
            BattleStatusEffect template = new BattleStatusEffect(effectType);
            bool self = template.GetIsSelfAffecting();
            bool rival = template.GetIsRivalAffecting();

            if (self)
            {
                StackOrAdd(user.StatusEffects, effectType);
                user.RequestStatusPopup(effectType); // 彈出附加狀態（文字＋icon）
            }
            if (rival)
            {
                StackOrAdd(target.StatusEffects, effectType);
                target.RequestStatusPopup(effectType);
            }

            user.NotifyChanged();
            target.NotifyChanged();
        }

        private void StackOrAdd(List<BattleStatusEffect> statusList, BattleStatusEffectType effectType)
        {
            if (statusList == null) return;
            BattleStatusEffect existing = statusList.Find(e => e != null && e.GetEffectType() == effectType);
            if (existing != null)
            {
                // 沿用舊版疊加：回合數與施加次數各自加上自身現值（等於加倍）
                existing.SetLastTurn(existing.GetLastTurn() + existing.GetLastTurn());
                existing.SetDotAddTimes(existing.GetDotAddTimes() + existing.GetDotAddTimes());
            }
            else
            {
                statusList.Add(new BattleStatusEffect(effectType));
            }
        }
        #endregion

        #region 回合時機狀態結算（對照 BattleAction.OnTurnStart / OnTurnEnd + Effects 引擎）
        /// <summary>行動者回合開始時的狀態結算結果，供 Controller 決定是否跳過回合、是否指定骰數。</summary>
        public struct TurnStartResult
        {
            public bool SkipTurn;          // 暈眩 → 這回合跳過
            public bool AssignFixedDice;   // 星辰威嚇 → 這回合骰數被指定
            public int FixedDiceValue;
            public int FixedDiceCount;
        }

        /// <summary>
        /// 行動者(current)回合「開始」時結算其身上、在回合開始觸發的狀態：
        /// 中毒扣血、火燒減骰、暈眩跳過、星辰指定骰；接著把「回合開始移除」的狀態減層並移除。
        ///
        /// 註：為求清晰，這個乾淨版只結算「當前行動者」自己的狀態（舊版在每個時機同時掃雙方，
        /// 語意較繞且有重複觸發疑慮）。中毒/火燒/暈眩/星辰的預期行為一致；請在能 Play 後驗證數值。
        /// </summary>
        public TurnStartResult ResolveTurnStart(BattleUnit current, BattleUnit opponent)
        {
            var r = new TurnStartResult();
            if (current == null || current.StatusEffects == null) return r;

            int dice = current.DiceCount;
            bool skip = false;

            foreach (var e in new List<BattleStatusEffect>(current.StatusEffects))
            {
                if (e == null || !ActivatesAt(e, BattleStatusEffectWorkTiming.ACTIVATES_AT_TURN_START)) continue;

                switch (e.GetEffectType())
                {
                    case BattleStatusEffectType.Poisoned:
                        {
                            int dmg = calc.Poisoned(e.GetEffectValue(), e.GetLastTurn(), e.GetDotAddTimes()); // 負值
                            current.SetCurrentHp(current.CurrentHp + dmg);
                            RequestVfx(current, CardType.Undefined, BattleStatusEffectType.Poisoned, dmg); // 中毒扣血特效
                            current.RequestHpPopup(dmg); // 中毒扣血也彈數值
                            e.SetHasTakenEffect(true);
                            break;
                        }
                    case BattleStatusEffectType.Burnt:
                        dice = calc.Burnt(dice, BattleStatusEffect.BurntReduceDiceCountValue); // -1 顆
                        e.SetHasTakenEffect(true);
                        break;
                    case BattleStatusEffectType.Dizziness:
                        skip = calc.Dizziness(e.GetLastTurn(), e.GetDotAddTimes(), skip, e.GetHasTakenEffect());
                        e.SetHasTakenEffect(true);
                        break;
                    case BattleStatusEffectType.StarThreaten:
                        (r.AssignFixedDice, r.FixedDiceValue, r.FixedDiceCount) = calc.StarThreaten(e.GetLastTurn(), e.GetHasTakenEffect());
                        e.SetHasTakenEffect(true);
                        break;
                }
            }

            current.SetDiceCount(dice);
            ReduceAndRemoveAtTiming(current, BattleStatusEffectWorkTiming.ACTIVATES_AT_TURN_START); // 中毒在回合開始減層移除
            r.SkipTurn = skip;
            current.NotifyChanged();
            return r;
        }

        /// <summary>
        /// 行動者(current)回合「結束」時，把「回合結束移除」的狀態減層並移除
        /// （火燒移除時會把減掉的骰子加回、暈眩/星辰/聖光/毒誓於此到期消失）。
        /// </summary>
        public void ResolveTurnEnd(BattleUnit current, BattleUnit opponent)
        {
            if (current == null || current.StatusEffects == null) return;
            ReduceAndRemoveAtTiming(current, BattleStatusEffectWorkTiming.ACTIVATES_AT_TURN_END);
            current.NotifyChanged();
        }

        #region 內部：減層與移除
        private void ReduceAndRemoveAtTiming(BattleUnit unit, BattleStatusEffectWorkTiming timing)
        {
            var list = unit.StatusEffects;
            if (list == null) return;

            int dice = unit.DiceCount;
            bool skip = false;

            foreach (var e in new List<BattleStatusEffect>(list))
            {
                if (e == null || !RemovesAt(e, timing)) continue;

                e.SetLastTurn(valueSetter.ReduceLastTimes(e.GetLastTurn())); // 減一回合（不低於 0）

                if (e.GetLastTurn() <= 0)
                {
                    // 狀態到期時的還原副作用（火燒 +1 骰、暈眩解除等），重用 CardCalculation
                    (dice, skip) = calc.RemoveEffect(e, ref dice, ref skip, timing);
                }
            }

            unit.SetDiceCount(dice);

            // 移除規則沿用舊版 RemoveEffectsLastTurn：
            list.RemoveAll(e => e.GetLastTurn() <= 0 && e.GetIsAbleToRemoveAfterEffect() == false && e.GetHasTakenEffect() == true);
            list.RemoveAll(e => e.GetLastTurn() <= 0 && e.GetIsAbleToRemoveAfterEffect() == true);
        }

        private static bool ActivatesAt(BattleStatusEffect e, BattleStatusEffectWorkTiming timing)
        {
            var l = e.GetActivatesTimingList();
            int i = (int)timing;
            return l != null && i < l.Count && l[i];
        }

        private static bool RemovesAt(BattleStatusEffect e, BattleStatusEffectWorkTiming timing)
        {
            var l = e.GetRemovesTimingList();
            int i = (int)timing;
            return l != null && i < l.Count && l[i];
        }
        #endregion
        #endregion
    }
}
