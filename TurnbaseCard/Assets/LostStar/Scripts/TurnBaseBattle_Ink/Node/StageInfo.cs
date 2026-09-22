using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個 Stage（小區域／一顆星球）的資料，掛在 Stage prefab 上（由 A_Good_Ink 使用 AI 生成）。
///
/// 取代原本 LevelMapManager.LevelInfo：開發者把 Stage prefab 重複利用、在 Scene 裡擺成各種關卡；
/// <see cref="LevelMapManager"/> 依自己的 <c>stages</c> 清單與各 Stage 的出口(<see cref="exits"/>)控制地圖。
///
/// 節點採「明確編寫」而非自動蒐集：
///   - <see cref="nodes"/>：本 Stage 所有可走節點的清單，由開發者在 Inspector 指定（可用右鍵選單一鍵從子物件填入）。
///   - 節點之間「誰連誰／玩家可走路徑」由各 <see cref="NodeData.connectedNodes"/> 自己編寫（在該節點的 Inspector 拖鄰居）；
///     視覺線由 <see cref="NodeLinkRenderer"/> 依這些已編寫的相鄰畫出，不再靠距離自動連。
///   - <see cref="entryNode"/>（NodeData）/ <see cref="cameraTarget"/>（Transform）/ 出口的 exitNode 是自己的子物件，
///     可放在 prefab 上當預設（用子物件名稱自動抓 "Start" / "CameraPoint"）。
///   - 出口的 <see cref="Exit.targetStage"/> / <see cref="Exit.targetEntryNode"/> 是「跨 Stage 的連接」，
///     由每個場景實例各自在 Inspector 連，不存在共用 prefab 資產上（同一 prefab 會被重複利用成不同關卡）。
///   - <see cref="Enemies"/> 執行期從子物件自動蒐集（敵人是擺放物，不需手動維護清單）。
/// </summary>
public class StageInfo : MonoBehaviour
{
    /// <summary>出口種類：接到另一個 Stage，或作為大關卡的盡頭（結束→結算）。</summary>
    public enum ExitKind { ToStage, EndLevel }

    /// <summary>一個出口：走到 exitNode 時，依 kind 去 targetStage 或結束大關卡。</summary>
    [System.Serializable]
    public class Exit
    {
        [Tooltip("此出口的節點（走到這個節點觸發）")]
        public NodeData exitNode;
        [Tooltip("ToStage：接到 targetStage；EndLevel：大關卡盡頭，結束並進結算")]
        public ExitKind kind = ExitKind.ToStage;
        [Tooltip("目標 Stage（ToStage 用；由每個場景實例各自連）")]
        public StageInfo targetStage;
        [Tooltip("進入目標 Stage 後玩家落點（留空＝目標的 entryNode）")]
        public NodeData targetEntryNode;
    }

    [Header("節點（明確編寫；預設以名稱自動抓入口/相機）")]
    [Tooltip("本 Stage 所有可走節點；由開發者指定（右鍵選單可一鍵從子物件填入）。節點相連由各節點的 connectedNodes 自己編寫。")]
    [SerializeField] private List<NodeData> nodes = new List<NodeData>();
    [Tooltip("入口節點，玩家進入此 Stage 的落點（預設子物件 \"Start\" 上的 NodeData）")]
    public NodeData entryNode;
    [Tooltip("相機錨點（預設子物件 \"CameraPoint\"；留空則看 entryNode）")]
    public Transform cameraTarget;

    [Header("出口（跨 Stage 連接／大關卡盡頭）")]
    [Tooltip("此 Stage 的出口清單；targetStage 由場景實例各自連")]
    public List<Exit> exits = new List<Exit>();

    [Header("其他")]
    [Tooltip("保留：之後接「清完敵人才開門」用")]
    public bool isEnemyClearedCheckEnabled = false;

    // 敵人執行期自動蒐集（快取）
    private List<Transform> _enemies;

    /// <summary>相機對焦點（cameraTarget 優先，否則 entryNode）。</summary>
    public Transform CameraFocus =>
        cameraTarget != null ? cameraTarget : (entryNode != null ? entryNode.transform : null);

    /// <summary>本 Stage 的所有節點（開發者在 Inspector 編寫的清單）。</summary>
    public List<NodeData> Nodes => nodes;

    /// <summary>本 Stage 的敵人（子物件上的 Enemy）。可變動：戰鬥勝利後由外部移除。</summary>
    public List<Transform> Enemies { get { EnsureEnemies(); return _enemies; } }

    /// <summary>
    /// 生成本 Stage 所有「戰鬥節點」(Combat／BossCombat 的 <see cref="NodeEvent"/>) 的敵人。
    /// 由 <see cref="LevelMapManager"/> 在玩家進入本 Stage 時呼叫；冪等（各節點已生成過不會重複生成）。
    /// </summary>
    public void SpawnCombatEnemies()
    {
        foreach (var n in nodes)
        {
            if (n == null) continue;
            var ne = n.GetComponent<NodeEvent>();
            if (ne != null && ne.IsCombatNode) ne.SpawnEnemy();
        }
    }

    private void Awake() => EnsureEnemies();

    private void EnsureEnemies()
    {
        if (_enemies != null) return;
        _enemies = new List<Transform>();
        foreach (var e in GetComponentsInChildren<Enemy>(true)) _enemies.Add(e.transform);
    }

    /// <summary>找出「走到某節點」對應的出口；沒有回傳 null。（node 為玩家所在格的 Transform）</summary>
    public Exit FindExitAt(Transform node)
    {
        if (node == null || exits == null) return null;
        foreach (var ex in exits)
            if (ex != null && ex.exitNode != null && ex.exitNode.transform == node) return ex;
        return null;
    }

#if UNITY_EDITOR
    private void Reset() { AutoWireByName(); CollectNodesFromChildren(); }
    private void OnValidate() { if (entryNode == null || cameraTarget == null) AutoWireByName(); }

    /// <summary>依子物件名稱自動接上 entryNode("Start" 上的 NodeData) 與 cameraTarget("CameraPoint")。</summary>
    private void AutoWireByName()
    {
        if (entryNode == null)
        {
            var start = FindDescendant("Start");
            if (start != null) entryNode = start.GetComponent<NodeData>();
        }
        if (cameraTarget == null) cameraTarget = FindDescendant("CameraPoint");
    }

    /// <summary>一鍵把所有子物件上的 NodeData 填入 <see cref="nodes"/>（僅填清單，不動各節點的相鄰）。</summary>
    [ContextMenu("從子物件蒐集節點 (填入 nodes)")]
    private void CollectNodesFromChildren()
    {
        nodes = new List<NodeData>(GetComponentsInChildren<NodeData>(true));
        UnityEditor.EditorUtility.SetDirty(this);
    }

    private Transform FindDescendant(string childName)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t != transform && t.name == childName) return t;
        return null;
    }
#endif
}
