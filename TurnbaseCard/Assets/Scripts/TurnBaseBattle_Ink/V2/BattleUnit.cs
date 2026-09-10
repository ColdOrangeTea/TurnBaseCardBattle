using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：一個戰鬥單位的「單一真實資料來源」(single source of truth)。
    ///
    /// 取代舊架構中散在三處、靠事件不斷同步的重複狀態：
    ///   - TurnBaseBattleUnitDisplayData 的欄位
    ///   - TurnBaseBattleManager 的 temp_CardUser / temp_Target
    ///   - BattleAction 的 temp_UserHp / temp_TargetHp / temp_*StatusEffects ...
    ///
    /// runtime 期間，HP／骰數／狀態只存在這裡。
    /// BattleController 直接讀寫本類別；View 只讀、靠 <see cref="Changed"/> 事件刷新，
    /// 因此不再需要 BattleTurnBaseEvent 那套「Manager↔Unit 乒乓傳資料」的機制。
    /// </summary>
    public class BattleUnit : MonoBehaviour
    {
        /// <summary>資料一有變動就觸發，讓 View（或任何顯示端）重新刷新。純顯示通知，不承載資料。</summary>
        public event Action Changed;

        /// <summary>
        /// 請求在此單位身上播放特效（傷害/治療/施加狀態）。由 BattleUnitView 接收後轉呼叫 BattleUnitProfile.PlaySE。
        /// 參數：卡片種類、是否傷害、是否為施加狀態、狀態類型。BattleUnit 本身不碰顯示，只轉發需求。
        /// </summary>
        public event Action<CardType, bool, bool, BattleStatusEffectType> VfxRequested;

        /// <summary>由 BattleCombat 呼叫，發出「在此單位播特效」的需求。</summary>
        public void RequestVfx(CardType cardType, bool isDamage, bool isApplyState, BattleStatusEffectType effectType)
            => VfxRequested?.Invoke(cardType, isDamage, isApplyState, effectType);

        [Header("身分")]
        [SerializeField] private bool isEnemy;
        [SerializeField] private string nameEn = "";
        [SerializeField] private string nameTw = "";
        [SerializeField] private TurnBaseBattleOrderType order = TurnBaseBattleOrderType.Undefined;
        [Tooltip("敵人單位的型別，供敵人 AI 用骰數決定出牌；玩家單位維持 Undefined。")]
        [SerializeField] private EnemyType enemyType = EnemyType.Undefined_Temp_ThisIsTypeEndNumber;

        [Header("生命值")]
        [SerializeField] private int maxHp;
        [SerializeField] private int currentHp;

        [Header("骰子")]
        [Tooltip("每回合開始會回復到這個顆數")]
        [SerializeField] private int maxDiceCount = 2;
        [SerializeField] private int diceCount = 2;

        [Header("狀態效果")]
        [SerializeField] private List<BattleStatusEffect> statusEffects = new List<BattleStatusEffect>();

        #region 唯讀屬性
        public bool IsEnemy => isEnemy;
        public string NameEn => nameEn;
        public string NameTw => string.IsNullOrEmpty(nameTw) ? nameEn : nameTw;
        public TurnBaseBattleOrderType Order { get => order; set => order = value; }
        public EnemyType EnemyType => enemyType;

        public int MaxHp => maxHp;
        public int CurrentHp => currentHp;
        public bool IsDead => currentHp <= 0;

        public int MaxDiceCount => maxDiceCount;
        public int DiceCount => diceCount;

        /// <summary>目前狀態效果清單。直接回傳內部參考，修改後請呼叫 <see cref="NotifyChanged"/>。</summary>
        public List<BattleStatusEffect> StatusEffects => statusEffects;
        #endregion

        #region 資料載入
        /// <summary>戰鬥開始時，用一份 TurnBaseBattleUnitData 初始化本單位的 runtime 狀態。</summary>
        public void LoadFrom(TurnBaseBattleUnitData data, bool asEnemy)
        {
            isEnemy = asEnemy;
            nameEn = data.unitName;
            order = data.GetTurnOrder();

            // 敵人單位額外記下型別，供敵人 AI（TurnBaseEnemyBehavior）用骰數決定出牌
            if (data is TurnBaseBattleEnemyData enemyData)
                enemyType = enemyData.BaseEnemyType;

            maxHp = data.GetOriginMaxHp();
            currentHp = data.GetCurHp() > 0 ? data.GetCurHp() : maxHp;

            maxDiceCount = data.GetOriginMaxCountOfDice();
            diceCount = data.GetCountOfDice() > 0 ? data.GetCountOfDice() : maxDiceCount;

            statusEffects = new List<BattleStatusEffect>();
            NotifyChanged();
        }

        /// <summary>設定顯示用的中文名（英文名由 LoadFrom 帶入，中文名可能來自別處查表）。</summary>
        public void SetDisplayNames(string english, string traditionalChinese)
        {
            if (!string.IsNullOrEmpty(english)) nameEn = english;
            if (!string.IsNullOrEmpty(traditionalChinese)) nameTw = traditionalChinese;
            NotifyChanged();
        }
        #endregion

        #region 生命值
        /// <summary>受到傷害，HP 夾在 0..maxHp。</summary>
        public void TakeDamage(int amount)
        {
            if (amount <= 0) return;
            SetCurrentHp(currentHp - amount);
        }

        /// <summary>治療，HP 夾在 0..maxHp。</summary>
        public void Heal(int amount)
        {
            if (amount <= 0) return;
            SetCurrentHp(currentHp + amount);
        }

        public void SetCurrentHp(int value)
        {
            currentHp = Mathf.Clamp(value, 0, maxHp);
            NotifyChanged();
        }

        public void SetMaxHp(int value)
        {
            maxHp = Mathf.Max(0, value);
            currentHp = Mathf.Clamp(currentHp, 0, maxHp);
            NotifyChanged();
        }
        #endregion

        #region 骰子
        /// <summary>把目前骰數回復到 maxDiceCount（每回合開始呼叫）。</summary>
        public void ResetDiceCount()
        {
            diceCount = maxDiceCount;
            NotifyChanged();
        }

        public void SetDiceCount(int value)
        {
            diceCount = Mathf.Max(0, value);
            NotifyChanged();
        }

        public void SetMaxDiceCount(int value)
        {
            maxDiceCount = Mathf.Max(0, value);
            NotifyChanged();
        }

        /// <summary>消耗一顆骰子（用卡時），不會低於 0。</summary>
        public void ConsumeDice(int count = 1)
        {
            SetDiceCount(diceCount - count);
        }
        #endregion

        #region 狀態效果
        public void SetStatusEffects(List<BattleStatusEffect> effects)
        {
            statusEffects = effects ?? new List<BattleStatusEffect>();
            NotifyChanged();
        }

        public void ClearStatusEffects()
        {
            statusEffects.Clear();
            NotifyChanged();
        }
        #endregion

        /// <summary>手動通知顯示端刷新（外部直接改動 StatusEffects 清單內容後呼叫）。</summary>
        public void NotifyChanged() => Changed?.Invoke();
    }
}
