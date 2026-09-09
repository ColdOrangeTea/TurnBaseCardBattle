using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 章節選擇畫面的控制器：
/// 依 ChapterListData（章節列表資訊表）在可捲動的清單中由上到下生成章節按鈕（ChapterButton Prefab），
/// 按鈕顯示「第幾章 + 標題」，點擊後直接把該章節的對話資訊表送進對話系統並開始播放劇情。
/// 對話結束（跳過或播畢淡出）後可自動回到章節選擇畫面。
/// </summary>
public class ChapterSelectController : MonoBehaviour
{
    /// <summary>帶章節索引的事件（供 Inspector 綁定額外行為，如存檔、音效）。</summary>
    [Serializable]
    public class ChapterIndexEvent : UnityEvent<int> { }

    [Header("資料")]
    [SerializeField]
    [Tooltip("章節列表資訊表。可在執行期用 SetChapterList() 換成其他章節列表（主線 / 支線）。")]
    private ChapterListData chapterList;

    [Header("組件")]
    [SerializeField]
    [Tooltip("整個章節選擇面板的根物件；播放劇情時會隱藏它。留空則使用本物件。")]
    private GameObject panelRoot;
    [SerializeField]
    [Tooltip("章節清單的捲動視圖 (ScrollRect)。")]
    private ScrollRect scrollRect;
    [SerializeField]
    [Tooltip("章節按鈕的父物件（掛 VerticalLayoutGroup，由上往下排列）。")]
    private RectTransform contentRoot;
    [SerializeField]
    [Tooltip("章節按鈕 Prefab（需含 ChapterButtonController）。")]
    private GameObject chapterButtonPrefab;
    [SerializeField]
    [Tooltip("畫面標題文字（顯示 ChapterListData.menuTitle）。可為空。")]
    private TMP_Text menuTitleText;

    [Header("對話系統")]
    [SerializeField]
    [Tooltip("對話 UI 的總控制器；點擊章節後由它開啟對話 UI 並開始播放。")]
    private TriggerDialogue dialogueTrigger;
    [SerializeField]
    [Tooltip("對話打字機；未指定 dialogueTrigger 時的備援播放對象。")]
    private DialogueTypingEffect dialogueTyper;

    [Header("行為設定")]
    [SerializeField]
    [Tooltip("面板啟用時重新依資料生成按鈕（章節解鎖狀態變動時會即時反映）。")]
    private bool rebuildOnEnable = true;
    [SerializeField]
    [Tooltip("點擊章節後隱藏章節選擇面板，避免與對話 UI 疊在一起。")]
    private bool hidePanelOnPlay = true;
    [SerializeField]
    [Tooltip("對話結束後自動回到章節選擇畫面。")]
    private bool reopenAfterDialogue = true;

    [Header("事件接口")]
    [Tooltip("章節被點擊時觸發，參數為章節索引。")]
    public ChapterIndexEvent onChapterSelected;
    [Tooltip("從劇情返回章節選擇畫面時觸發。")]
    public UnityEvent onReturnFromDialogue;

    // 已生成的章節按鈕（重建時整批回收）
    private readonly List<ChapterButtonController> spawnedButtons = new List<ChapterButtonController>();

    private bool eventSubscribed;   // 是否已訂閱對話結束事件
    private bool isPlayingChapter;  // 是否正在播放章節劇情

