using System;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TurnBaseBattleV2;

/// <summary>
/// 地圖格子事件的統一入口。玩家走到事件格時，由 PlayerController 呼叫本服務依類型觸發：
///   - Combat / BossCombat：本服務不處理——戰鬥一律靠節點生成的 Enemy 碰撞觸發（見 NodeEvent.SpawnEnemy）。
///   - Shop / Event / Treasure / quest：尚未實作，先做「空殼」——印訊息並拋出對應事件（hook），
///     日後把商店/事件/寶箱/任務系統接到這些 hook 即可，不必再改動玩家/回合流程。
/// </summary>
public class MapEventService : MonoBehaviour
{
    [Tooltip("開戰前用來啟用阻擋層；可留空。")]
    [SerializeField] private S001_PlayerController playerController;

    [Tooltip("戰鬥時的玩家角色類型（之後可改為帶入實際存檔資料）。")]
    [SerializeField] private CharacterType playerType = CharacterType.Seraphis;

    // ── 空殼事件 hook：日後接上對應系統時訂閱即可 ──
    public event Action<NodeEvent> ShopRequested;
    public event Action<NodeEvent> GenericEventRequested;
    public event Action<NodeEvent> TreasureRequested;
    public event Action<NodeEvent> QuestRequested;

    private void Awake()
    {
        // 烘進 LevelMapManager prefab 後，playerController 未指派時執行期自動尋找（免場景接線）
        if (playerController == null) playerController = FindAnyObjectByType<S001_PlayerController>();
    }

    /// <summary>
    /// 觸發一格的事件。回傳 true 代表「進入了戰鬥」，呼叫端應暫停地圖回合、等戰鬥結束再繼續。
    /// </summary>
    public bool TriggerNodeEvent(NodeEvent grid)
    {
        if (grid == null || !grid.CanTrigger) return false;

        // 無事件格：不觸發、也不消耗（保持可重複踏過）
        if (grid.eventType == NodeEventType.None) return false;

        // 戰鬥節點(Combat／BossCombat)：戰鬥一律靠生成的 Enemy 碰撞觸發，節點本身不開戰、也不消耗
        if (grid.eventType == NodeEventType.Combat || grid.eventType == NodeEventType.BossCombat) return false;

        grid.MarkConsumed();

        switch (grid.eventType)
        {
            case NodeEventType.Shop:
                BattleLog.Log("[MapEventService]（空殼）觸發商店事件。");
                ShopRequested?.Invoke(grid);
                return false;

            case NodeEventType.Event:
                BattleLog.Log("[MapEventService]（空殼）觸發一般事件。");
                GenericEventRequested?.Invoke(grid);
                return false;

            case NodeEventType.Treasure:
                BattleLog.Log("[MapEventService]（空殼）觸發寶箱事件。");
                TreasureRequested?.Invoke(grid);
                return false;

            case NodeEventType.quest:
                BattleLog.Log("[MapEventService]（空殼）觸發任務事件。");
                QuestRequested?.Invoke(grid);
                return false;

            case NodeEventType.StageGate:
                // 起點/終點的門只是視覺標記；換關由 LevelMapManager 依 endGrid 判定，這裡不觸發事件。
                return false;
        }
        return false;
    }
}
