using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一個 Stage（小區域）的資料，掛在 LevelMap_Stage prefab 上（由 A_Good_Ink 使用 AI 生成）。
///
/// 取代原本 LevelMapManager.LevelInfo：開發者把此 prefab 重複利用、在 Scene 裡擺成各種關卡；
/// <see cref="LevelMapManager"/> 於 Start 自動蒐集場上所有 StageInfo，並依 startStage 與各 Stage 的
/// 出口(<see cref="exits"/>)控制地圖（哪裡是起點、走某出口去哪個 Stage、哪個出口是大關卡盡頭→結算）。
///
/// 資料分工：
///   - <see cref="entryNode"/> / <see cref="cameraTarget"/> / 出口的 exitNode 是自己的子物件，
///     可放在 prefab 上當預設（用子物件名稱自動抓 "Start" / "CameraPoint"）。
///   - 出口的 <see cref="Exit.targetStage"/> / <see cref="Exit.targetEntryNode"/> 是「跨 Stage 的連接」，
///     由每個場景實例各自在 Inspector 連，不存在共用 prefab 資產上（同一 prefab 會被重複利用成不同關卡）。
///   - <see cref="GridList"/> / <see cref="Enemies"/> 執行期從自己的子物件自動蒐集，不必手動維護。
/// </summary>
public class StageInfo : MonoBehaviour
{
    /// <summary>出口種類：接到另一個 Stage，或作為大關卡的盡頭（結束→結算）。</summary>
    public enum ExitKind { ToStage, EndLevel }

    /// <summary>一個出口：走到 exitNode 時，依 kind 去 targetStage 或結束大關卡。</summary>
    [System.Serializable]
    public class Exit
    {
        [Tooltip("此出口的節點（走到這格觸發）")]
        public Transform exitNode;
        [Tooltip("ToStage：接到 targetStage；EndLevel：大關卡盡頭，結束並進結算")]
        public ExitKind kind = ExitKind.ToStage;
        [Tooltip("目標 Stage（ToStage 用；由每個場景實例各自連）")]
        public StageInfo targetStage;
        [Tooltip("進入目標 Stage 後玩家落點（留空＝目標的 entryNode）")]
        public Transform targetEntryNode;
    }

    [Header("節點（本 Stage 子物件；預設以名稱自動抓）")]
    [Tooltip("入口節點，玩家進入此 Stage 的落點（預設子物件 \"Start\"）")]
    public Transform entryNode;
    [Tooltip("相機錨點（預設子物件 \"CameraPoint\"；留空則看 entryNode）")]
    public Transform cameraTarget;

    [Header("出口（跨 Stage 連接／大關卡盡頭）")]
    [Tooltip("此 Stage 的出口清單；targetStage 由場景實例各自連")]
    public List<Exit> exits = new List<Exit>();

    [Header("其他")]
    [Tooltip("保留：之後接「清完敵人才開門」用")]
    public bool isEnemyClearedCheckEnabled = false;

    // 執行期自動蒐集（快取）
    private List<Transform> _gridList;
    private List<Transform> _enemies;

    /// <summary>相機對焦點（cameraTarget 優先，否則 entryNode）。</summary>
    public Transform CameraFocus => cameraTarget != null ? cameraTarget : entryNode;

    /// <summary>本 Stage 的所有格（子物件上的 NodeData）。首次存取時蒐集並快取。</summary>
    public List<Transform> GridList { get { EnsureCollected(); return _gridList; } }

    /// <summary>本 Stage 的敵人（子物件上的 Enemy）。可變動：戰鬥勝利後由外部移除。</summary>
    public List<Transform> Enemies { get { EnsureCollected(); return _enemies; } }

    private void Awake() => EnsureCollected();

    private void EnsureCollected()
    {
        if (_gridList != null) return;
        _gridList = new List<Transform>();
        foreach (var gd in GetComponentsInChildren<NodeData>(true)) _gridList.Add(gd.transform);
        _enemies = new List<Transform>();
        foreach (var e in GetComponentsInChildren<Enemy>(true)) _enemies.Add(e.transform);
    }

    /// <summary>找出「走到某節點」對應的出口；沒有回傳 null。</summary>
    public Exit FindExitAt(Transform node)
    {
        if (node == null || exits == null) return null;
        foreach (var ex in exits)
            if (ex != null && ex.exitNode == node) return ex;
        return null;
    }

#if UNITY_EDITOR
    private void Reset() => AutoWireByName();
    private void OnValidate() { if (entryNode == null || cameraTarget == null) AutoWireByName(); }

    /// <summary>依子物件名稱自動接上 entryNode("Start") 與 cameraTarget("CameraPoint")。</summary>
    private void AutoWireByName()
    {
        if (entryNode == null) entryNode = FindDescendant("Start");
        if (cameraTarget == null) cameraTarget = FindDescendant("CameraPoint");
    }

    private Transform FindDescendant(string childName)
    {
        foreach (var t in GetComponentsInChildren<Transform>(true))
            if (t != transform && t.name == childName) return t;
        return null;
    }
#endif
}
