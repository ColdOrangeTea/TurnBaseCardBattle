using System;
using System.Collections;
using UnityEngine;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：整場戰鬥的「大腦」，由 A_Good_Ink 使用 AI 生成的重構版本。
    ///
    /// 取代舊架構的 TurnBaseBattleManager（約 730 行）。核心差異：
    ///   1. 直接 [SerializeField] 持有 View / 兩個 BattleUnit / 子系統橋接 —— 不再用 Tag / GetChild 尋找。
    ///   2. runtime 狀態只有一份（在 BattleUnit）—— 不再有 temp_CardUser / temp_Target 等副本，
    ///      也不再靠 BattleTurnBaseEvent 在 Manager↔Unit↔Action 之間乒乓傳資料。
    ///   3. 回合流程是單純的狀態機：開戰 → (每回合) 準備→行動→結束→換手 → 勝負結算。
    ///
    /// 對外只保留一個通知事件 <see cref="BattleFinished"/> 供地圖/存檔等外部系統使用。
    /// </summary>
    public class BattleController : MonoBehaviour
    {
        [Header("顯示層")]
        [SerializeField] private BattleView view;

        [Header("資料單位（單一真實資料來源）")]
        [SerializeField] private BattleUnit playerUnit; // 對應舊 Player1
        [SerializeField] private BattleUnit enemyUnit;  // 對應舊 Player2

        [Header("子系統橋接（骰子 / 抽牌 / 用卡計算 / 敵人 AI）")]
        [Tooltip("Chunk 4 會提供具體實作；未指派時回合流程仍可跑，只是沒有骰子/卡片表現。")]
        [SerializeField] private BattleSystemsBridge systems;

        [Header("設定")]
        [SerializeField] private bool isPlayerFirst = true;
        [Tooltip("敵人行動前的思考停頓秒數")]
        [SerializeField] private float enemyThinkSeconds = 1.5f;

        /// <summary>戰鬥結束通知：參數為「玩家是否獲勝」。供地圖 / 存檔等外部系統訂閱。</summary>
        public event Action<bool> BattleFinished;

        #region 回合狀態
        public int RoundCount { get; private set; }
        public int TurnCount { get; private set; }

        private BattleUnit current;   // 這回合行動者
        private BattleUnit opponent;  // 這回合對象
        private bool isBattleOver;
        private bool isStarted;
        private Coroutine turnRoutine;
        private Coroutine enemyRoutine;

        public BattleUnit Current => current;
        public BattleUnit Opponent => opponent;
        public bool IsPlayerTurn => current == playerUnit;
        public BattleUnit PlayerUnit => playerUnit;
        public BattleUnit EnemyUnit => enemyUnit;
        #endregion

        #region 開戰
        /// <summary>
        /// 用一份戰鬥設定開戰（沿用舊的 SetBattleSetting 結構，方便與現有 SetUp 流程接軌）。
        /// TB_BattleUnits[0]=玩家、[1]=敵人。
        /// </summary>
        public void StartBattle(SetBattleSetting setting)
        {
            if (!ValidateRefs()) return;

            view.ShowSettlement(false, false); // 先確保結算面板隱藏（即使後面中止開戰，畫面也乾淨）

            if (setting.TB_BattleUnits == null || setting.TB_BattleUnits.Count < 2)
            {
                Debug.LogError($"[{name}] StartBattle：設定內的單位資料不足兩位，無法開戰。");
                return;
            }

            playerUnit.LoadFrom(setting.TB_BattleUnits[0], asEnemy: false);
            enemyUnit.LoadFrom(setting.TB_BattleUnits[1], asEnemy: true);
            isPlayerFirst = setting.IsPlayer1First;

            // 保險：數值載入失敗（例如角色/敵人型別在 SO 中查無數值）會讓單位 MaxHp=0，
            // 若照常開戰會在第一次資料變動時被判定死亡、直接跳結算。這裡直接中止並給明確錯誤。
            if (playerUnit.MaxHp <= 0 || enemyUnit.MaxHp <= 0)
            {
                Debug.LogError($"[{name}] 開戰中止：單位數值載入失敗（玩家 MaxHp={playerUnit.MaxHp}、敵人 MaxHp={enemyUnit.MaxHp}）。" +
                    "請確認 BattleV2Bootstrap 的 playerType / enemyType 在 Resources/SO_Battle/BattleUnitStats 內有對應數值。");
                return;
            }

            view.SetBackground(setting.TB_OrderOfBackGround);
            BeginBattleCommon();
        }

        /// <summary>
        /// 測試用：不吃外部資料，直接用兩個 BattleUnit 在 Inspector 上已設好的數值開戰。
        /// 方便在 Chunk 3 階段先驗證回合狀態機。
        /// </summary>
        public void StartBattleWithCurrentUnits(bool playerFirst)
        {
            if (!ValidateRefs()) return;
            isPlayerFirst = playerFirst;
            BeginBattleCommon();
        }

        private void BeginBattleCommon()
        {
            RoundCount = 0;
            TurnCount = 0;
            isBattleOver = false;
            isStarted = true;

            view.Bind(playerUnit, enemyUnit);
            view.ShowSettlement(false, false);

            // 訂閱死亡偵測（單一資料來源一變就檢查，取代舊版散落各處的 IfUnitDead 呼叫）
            SubscribeDeath();

            current = isPlayerFirst ? playerUnit : enemyUnit;
            opponent = isPlayerFirst ? enemyUnit : playerUnit;

            if (systems != null) systems.OnBattleStart(this);

            BeginTurn();
        }
        #endregion

        #region 回合流程（狀態機）
        private void BeginTurn()
        {
            if (isBattleOver) return;

            current.ResetDiceCount();
            UpdateTurnLabel();

            // 只有玩家回合才顯示「下一回合」按鈕（敵人回合由 AI 自動結束）
            view.ShowNextTurnButton(IsPlayerTurn);

            if (systems != null) systems.PrepareTurn(current, opponent);

            // 這回合是否被跳過（暈眩等）——由子系統於 PrepareTurn 中判定
            if (systems != null && systems.ConsumeSkipFlag())
            {
                Debug.Log($"[{name}] {current.NameTw} 這回合被跳過。");
                RequestNextTurn();
                return;
            }

            if (!IsPlayerTurn)
            {
                if (enemyRoutine != null) StopCoroutine(enemyRoutine);
                enemyRoutine = StartCoroutine(EnemyTurn());
            }
        }

        /// <summary>玩家按「下一回合」按鈕時呼叫（把這顆按鈕的 OnClick 綁到這個方法）。</summary>
        public void RequestNextTurn()
        {
            if (!isStarted || isBattleOver) return;
            if (turnRoutine != null) StopCoroutine(turnRoutine);
            turnRoutine = StartCoroutine(SwitchToNextTurn());
        }

        private IEnumerator SwitchToNextTurn()
        {
            if (systems != null) systems.EndTurn(current, opponent);
            yield return null; // 讓上面的結束表現跑一影格

            if (isBattleOver) yield break;

            AdvanceCounters();
            SwapCurrent();
            BeginTurn();
        }

        private IEnumerator EnemyTurn()
        {
            view.ShowEnemyActionPanel(true);
            yield return new WaitForSeconds(enemyThinkSeconds);

            if (!isBattleOver && systems != null)
                systems.RunEnemyTurn(current, opponent);

            view.ShowEnemyActionPanel(false);

            if (isBattleOver) yield break;

            // 敵人行動完自動換回玩家
            RequestNextTurn();
        }

        private void AdvanceCounters()
        {
            TurnCount++;
            if (TurnCount >= 2) // 兩位各行動一次 = 一個遊戲回合(round)
            {
                RoundCount++;
                TurnCount = 0;
            }
        }

        private void SwapCurrent()
        {
            (current, opponent) = (opponent, current);
        }

        private void UpdateTurnLabel()
        {
            view.SetTurnInfo(RoundCount, current.NameTw);
        }
        #endregion

        #region 勝負
        private void SubscribeDeath()
        {
            UnsubscribeDeath();
            if (playerUnit != null) playerUnit.Changed += OnAnyUnitChanged;
            if (enemyUnit != null) enemyUnit.Changed += OnAnyUnitChanged;
        }

        private void UnsubscribeDeath()
        {
            if (playerUnit != null) playerUnit.Changed -= OnAnyUnitChanged;
            if (enemyUnit != null) enemyUnit.Changed -= OnAnyUnitChanged;
        }

        private void OnAnyUnitChanged()
        {
            if (isBattleOver) return;
            if (playerUnit.IsDead) FinishBattle(playerWin: false);
            else if (enemyUnit.IsDead) FinishBattle(playerWin: true);
        }

        private void FinishBattle(bool playerWin)
        {
            if (isBattleOver) return;
            isBattleOver = true;

            if (enemyRoutine != null) StopCoroutine(enemyRoutine);
            if (turnRoutine != null) StopCoroutine(turnRoutine);

            view.ShowNextTurnButton(false);
            view.ShowEnemyActionPanel(false);
            view.ShowSettlement(true, playerWin, playerWin ? "勝利" : "失敗");

            Debug.Log($"[{name}] 戰鬥結束，玩家{(playerWin ? "勝利" : "失敗")}。");
            BattleFinished?.Invoke(playerWin);
        }
        #endregion

        private bool ValidateRefs()
        {
            bool ok = true;
            if (view == null) { Debug.LogError($"[{name}] 未指派 BattleView。"); ok = false; }
            if (playerUnit == null) { Debug.LogError($"[{name}] 未指派 playerUnit。"); ok = false; }
            if (enemyUnit == null) { Debug.LogError($"[{name}] 未指派 enemyUnit。"); ok = false; }
            if (systems == null) Debug.LogWarning($"[{name}] 未指派 BattleSystemsBridge：回合流程可跑，但沒有骰子/卡片表現（Chunk 4 補上）。");
            return ok;
        }

        private void OnDestroy() => UnsubscribeDeath();
    }
}
