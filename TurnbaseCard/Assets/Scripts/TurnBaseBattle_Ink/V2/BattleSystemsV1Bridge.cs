using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：把「沿用的舊子系統」接進乾淨 Controller 的具體橋接，由 A_Good_Ink 使用 AI 生成的重構版本。
    ///
    /// 走「修改舊子系統改吃直接引用」路線：骰子(S001)、骰子池(DicePoolManager)、抽牌(S002)、
    /// 卡片投放計算(S005) 都在 Inspector 直接拖引用（不再經 TurnBaseBattleUI / manager），
    /// 用卡的數值計算則交給 V2 的 <see cref="BattleCombat"/>（吃 BattleUnit）。
    ///
    /// 用卡輸入路徑（沿用舊的拖放與事件）：
    ///   骰子拖到卡 → S004_DiceMove → S005 DetectCard → DiceEvent.OnInfoOfCardAndDicesSent
    ///   → 本橋接 OnPlayerUseCard → BattleCombat.ApplyCard(寫回 BattleUnit)。
    /// </summary>
    public class BattleSystemsV1Bridge : BattleSystemsBridge
    {
        [Header("沿用的舊子系統（直接拖引用）")]
        [SerializeField] private DicePoolManager dicePoolManager;
        [SerializeField] private S001_DiceSystem diceSystem;
        [SerializeField] private S002_DrawCardsSystem drawCardSystem;
        [SerializeField] private S005_NumericalCalculation numericalCalculation;

        private BattleController controller;
        private readonly BattleCombat combat = new BattleCombat();
        private bool subscribed;
        private bool pendingSkip;

        public override void OnBattleStart(BattleController c)
        {
            controller = c;

            // 初始化（直接引用版，不經 TurnBaseBattleUI）
            if (dicePoolManager != null) dicePoolManager.InitDirect();
            if (numericalCalculation != null) numericalCalculation.InitDirect();
            if (drawCardSystem != null) drawCardSystem.InitFromTurnBaseBattleUI(); // 內容為空，僅為對齊舊流程
            if (diceSystem != null)
            {
                diceSystem.InitDirect();
                diceSystem.OnDiceConsumed = OnDiceConsumed; // 場上骰子被用掉 → 扣 BattleUnit 骰數
            }

            Subscribe();
        }

        private void OnEnable() => Subscribe();
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();

        private void Subscribe()
        {
            if (subscribed) return;
            DiceEvent.OnInfoOfCardAndDicesSent += OnPlayerUseCard;
            subscribed = true;
        }

        private void Unsubscribe()
        {
            if (!subscribed) return;
            DiceEvent.OnInfoOfCardAndDicesSent -= OnPlayerUseCard;
            subscribed = false;
        }

        #region 回合準備 / 結束
        public override void PrepareTurn(BattleUnit current, BattleUnit opponent)
        {
            // 回合開始的狀態結算（中毒扣血、火燒減骰、暈眩跳過、星辰指定骰）
            BattleCombat.TurnStartResult r = combat.ResolveTurnStart(current, opponent);
            pendingSkip = r.SkipTurn;

            // 擲骰（顆數已被火燒等結算影響）
            if (diceSystem != null)
            {
                diceSystem.CurTurnDiceCount = current.DiceCount;
                diceSystem.RollTheDice();
                if (r.AssignFixedDice)
                    diceSystem.OnRollDice(true, r.FixedDiceValue, r.FixedDiceCount); // 星辰威嚇：指定骰數
            }

            // 抽牌 + 重置卡片投放區
            if (drawCardSystem != null) drawCardSystem.DrawCards();
            if (numericalCalculation != null) numericalCalculation.ResetAreaDiceValue();
        }

        public override bool ConsumeSkipFlag()
        {
            bool skip = pendingSkip;
            pendingSkip = false;
            return skip;
        }

        public override void EndTurn(BattleUnit current, BattleUnit opponent)
        {
            combat.ResolveTurnEnd(current, opponent);
            if (diceSystem != null) diceSystem.RemoveUsableDices(); // 清除場上骰子
        }
        #endregion

        #region 敵人 AI
        public override void RunEnemyTurn(BattleUnit enemy, BattleUnit target)
        {
            if (enemy == null || target == null) return;

            int diceCount = enemy.DiceCount;
            for (int i = 0; i < diceCount; i++)
            {
                int value = Random.Range(1, 7);
                CardType cardType = TurnBaseEnemyBehavior.GetEnemyAction(enemy.EnemyType, value);
                if (cardType == CardType.Undefined) continue;

                combat.ApplyCard(enemy, target, cardType, value);
                if (target.IsDead || enemy.IsDead) break; // 有人陣亡就停手
            }
        }
        #endregion

        #region 玩家用卡（沿用舊的拖放輸入）
        private void OnPlayerUseCard(CardData card, int value)
        {
            if (controller == null || card == null) return;

            BattleUnit user = controller.Current;
            BattleUnit target = controller.Opponent;

            BattleCombat.CardApplyResult result = combat.ApplyCard(user, target, card.cardType, value);

            // 骰子功能卡（拆解/複製/顛倒/重骰）：沿用舊 S001 的骰子視覺重建
            if (result.AffectedDice && diceSystem != null)
            {
                ValueForOperation v = new ValueForOperation
                {
                    UserDiceValue = result.NewUserDiceValue,
                    UserDiceCount = result.NewUserDiceCount
                };
                diceSystem.DiceFunction(card.cardType, v, result.OriUserDiceCount);
            }
        }

        private void OnDiceConsumed(int count)
        {
            // 場上一顆骰子被放到卡上用掉 → 扣目前行動者的骰數（取代舊版改 BattleAction.temp_UserDiceCount）
            if (controller != null && controller.Current != null)
                controller.Current.ConsumeDice(count);
        }
        #endregion
    }
}
