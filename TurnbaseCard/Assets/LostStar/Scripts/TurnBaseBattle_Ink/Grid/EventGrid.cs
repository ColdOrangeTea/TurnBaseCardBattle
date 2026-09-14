using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

/// <summary>格子事件類型。</summary>
public enum GridEventType
{
    BossCombat, // 戰鬥（接 V2 戰鬥）
    Shop,       // 商店（尚未實作，先空殼）
    Event,      // 一般事件（尚未實作，先空殼）
    Treasure,   // 寶箱（尚未實作，先空殼）
    quest       // 任務（尚未實作，先空殼）
}

/// <summary>
/// 掛在「事件格」上的標記。玩家走到此格時由 MapEventService 依 eventType 觸發對應事件。
///
/// 註：舊版此腳本內含 Shop/Treasure/Quest/EventUI 等觸發邏輯，因那些系統尚未移植而移除；
/// 現在只保留「事件資料」，實際觸發交給 MapEventService（戰鬥已接 V2，其餘為空殼待補）。
/// </summary>
public class EventGrid : MonoBehaviour
{
    [Tooltip("此格的事件類型")]
    public GridEventType eventType;

    [Tooltip("BossCombat 時要開打的敵人類型")]
    public EnemyType enemyType = EnemyType.Yarn;

    [Tooltip("是否只觸發一次（觸發後不再觸發）")]
    public bool triggerOnce = true;

    [Tooltip("執行期用：是否已觸發過")]
    [SerializeField] private bool consumed = false;

    /// <summary>此格是否還能觸發（triggerOnce 且已觸發過則否）。</summary>
    public bool CanTrigger => !(triggerOnce && consumed);

    /// <summary>標記為已觸發。</summary>
    public void MarkConsumed() => consumed = true;
}