    /// <summary>目前使用的章節列表資訊表。</summary>
    public ChapterListData ChapterList => chapterList;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
    }

    private void OnEnable()
    {
        SubscribeDialogueEvents();
        if (rebuildOnEnable || spawnedButtons.Count == 0) Build();
    }

    private void OnDisable()
    {
        UnsubscribeDialogueEvents();
    }

    #region 對外接口

    /// <summary>更換章節列表資訊表並重建清單（例：主線 / 支線切換）。</summary>
    public void SetChapterList(ChapterListData data)
    {
        chapterList = data;
        Build();
    }

    /// <summary>依目前的章節列表資訊表重新生成整份章節清單。</summary>
    public void Build()
    {
        ClearButtons();

        if (contentRoot == null)
        {
            Debug.LogWarning($"{name} 的 ChapterSelectController 缺少 contentRoot（章節按鈕的父物件），無法生成章節清單。");
            return;
        }
        if (chapterButtonPrefab == null)
        {
            Debug.LogWarning($"{name} 的 ChapterSelectController 缺少 chapterButtonPrefab（章節按鈕 Prefab），無法生成章節清單。");
            return;
        }
        if (chapterList == null)
        {
            Debug.LogWarning($"{name} 的 ChapterSelectController 尚未指定章節列表資訊表 (ChapterListData)，章節清單為空。");
            return;
        }

        if (menuTitleText != null && !string.IsNullOrEmpty(chapterList.menuTitle))
        {
            menuTitleText.text = chapterList.menuTitle;
        }

        for (int i = 0; i < chapterList.ChapterCount; i++)
        {
            ChapterListData.ChapterEntry entry = chapterList.GetChapter(i);
            if (entry == null) continue;

            GameObject go = Instantiate(chapterButtonPrefab, contentRoot, false); // false = 沿用 Prefab 的區域座標，不要為了保住世界座標而算歪 UI
            go.name = $"ChapterButton_{chapterList.GetChapterNumber(i)}";
            go.SetActive(true); // Prefab 若以隱藏狀態存檔仍能正常顯示

            var controller = go.GetComponent<ChapterButtonController>();
            if (controller == null)
            {
                Debug.LogWarning($"章節按鈕 Prefab「{chapterButtonPrefab.name}」缺少 ChapterButtonController，第 {i + 1} 個按鈕無法運作。");
                continue;
            }

            controller.Setup(
                i,
                chapterList.GetDisplayTitle(i),
                chapterList.GetSummary(i),
                entry.thumbnail,
                entry.unlocked,
                PlayChapter);
            spawnedButtons.Add(controller);
        }

        ScrollToTop();
    }

    /// <summary>播放指定索引的章節劇情（由章節按鈕點擊觸發，也可由外部直接呼叫）。</summary>
    public void PlayChapter(int index)
    {
        if (isPlayingChapter) return; // 劇情播放中，忽略重複點擊
        if (chapterList == null) return;

        // 是否可播放的判斷集中在資訊表，與其他章節選擇畫面共用同一套規則
        if (!chapterList.TryGetPlayableChapter(index, out ChapterListData.ChapterEntry entry, out string reason))
        {
            Debug.Log(reason);
            return;
        }

        isPlayingChapter = true;
        onChapterSelected?.Invoke(index);

        if (hidePanelOnPlay && panelRoot != null) panelRoot.SetActive(false);

        // 優先交給對話總控制器（會一併處理淡入淡出與按鈕狀態）
        if (dialogueTrigger != null)
        {
            dialogueTrigger.PlayDialogue(entry.dialogueData);
            return;
        }
        // 備援：直接驅動打字機
        if (dialogueTyper != null)
        {
            dialogueTyper.SetDialogueData(entry.dialogueData);
            dialogueTyper.ToStartDialogue();
            return;
        }

        isPlayingChapter = false;
        if (hidePanelOnPlay && panelRoot != null) panelRoot.SetActive(true);
        Debug.LogWarning($"{name} 的 ChapterSelectController 尚未綁定 TriggerDialogue 或 DialogueTypingEffect，無法跳轉到劇情。");
    }

    /// <summary>回到章節選擇畫面（可由外部的「返回」按鈕綁定）。</summary>
    public void ReturnToChapterSelect()
    {
        isPlayingChapter = false;
        if (panelRoot != null) panelRoot.SetActive(true);
        if (rebuildOnEnable) Build(); // 重建清單，反映期間變動的章節解鎖狀態
        onReturnFromDialogue?.Invoke();
    }

    #endregion

    #region 內部處理

    /// <summary>對話結束（淡出完成）後的回呼：依設定回到章節選擇畫面。</summary>
    private void HandleDialogueClosed()
    {
        if (!isPlayingChapter) return;
        isPlayingChapter = false;
        if (!reopenAfterDialogue) return;
        ReturnToChapterSelect();
    }

    private void SubscribeDialogueEvents()
    {
        if (eventSubscribed || dialogueTrigger == null) return;
        dialogueTrigger.OnDialogueClosed += HandleDialogueClosed;
        eventSubscribed = true;
    }

    private void UnsubscribeDialogueEvents()
    {
        if (!eventSubscribed || dialogueTrigger == null) return;
        dialogueTrigger.OnDialogueClosed -= HandleDialogueClosed;
        eventSubscribed = false;
    }

    private void ClearButtons()
    {
        spawnedButtons.Clear();
        if (contentRoot == null) return;

        for (int i = contentRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = contentRoot.GetChild(i).gameObject;
            child.transform.SetParent(null, false); // 先脫離父物件：Destroy 是延遲執行的，避免與新生成的卡片混在一起
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    /// <summary>把清單捲回最上方（重建後從第一章開始看）。</summary>
    private void ScrollToTop()
    {
        if (scrollRect == null) return;
        scrollRect.verticalNormalizedPosition = 1f;
    }

    #endregion
}
