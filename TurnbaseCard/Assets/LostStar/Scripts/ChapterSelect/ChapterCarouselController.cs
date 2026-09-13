using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 章節選擇的「大圖預覽 + 中心聚焦縮圖輪播」主控。
///
/// 整份畫面由 ChapterListData（章節列表資訊表）驅動：新增章節只要在資料表加一筆、
/// 補上圖與（需要時）該章節專用的卡片 Prefab 即可，不必改任何程式。
///
/// 排列以浮點 offset（目前索引）驅動，每格 d = i - offset、ad = Min(|d|, maxDistance)：
///   x         = Sign(d) * STEP / (1 - DECAY) * (1 - DECAY^ad)
///   scale     = DECAY^ad
///   brightness= brightnessDecay^ad（乘算到卡片的 Image.color）
///   alpha     = alphaDecay^|d|（不夾下限，遠處直接溶進背景色）
///   siblingIndex 越遠越低，中央永遠疊最上面；alpha 過低時關掉 blocksRaycasts。
/// </summary>
public class ChapterCarouselController : MonoBehaviour
{
    /// <summary>帶章節索引的事件（供 Inspector 綁定額外行為，如音效、存檔）。</summary>
    [Serializable]
    public class ChapterIndexEvent : UnityEvent<int> { }

    [Header("資料")]
    [SerializeField]
    [Tooltip("章節列表資訊表。可在執行期用 SetChapterList() 換成其他章節列表（主線 / 支線）。")]
    private ChapterListData chapterList;

    [Header("畫面區塊")]
    [SerializeField]
    [Tooltip("整個章節選擇畫面的根物件；播放劇情時會隱藏它。留空則使用本物件。")]
    private GameObject panelRoot;
    [SerializeField]
    [Tooltip("章節主視覺呈現（ChapterHeroView）：可設為全螢幕主題背景（黑底之上、切章節交叉淡化）或頂部大圖帶。可為空。")]
    private ChapterHeroView heroView;
    [SerializeField]
    [Tooltip("文案區（MetaPanel）。可為空。")]
    private ChapterMetaView metaView;

    [Header("輪播組件")]
    [SerializeField]
    [Tooltip("縮圖卡片的父物件（卡片以 anchoredPosition.x 排列，錨點需為中央）。")]
    private RectTransform itemsRoot;
    [SerializeField]
    [Tooltip("預設的縮圖卡片 Prefab（需含 ChapterCarouselItem）。章節可在資料表用 customCardPrefab 各自覆寫。")]
    private GameObject defaultCardPrefab;
    [SerializeField]
    [Tooltip("左箭頭按鈕（往前一章）。可為空。")]
    private Button leftArrow;
    [SerializeField]
    [Tooltip("右箭頭按鈕（往後一章）。可為空。")]
    private Button rightArrow;
    [SerializeField]
    [Tooltip("卡片所在的 Canvas；換算拖曳距離用。留空會自動往父物件尋找。")]
    private Canvas parentCanvas;

    [Header("排列參數")]
    [SerializeField]
    [Tooltip("相鄰兩格的基礎間距（STEP）。")]
    [Min(1f)]
    private float step = 165f;
    [SerializeField]
    [Tooltip("每往外一格的衰減率（DECAY），同時作用於間距與縮放。")]
    [Range(0.05f, 0.99f)]
    private float decay = 0.75f;
    [SerializeField]
    [Tooltip("每往外一格的亮度衰減率。")]
    [Range(0.05f, 1f)]
    private float brightnessDecay = 0.74f;
    [SerializeField]
    [Tooltip("每往外一格的透明度衰減率（不夾下限，遠處直接溶進背景色）。")]
    [Range(0.05f, 1f)]
    private float alphaDecay = 0.62f;
    [SerializeField]
    [Tooltip("位移 / 縮放 / 亮度的距離上限；超過這個格數後外觀不再變化。")]
    [Min(1f)]
    private float maxDistance = 3f;
    [SerializeField]
    [Tooltip("alpha 低於此值的卡片會關掉 blocksRaycasts，避免透明卡片擋住點擊。")]
    [Range(0f, 1f)]
    private float raycastAlphaThreshold = 0.12f;

