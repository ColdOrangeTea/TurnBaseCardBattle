using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 測試/作弊按鈕的顯示控制（由 A_Good_Ink 使用 AI 生成）。
///
/// BattleEmpty 上保留了一批舊的測試/作弊按鈕（開戰測試、敵我 HP 歸零、敵人行動測試、作弊開關等）。
/// 這些按鈕先不刪除，但平常應該藏起來，避免正式遊玩時誤觸；需要除錯時再用快捷鍵叫出。
///
/// 行為：
///   - 進場預設隱藏（<see cref="hiddenOnStart"/>）；
///   - 除錯時按 <see cref="toggleKey"/> 切換顯示/隱藏；
///   - <see cref="devOnly"/> 開啟時，只有 Editor / Development Build 能叫出，正式版一律保持隱藏。
/// 只切換按鈕的 active 狀態，不刪除按鈕本身。
/// </summary>
public class CheatButtonController : MonoBehaviour
{
    [Tooltip("要控制顯示的測試/作弊按鈕（GameObject）。")]
    [SerializeField] private List<GameObject> cheatButtons = new List<GameObject>();

    [Tooltip("進場時先隱藏這些按鈕。")]
    [SerializeField] private bool hiddenOnStart = true;

    [Tooltip("切換顯示/隱藏的快捷鍵（除錯用）。")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Tooltip("只允許在 Editor / Development Build 用快捷鍵叫出；正式版一律保持隱藏。")]
    [SerializeField] private bool devOnly = true;

    private bool visible;

    private void Start()
    {
        SetVisible(!hiddenOnStart);
    }

    private void Update()
    {
        if (!IsToggleAllowed()) return;
        if (Input.GetKeyDown(toggleKey)) SetVisible(!visible);
    }

    private bool IsToggleAllowed()
    {
        if (!devOnly) return true;
        return Application.isEditor || Debug.isDebugBuild;
    }

    /// <summary>設定所有作弊按鈕的顯示狀態。</summary>
    public void SetVisible(bool show)
    {
        visible = show;
        foreach (var go in cheatButtons)
        {
            if (go != null) go.SetActive(show);
        }
    }

    /// <summary>切換顯示（可接到 UI 或其他觸發）。</summary>
    public void Toggle() => SetVisible(!visible);

    /// <summary>供 Editor 工具帶入要控制的按鈕清單（覆寫既有內容）。</summary>
    public void SetButtons(List<GameObject> buttons) => cheatButtons = buttons ?? new List<GameObject>();
}