    [Header("動效設定")]
    [SerializeField]
    [Tooltip("縮圖位移 / 縮放的時長（秒），使用 EaseOutCubic。")]
    [Min(0f)]
    private float moveDuration = 0.45f;

    [Header("互動設定")]
    [SerializeField]
    [Tooltip("是否啟用鍵盤左右方向鍵切換。")]
    private bool enableKeyboard = true;
    [SerializeField]
    [Tooltip("拖曳超過這個螢幕距離（像素）後，放開時不視為點擊，避免拖完誤觸卡片。")]
    [Min(0f)]
    private float dragClickCancelDistance = 12f;
    [SerializeField]
    [Tooltip("點擊已選中的中央卡片時直接開始該章節劇情。")]
    private bool centerClickStartsChapter = true;
    [SerializeField]
    [Tooltip("面板啟用時重新依資料生成卡片（章節解鎖狀態變動時會即時反映）。")]
    private bool rebuildOnEnable = true;
    [SerializeField]
    [Tooltip("點擊章節後隱藏本畫面，避免與對話 UI 疊在一起。")]
    private bool hidePanelOnPlay = true;
    [SerializeField]
    [Tooltip("對話結束後自動回到章節選擇畫面。")]
    private bool reopenAfterDialogue = true;
    [SerializeField]
    [Tooltip("選定章節後不在此播放對話，改交給外部（如章節劇情節點圖）接手；由 onChapterChosen 通知。連接元件會自動打開此開關。")]
    private bool deferChapterToExternal = false;

    [Header("對話系統")]
    [SerializeField]
    [Tooltip("對話 UI 的總控制器；選定章節後由它開啟對話 UI 並開始播放。")]
    private TriggerDialogue dialogueTrigger;
    [SerializeField]
    [Tooltip("對話打字機；未指定 dialogueTrigger 時的備援播放對象。")]
    private DialogueTypingEffect dialogueTyper;

    [Header("事件接口")]
    [Tooltip("選中的章節改變時觸發，參數為章節索引。")]
    public ChapterIndexEvent onSelectionChanged;
    [Tooltip("開始播放某章節時觸發，參數為章節索引。")]
    public ChapterIndexEvent onChapterStarted;
    [Tooltip("啟用「交給外部接手」時，選定章節後觸發（連接元件用它開啟該章節的劇情節點圖），參數為章節索引。")]
    public ChapterIndexEvent onChapterChosen;
    [Tooltip("從劇情返回章節選擇畫面時觸發。")]
    public UnityEvent onReturnFromDialogue;

    private readonly List<ChapterCarouselItem> items = new List<ChapterCarouselItem>();
    private int[] sortOrder = Array.Empty<int>();   // 依距離排序後的卡片索引（用於 siblingIndex）
    private float[] sortDistances = Array.Empty<float>();

    private float offset;              // 浮點索引（驅動整個排列）
    private int selectedIndex;         // 目前選中的章節
    private Coroutine moveRoutine;     // 位移協程（重啟而非排隊）
    private bool dragging;
    private float dragScreenDistance;  // 本次拖曳累積的螢幕位移
    private float suppressClickUntil;  // 在這個時間點之前收到的點擊視為拖曳殘留
    private bool eventSubscribed;
    private bool isPlayingChapter;
    private bool built;

    /// <summary>目前選中的章節索引。</summary>
    public int SelectedIndex => selectedIndex;

    /// <summary>目前使用的章節列表資訊表。</summary>
    public ChapterListData ChapterList => chapterList;

    /// <summary>章節總數。</summary>
    public int Count => chapterList != null ? chapterList.ChapterCount : 0;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();
    }

    private void OnEnable()
    {
        SubscribeDialogueEvents();
        if (rebuildOnEnable || !built) Build();
    }

    private void OnDisable()
    {
        UnsubscribeDialogueEvents();
    }

    private void Update()
    {
        HandleKeyboard();
    }

    #region 對外接口

    /// <summary>更換章節列表資訊表並重建整個輪播。</summary>
    public void SetChapterList(ChapterListData data)
    {
        chapterList = data;
        selectedIndex = 0;
        Build();
    }

    /// <summary>依目前的章節列表資訊表重建卡片、大圖層與文案。</summary>
    public void Build()
    {
        ClearItems();
        built = true;

        if (itemsRoot == null)
        {
            Debug.LogWarning($"{name} 的 ChapterCarouselController 缺少 itemsRoot（縮圖卡片的父物件），無法生成輪播。");
            return;
        }
        if (chapterList == null)
        {
            Debug.LogWarning($"{name} 的 ChapterCarouselController 尚未指定章節列表資訊表 (ChapterListData)，輪播為空。");
            if (heroView != null) heroView.Build(null);
            UpdateArrows();
            return;
        }

        for (int i = 0; i < chapterList.ChapterCount; i++)
        {
            ChapterListData.ChapterEntry entry = chapterList.GetChapter(i);
            if (entry == null) continue;

            // 章節可以自帶專用卡片 Prefab，沒指定就用預設的
            GameObject prefab = entry.customCardPrefab != null ? entry.customCardPrefab : defaultCardPrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"章節「{chapterList.GetDisplayTitle(i)}」沒有可用的縮圖卡片 Prefab（預設與自訂皆為空），已略過。");
                continue;
            }

            GameObject go = Instantiate(prefab, itemsRoot, false); // false = 沿用 Prefab 的區域座標，不要為了保住世界座標而算歪 UI
            go.name = $"ChapterCard_{chapterList.GetChapterNumber(i)}";
            go.SetActive(true);

            var item = go.GetComponent<ChapterCarouselItem>();
            if (item == null)
            {
                Debug.LogWarning($"卡片 Prefab「{prefab.name}」缺少 ChapterCarouselItem，章節「{chapterList.GetDisplayTitle(i)}」的卡片無法運作。");
                continue;
            }

            item.Bind(
                i,
                chapterList.GetThumbnailSprite(i),
                chapterList.GetBlurredSprite(i),
                chapterList.GetChapterNumber(i).ToString(),
                entry.unlocked,
                HandleItemClicked);
            items.Add(item);
        }

        if (sortOrder.Length != items.Count)
        {
            sortOrder = new int[items.Count];
            sortDistances = new float[items.Count];
        }

        if (heroView != null) heroView.Build(chapterList);

        selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, Count - 1));
        offset = selectedIndex;
        ApplyLayout();
        ApplySelection(selectedIndex, true);
    }

    /// <summary>移動到指定章節（會播放位移動效）。</summary>
    public void GoTo(int index)
    {
        GoTo(index, true);
    }

    /// <summary>移動到指定章節；animate = false 時直接跳到定位。</summary>
    public void GoTo(int index, bool animate)
    {
        if (Count == 0) return;

        index = Mathf.Clamp(index, 0, Count - 1);
        ApplySelection(index, false);

        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine); // 連續操作時中途接手，不排隊
            moveRoutine = null;
        }

        if (!animate || moveDuration <= 0f || !isActiveAndEnabled)
        {
            SetOffset(index);
            return;
        }
        moveRoutine = StartCoroutine(MoveRoutine(index));
    }

    /// <summary>往前 / 往後一格（左右箭頭按鈕綁定用；delta 傳 -1 或 +1）。</summary>
    public void StepBy(int delta)
    {
        if (Count == 0) return;
        GoTo(selectedIndex + delta, true);
    }

    /// <summary>左箭頭（Inspector OnClick 綁定）。</summary>
    public void Btn_Previous() => StepBy(-1);

    /// <summary>右箭頭（Inspector OnClick 綁定）。</summary>
    public void Btn_Next() => StepBy(1);

    /// <summary>開始播放目前選中的章節（可綁在「開始閱讀」按鈕上）。</summary>
    public void PlaySelectedChapter() => PlayChapter(selectedIndex);

    /// <summary>開始播放指定章節的劇情。</summary>
    public void PlayChapter(int index)
    {
        if (isPlayingChapter) return; // 劇情播放中，忽略重複觸發
        if (chapterList == null) return;

        // 交給外部（如章節劇情節點圖）接手：只做解鎖檢查，不在此播放對話。
        // 此時的章節可以沒有單一 DialogueData（劇情拆成多個節點），因此不套用 TryGetPlayableChapter。
        if (deferChapterToExternal)
        {
            ChapterListData.ChapterEntry chosen = chapterList.GetChapter(index);
            if (chosen == null)
            {
                Debug.Log($"章節索引 {index} 超出章節列表範圍。");
                return;
            }
            if (!chosen.unlocked)
            {
                Debug.Log($"章節「{chapterList.GetDisplayTitle(index)}」尚未解鎖。");
                return;
            }

            if (hidePanelOnPlay && panelRoot != null) panelRoot.SetActive(false);
            onChapterStarted?.Invoke(index);
            onChapterChosen?.Invoke(index);
            return;
        }

        if (!chapterList.TryGetPlayableChapter(index, out ChapterListData.ChapterEntry entry, out string reason))
        {
            Debug.Log(reason);
            return;
        }

        isPlayingChapter = true;
        onChapterStarted?.Invoke(index);

        if (hidePanelOnPlay && panelRoot != null) panelRoot.SetActive(false);

        if (dialogueTrigger != null)
        {
            dialogueTrigger.PlayDialogue(entry.dialogueData);
            return;
        }
        if (dialogueTyper != null)
        {
            dialogueTyper.SetDialogueData(entry.dialogueData);
            dialogueTyper.ToStartDialogue();
            return;
        }

        isPlayingChapter = false;
        if (hidePanelOnPlay && panelRoot != null) panelRoot.SetActive(true);
        Debug.LogWarning($"{name} 的 ChapterCarouselController 尚未綁定 TriggerDialogue 或 DialogueTypingEffect，無法跳轉到劇情。");
    }

    /// <summary>設定是否把「選定章節」交給外部接手（連接元件會在 Awake 時打開）。</summary>
    public void SetDeferChapterToExternal(bool defer)
    {
        deferChapterToExternal = defer;
    }

    /// <summary>回到章節選擇畫面（可由外部的「返回」按鈕綁定）。</summary>
    public void ReturnToChapterSelect()
    {
        isPlayingChapter = false;
        if (panelRoot != null) panelRoot.SetActive(true);
        onReturnFromDialogue?.Invoke();
    }

    #endregion

    #region 排列計算

    /// <summary>設定浮點索引並立刻套用整排卡片的版面。</summary>
    private void SetOffset(float value)
    {
        offset = value;
        ApplyLayout();
    }

    /// <summary>
    /// 依目前 offset 算出每張卡片的位置 / 縮放 / 亮度 / 透明度 / 疊層順序。
    /// </summary>
    private void ApplyLayout()
    {
        if (items.Count == 0) return;

        // STEP / (1 - DECAY) 為等比級數的和；decay 已限制在 1 以下，不會除以 0
        float safeDecay = Mathf.Clamp(decay, 0.05f, 0.99f);
        float spread = step / (1f - safeDecay);

        for (int i = 0; i < items.Count; i++)
        {
            ChapterCarouselItem item = items[i];
            if (item == null) continue;

            float d = item.Index - offset;
            float absD = Mathf.Abs(d);
            float ad = Mathf.Min(absD, maxDistance);

            float x = Mathf.Sign(d) * spread * (1f - Mathf.Pow(safeDecay, ad));
            float scale = Mathf.Pow(safeDecay, ad);
            float brightness = Mathf.Pow(brightnessDecay, ad);
            float alpha = Mathf.Pow(alphaDecay, absD); // 不夾下限：遠處自然溶進背景色

            item.ApplyLayout(x, scale, brightness, alpha, alpha >= raycastAlphaThreshold);
            item.SetFocused(absD < 0.5f); // 只有中央那格用清晰圖，其餘用預先模糊好的圖

            sortOrder[i] = i;
            sortDistances[i] = absD;
        }

        ApplySiblingOrder();
    }

    /// <summary>越遠的卡片 siblingIndex 越低，中央永遠疊在最上面。</summary>
    private void ApplySiblingOrder()
    {
        // 插入排序（距離由遠到近）：卡片數量少且每次幾乎已排好，且完全不配置記憶體
        for (int i = 1; i < sortOrder.Length; i++)
        {
            int key = sortOrder[i];
            float keyDist = sortDistances[key];
            int j = i - 1;
            while (j >= 0 && sortDistances[sortOrder[j]] < keyDist)
            {
                sortOrder[j + 1] = sortOrder[j];
                j--;
            }
            sortOrder[j + 1] = key;
        }

        for (int k = 0; k < sortOrder.Length; k++)
        {
            ChapterCarouselItem item = items[sortOrder[k]];
            if (item != null) item.transform.SetSiblingIndex(k);
        }
    }

    private IEnumerator MoveRoutine(float target)
    {
        float start = offset;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            SetOffset(Mathf.LerpUnclamped(start, target, EaseOutCubic(t)));
            yield return null;
        }

        SetOffset(target);
        moveRoutine = null;
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    #endregion

    #region 選中狀態

    /// <summary>切換選中的章節，並連動大圖、文案、描邊與箭頭。</summary>
    private void ApplySelection(int index, bool instant)
    {
        bool changed = index != selectedIndex;
        selectedIndex = index;

        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null) items[i].SetSelected(items[i].Index == index);
        }

        if (heroView != null) heroView.Show(index, instant);

        if (metaView != null && chapterList != null)
        {
            metaView.Show(
                chapterList.GetDisplayTitle(index),
                chapterList.GetSubtitle(index),
                chapterList.GetSummary(index),
                instant);
        }

        UpdateArrows();

        if (changed || instant) onSelectionChanged?.Invoke(index);
    }

    private void UpdateArrows()
    {
        if (leftArrow != null) leftArrow.interactable = Count > 0 && selectedIndex > 0;
        if (rightArrow != null) rightArrow.interactable = Count > 0 && selectedIndex < Count - 1;
    }

    #endregion

    #region 互動

    private void HandleKeyboard()
    {
        if (!enableKeyboard) return;
        if (isPlayingChapter) return;
        if (panelRoot != null && !panelRoot.activeInHierarchy) return;

        if (Input.GetKeyDown(KeyCode.LeftArrow)) StepBy(-1);
        else if (Input.GetKeyDown(KeyCode.RightArrow)) StepBy(1);
    }

    /// <summary>卡片被點擊：非中央的移到中央，中央的（若開啟設定）直接開始劇情。</summary>
    private void HandleItemClicked(int index)
    {
        if (dragging) return;
        if (Time.unscaledTime < suppressClickUntil) return; // 拖曳剛結束，忽略殘留的點擊

        if (index == selectedIndex)
        {
            if (centerClickStartsChapter) PlayChapter(index);
            return;
        }
        GoTo(index, true);
    }

    /// <summary>拖曳開始（由 ChapterCarouselDragArea 轉發）。</summary>
    public void OnCarouselBeginDrag(PointerEventData eventData)
    {
        if (Count == 0) return;

        dragging = true;
        dragScreenDistance = 0f;
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
    }

    /// <summary>拖曳中（由 ChapterCarouselDragArea 轉發）：連續改變 offset。</summary>
    public void OnCarouselDrag(PointerEventData eventData)
    {
        if (!dragging || Count == 0 || eventData == null) return;

        float canvasScale = parentCanvas != null ? parentCanvas.scaleFactor : 1f;
        if (canvasScale <= 0f) canvasScale = 1f;

        dragScreenDistance += Mathf.Abs(eventData.delta.x);
        SetOffset(Mathf.Clamp(offset - eventData.delta.x / (step * canvasScale), 0f, Count - 1));

        // 拖曳過程中大圖與文案就跟著換，放開只負責吸附
        int nearest = Mathf.RoundToInt(offset);
        if (nearest != selectedIndex) ApplySelection(nearest, false);
    }

    /// <summary>拖曳結束（由 ChapterCarouselDragArea 轉發）：吸附到最近的整數格。</summary>
    public void OnCarouselEndDrag(PointerEventData eventData)
    {
        if (!dragging) return;

        dragging = false;
        if (dragScreenDistance > dragClickCancelDistance)
        {
            suppressClickUntil = Time.unscaledTime + 0.1f;
        }
        GoTo(Mathf.RoundToInt(offset), true);
    }

    #endregion

    #region 內部處理

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

    private void ClearItems()
    {
        items.Clear();
        if (moveRoutine != null)
        {
            StopCoroutine(moveRoutine);
            moveRoutine = null;
        }
        if (itemsRoot == null) return;

        for (int i = itemsRoot.childCount - 1; i >= 0; i--)
        {
            GameObject child = itemsRoot.GetChild(i).gameObject;
            child.transform.SetParent(null, false); // 先脫離父物件：Destroy 是延遲執行的，避免與新生成的卡片混在一起
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    #endregion
}
