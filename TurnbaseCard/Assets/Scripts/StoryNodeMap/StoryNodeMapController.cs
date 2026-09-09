using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 「選完大章節後，挑選該章節哪一段劇情來讀」的節點圖畫面主控。
///
/// 上方黑邊放章節標題、下方黑邊放「返回章節選擇」按鈕，中間是一張可橫向捲動的劇情節點圖：
/// 節點（圓點）依 StoryNodeGraphData 的座標排列、節點間以連線相接；點某個節點會在右側淡入
/// 詳情面板（分類 / 編號標題 / 段落劇情大綱 / 閱讀鈕），按「閱讀」即播放該節點的對話。
///
/// 整張圖由 StoryNodeGraphData 驅動：改節點、連線、大綱與對話表都在資料裡調，不必改程式。
/// 對外用 Open(graph) 進入、Btn_Return() 返回；播放與返回都以 UnityEvent 對外，方便和章節選擇畫面接線。
/// </summary>
public class StoryNodeMapController : MonoBehaviour
{
    /// <summary>帶節點索引的事件（供 Inspector 綁定額外行為，如音效、存檔）。</summary>
    [Serializable]
    public class NodeIndexEvent : UnityEvent<int> { }

    [Header("資料")]
    [SerializeField]
    [Tooltip("要顯示的章節劇情節點圖資訊表。可在執行期用 Open() / SetGraph() 換成其他章節的節點圖。")]
    private StoryNodeGraphData graph;

    [Header("畫面區塊")]
    [SerializeField]
    [Tooltip("整個節點圖畫面的根物件；播放劇情時會隱藏它。留空則使用本物件。")]
    private GameObject panelRoot;
    [SerializeField]
    [Tooltip("上方黑邊的章節標題文字。")]
    private TMP_Text titleText;
    [SerializeField]
    [Tooltip("橫向捲動用的 ScrollRect。")]
    private ScrollRect scrollRect;
    [SerializeField]
    [Tooltip("節點與連線的容器（ScrollRect 的 content；錨點需為左側、樞紐 x = 0）。")]
    private RectTransform content;
    [SerializeField]
    [Tooltip("連線的容器（content 底下、排在節點之前，讓線畫在節點下方）。錨點 (0, 0.5)、樞紐 (0, 0.5)、尺寸 0。")]
    private RectTransform edgesRoot;
    [SerializeField]
    [Tooltip("右側詳情面板。")]
    private StoryNodeDetailView detailView;

    [Header("節點生成")]
    [SerializeField]
    [Tooltip("預設的節點 Prefab（需含 StoryNodeItem）。節點可在資料表用 customNodePrefab 各自覆寫。")]
    private GameObject defaultNodePrefab;

    [Header("連線外觀")]
    [SerializeField]
    [Tooltip("連線用的圖（留空會用純色矩形填滿）。")]
    private Sprite edgeSprite;
    [SerializeField]
    [Tooltip("連線顏色。")]
    private Color edgeColor = new Color(0.05f, 0.05f, 0.07f, 1f);
    [SerializeField]
    [Tooltip("連線粗細（像素）。")]
    [Min(1f)]
    private float edgeThickness = 7f;

    [Header("版面")]
    [SerializeField]
    [Tooltip("最右節點之後保留的捲動邊距（像素），讓最後的節點也能捲進視野。")]
    [Min(0f)]
    private float contentRightPadding = 360f;

    [Header("點節點置中視野")]
    [SerializeField]
    [Tooltip("點節點時把視野移到以該節點為中心（在可捲動範圍內盡量置中）。")]
    private bool focusOnSelect = true;
    [SerializeField]
    [Tooltip("置中時視野中心往左偏移的量（像素），讓節點落在未被右側詳情面板遮住的區域中央。約為詳情面板寬度的一半。")]
    private float viewFocusRightInset = 330f;
    [SerializeField]
    [Tooltip("同時在垂直方向置中（讓上 / 下分支的節點也移到畫面中央）。")]
    private bool centerVertically = true;
    [SerializeField]
    [Tooltip("視野移動的時長（秒），使用 EaseOutCubic。")]
    [Min(0f)]
    private float focusDuration = 0.35f;

    [Header("互動設定")]
    [SerializeField]
    [Tooltip("進入畫面時是否自動選中第一個節點並展開詳情面板（關 = 一開始不顯示面板，等玩家點節點）。")]
    private bool selectFirstOnOpen = false;
    [SerializeField]
    [Tooltip("面板啟用時依資料重建節點圖（節點解鎖狀態變動會即時反映）。")]
    private bool rebuildOnEnable = true;
    [SerializeField]
    [Tooltip("按「閱讀」開始播放時隱藏本畫面，避免與對話 UI 疊在一起。")]
    private bool hidePanelOnPlay = true;
    [SerializeField]
    [Tooltip("對話結束後自動回到節點圖畫面。")]
    private bool reopenAfterDialogue = true;
    [SerializeField]
    [Tooltip("按「返回章節選擇」時隱藏本畫面（交給 onReturn 綁定的對象開啟章節選擇畫面）。")]
    private bool hidePanelOnReturn = true;

    [Header("對話系統")]
    [SerializeField]
    [Tooltip("對話 UI 的總控制器；按閱讀後由它開啟對話 UI 並開始播放。")]
    private TriggerDialogue dialogueTrigger;
    [SerializeField]
    [Tooltip("對話打字機；未指定 dialogueTrigger 時的備援播放對象。")]
    private DialogueTypingEffect dialogueTyper;

    [Header("事件接口")]
    [Tooltip("選中的節點改變時觸發，參數為節點索引。")]
    public NodeIndexEvent onNodeSelected;
    [Tooltip("按閱讀、開始播放某節點劇情時觸發，參數為節點索引。")]
    public NodeIndexEvent onReadStarted;
    [Tooltip("按「返回章節選擇」時觸發（綁定重新開啟章節選擇畫面）。")]
    public UnityEvent onReturn;
    [Tooltip("從劇情返回節點圖畫面時觸發。")]
    public UnityEvent onReturnFromDialogue;

    private readonly List<StoryNodeItem> items = new List<StoryNodeItem>();
    private readonly List<Vector2> nodePositions = new List<Vector2>();
    private readonly List<GameObject> edgeObjects = new List<GameObject>();

    private int selectedIndex = -1;
    private bool isPlaying;
    private bool built;
    private bool eventSubscribed;
    private Coroutine focusRoutine;   // 視野置中的協程（重啟而非排隊）

    /// <summary>目前選中的節點索引（-1 = 未選）。</summary>
    public int SelectedIndex => selectedIndex;

    /// <summary>目前使用的節點圖資訊表。</summary>
    public StoryNodeGraphData Graph => graph;

    /// <summary>節點總數。</summary>
    public int Count => graph != null ? graph.NodeCount : 0;

    private void Awake()
    {
        if (panelRoot == null) panelRoot = gameObject;
        EnsureBackgroundHandler();
    }

    /// <summary>
    /// 確保 ScrollRect 的 Viewport 上有背景點擊感應（點空白處取消選取）。
    /// 舊的 Prefab 沒有這個元件時會在執行期自動補上，不必重跑產生工具。
    /// </summary>
    private void EnsureBackgroundHandler()
    {
        RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;
        if (viewport == null) return;

        var handler = viewport.GetComponent<StoryNodeMapBackground>();
        if (handler == null) handler = viewport.gameObject.AddComponent<StoryNodeMapBackground>();
        handler.Setup(this);
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

    #region 對外接口

    /// <summary>換一份節點圖資訊表並開啟本畫面（章節選擇畫面選定大章節後的入口）。</summary>
    public void Open(StoryNodeGraphData data)
    {
        SetGraph(data);
        if (panelRoot != null) panelRoot.SetActive(true);
    }

    /// <summary>更換節點圖資訊表並重建整張圖。</summary>
    public void SetGraph(StoryNodeGraphData data)
    {
        graph = data;
        selectedIndex = -1;
        Build();
    }

    /// <summary>依目前的節點圖資訊表重建節點與連線。</summary>
    public void Build()
    {
        ClearItems();
        built = true;

        if (titleText != null) titleText.text = graph != null ? graph.mapTitle : string.Empty;

        if (content == null)
        {
            Debug.LogWarning($"{name} 的 StoryNodeMapController 缺少 content（節點容器），無法生成節點圖。");
            return;
        }
        if (graph == null)
        {
            Debug.LogWarning($"{name} 的 StoryNodeMapController 尚未指定節點圖資訊表 (StoryNodeGraphData)，畫面為空。");
            if (detailView != null) detailView.Hide(true);
            return;
        }

        BuildNodes();
        BuildEdges();
        ResizeContent();

        // 回到最左端、垂直置中的起點
        if (scrollRect != null) scrollRect.StopMovement();
        content.anchoredPosition = Vector2.zero;

        if (detailView != null) detailView.Hide(true);
        selectedIndex = -1;

        if (selectFirstOnOpen && Count > 0) SelectNode(0, true);
    }

    /// <summary>選中某個節點（展開右側詳情面板）。</summary>
    public void SelectNode(int index)
    {
        SelectNode(index, false);
    }

    /// <summary>開始閱讀目前選中的節點（可綁在詳情面板的「閱讀」按鈕上）。</summary>
    public void Btn_ReadSelected()
    {
        if (selectedIndex < 0)
        {
            Debug.Log("尚未選擇任何劇情節點，無法閱讀。");
            return;
        }
        ReadNode(selectedIndex);
    }

    /// <summary>開始播放指定節點的劇情。</summary>
    public void ReadNode(int index)
    {
        if (isPlaying) return; // 播放中，忽略重複觸發
        if (graph == null) return;

        if (!graph.TryGetReadableNode(index, out StoryNodeGraphData.StoryNode node, out string reason))
        {
            Debug.Log(reason);
            return;
        }

        isPlaying = true;
        onReadStarted?.Invoke(index);

        if (hidePanelOnPlay && panelRoot != null) panelRoot.SetActive(false);

        if (dialogueTrigger != null)
        {
            dialogueTrigger.PlayDialogue(node.dialogueData);
            return;
        }
        if (dialogueTyper != null)
        {
            dialogueTyper.SetDialogueData(node.dialogueData);
            dialogueTyper.ToStartDialogue();
            return;
        }

        isPlaying = false;
        if (hidePanelOnPlay && panelRoot != null) panelRoot.SetActive(true);
        Debug.LogWarning($"{name} 的 StoryNodeMapController 尚未綁定 TriggerDialogue 或 DialogueTypingEffect，無法跳轉到劇情。");
    }

    /// <summary>返回章節選擇畫面（綁在「返回章節選擇」按鈕上）。</summary>
    public void Btn_Return()
    {
        if (hidePanelOnReturn && panelRoot != null) panelRoot.SetActive(false);
        onReturn?.Invoke();
    }

    /// <summary>重新開啟本節點圖畫面（供外部從劇情返回時呼叫）。</summary>
    public void ReturnToNodeMap()
    {
        isPlaying = false;
        if (panelRoot != null) panelRoot.SetActive(true);
        onReturnFromDialogue?.Invoke();
    }

    /// <summary>隱藏本節點圖畫面（供連接元件在尚未選章節時關閉）。</summary>
    public void Close()
    {
        if (panelRoot != null) panelRoot.SetActive(false);
    }

    #endregion

    #region 生成節點與連線

    private void BuildNodes()
    {
        for (int i = 0; i < graph.NodeCount; i++)
        {
            StoryNodeGraphData.StoryNode node = graph.GetNode(i);
            if (node == null)
            {
                items.Add(null);
                nodePositions.Add(Vector2.zero);
                continue;
            }

            GameObject prefab = node.customNodePrefab != null ? node.customNodePrefab : defaultNodePrefab;
            if (prefab == null)
            {
                Debug.LogWarning($"節點「{graph.GetNodeTitle(i)}」沒有可用的節點 Prefab（預設與自訂皆為空），已略過。");
                items.Add(null);
                nodePositions.Add(node.graphPosition);
                continue;
            }

            GameObject go = Instantiate(prefab, content, false);
            go.name = $"StoryNode_{i}";
            go.SetActive(true);

            var rt = (RectTransform)go.transform;
            rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f); // 以 content 左緣中央為原點
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = node.graphPosition;

            var item = go.GetComponent<StoryNodeItem>();
            if (item == null)
            {
                Debug.LogWarning($"節點 Prefab「{prefab.name}」缺少 StoryNodeItem，節點「{graph.GetNodeTitle(i)}」無法運作。");
                items.Add(null);
                nodePositions.Add(node.graphPosition);
                continue;
            }

            item.Bind(i, node.unlocked, HandleNodeClicked);
            items.Add(item);
            nodePositions.Add(node.graphPosition);
        }
    }

    /// <summary>依節點連線關係畫線；同一對節點只畫一次。</summary>
    private void BuildEdges()
    {
        if (edgesRoot == null) return;

        var drawn = new HashSet<long>();
        for (int i = 0; i < graph.NodeCount; i++)
        {
            StoryNodeGraphData.StoryNode node = graph.GetNode(i);
            if (node == null || node.connections == null) continue;

            foreach (int j in node.connections)
            {
                if (j < 0 || j >= graph.NodeCount || j == i) continue;

                int a = Mathf.Min(i, j);
                int b = Mathf.Max(i, j);
                long key = ((long)a << 32) | (uint)b;
                if (!drawn.Add(key)) continue; // 這對節點已經畫過

                CreateEdge(nodePositions[i], nodePositions[j]);
            }
        }
    }

    private void CreateEdge(Vector2 from, Vector2 to)
    {
        var go = new GameObject("Edge", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(edgesRoot, false);

        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);

        Vector2 mid = (from + to) * 0.5f;
        float length = Vector2.Distance(from, to);
        float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

        rt.anchoredPosition = mid;
        rt.sizeDelta = new Vector2(length, edgeThickness);
        rt.localRotation = Quaternion.Euler(0f, 0f, angle);

        var img = go.GetComponent<Image>();
        img.sprite = edgeSprite;
        img.type = Image.Type.Simple;
        img.color = edgeColor;
        img.raycastTarget = false;

        edgeObjects.Add(go);
    }

    /// <summary>依最右節點決定 content 寬度，讓最後的節點也能捲到（未被詳情面板遮住的）視野中央。</summary>
    private void ResizeContent()
    {
        float maxX = 0f;
        for (int i = 0; i < nodePositions.Count; i++)
        {
            if (nodePositions[i].x > maxX) maxX = nodePositions[i].x;
        }

        RectTransform viewport = scrollRect != null ? scrollRect.viewport : null;
        float viewportWidth = viewport != null ? viewport.rect.width : 0f;

        // 右側要保留的捲動空間：取「使用者設定的邊距」與「讓最右節點也能置中所需的空間」較大者。
        // 置中焦點在 focusX = 視野中心往左偏 viewFocusRightInset 處；要讓最右節點捲到 focusX，
        // 內容需比它再往右延伸 (viewportWidth - focusX) = 視野右半 + 偏移量，多留 40 邊距。
        float rightPadding = contentRightPadding;
        if (focusOnSelect && viewportWidth > 0f)
        {
            float focusX = viewportWidth * 0.5f - viewFocusRightInset;
            rightPadding = Mathf.Max(rightPadding, viewportWidth - focusX + 40f);
        }

        float width = maxX + rightPadding;
        // 至少和視窗一樣寬，ScrollRect 才不會出現負向捲動或抖動
        if (viewportWidth > 0f) width = Mathf.Max(width, viewportWidth);

        content.sizeDelta = new Vector2(width, content.sizeDelta.y);
    }

    #endregion

    #region 選中與互動

    private void SelectNode(int index, bool instant)
    {
        if (graph == null || index < 0 || index >= Count) return;

        selectedIndex = index;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null) items[i].SetSelected(items[i].Index == index);
        }

        if (detailView != null)
        {
            bool canRead = graph.TryGetReadableNode(index, out _, out _);
            detailView.Show(graph.GetCategory(index), graph.GetNodeTitle(index), graph.GetSummary(index), canRead, instant);
        }

        if (focusOnSelect) FocusOnNode(index, instant);

        onNodeSelected?.Invoke(index);
    }

    /// <summary>取消目前的節點選取：熄滅發光、右側詳情面板滑出畫面外（可綁在背景點擊上）。</summary>
    public void DeselectNode()
    {
        if (selectedIndex < 0) return;

        selectedIndex = -1;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] != null) items[i].SetSelected(false);
        }
        if (detailView != null) detailView.Hide();
    }

    private void HandleNodeClicked(int index)
    {
        SelectNode(index, false);
    }

    /// <summary>把視野移到以指定節點為中心（水平留出右側詳情面板的空間，可選同時垂直置中）。</summary>
    private void FocusOnNode(int index, bool instant)
    {
        if (content == null || scrollRect == null) return;

        RectTransform viewport = scrollRect.viewport;
        if (viewport == null) return;

        float viewportWidth = viewport.rect.width;
        if (viewportWidth <= 0f) return; // 版面尚未計算，略過（避免算出錯誤的定位）

        Vector2 nodePos = index >= 0 && index < nodePositions.Count ? nodePositions[index] : Vector2.zero;

        // 水平：讓節點落在未被詳情面板遮住區域的中央；並夾在可捲動範圍內
        float focusX = viewportWidth * 0.5f - viewFocusRightInset;
        float desiredX = focusX - nodePos.x;

        // 確保 content 夠寬，最右節點才捲得到 focusX（不會被詳情面板擋住）。
        // 若 Build 當下視野寬度尚未算出（初次啟用），這裡在點擊時（版面已就緒）補足。
        float contentWidth = content.sizeDelta.x;
        float requiredWidth = viewportWidth - desiredX + 40f; // desiredX 為負時代表要往左捲，需更寬的內容
        if (contentWidth < requiredWidth)
        {
            contentWidth = requiredWidth;
            content.sizeDelta = new Vector2(contentWidth, content.sizeDelta.y);
        }

        float minX = Mathf.Min(0f, viewportWidth - contentWidth);
        float targetX = Mathf.Clamp(desiredX, minX, 0f);

        // 垂直：把節點的 y 偏移抵銷掉（content 垂直置中，anchoredPosition.y = -nodeY 即置中）
        float targetY = centerVertically ? -nodePos.y : content.anchoredPosition.y;

        var target = new Vector2(targetX, targetY);

        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }

        scrollRect.StopMovement();

        if (instant || focusDuration <= 0f || !isActiveAndEnabled)
        {
            content.anchoredPosition = target;
            return;
        }
        focusRoutine = StartCoroutine(FocusRoutine(target));
    }

    private IEnumerator FocusRoutine(Vector2 target)
    {
        Vector2 start = content.anchoredPosition;
        float elapsed = 0f;
        while (elapsed < focusDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / focusDuration));
            content.anchoredPosition = Vector2.LerpUnclamped(start, target, t);
            yield return null;
        }
        content.anchoredPosition = target;
        focusRoutine = null;
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }

    #endregion

    #region 內部處理

    private void HandleDialogueClosed()
    {
        if (!isPlaying) return;
        isPlaying = false;
        if (!reopenAfterDialogue) return;
        ReturnToNodeMap();
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
        nodePositions.Clear();

        if (focusRoutine != null)
        {
            StopCoroutine(focusRoutine);
            focusRoutine = null;
        }

        // 連線與節點都掛在 content / edgesRoot 底下，直接清掉子物件
        ClearChildren(edgesRoot);
        edgeObjects.Clear();

        if (content != null)
        {
            for (int i = content.childCount - 1; i >= 0; i--)
            {
                Transform child = content.GetChild(i);
                if (edgesRoot != null && child == edgesRoot) continue; // 保留連線容器本身
                DestroyChild(child.gameObject);
            }
        }
    }

    private void ClearChildren(RectTransform root)
    {
        if (root == null) return;
        for (int i = root.childCount - 1; i >= 0; i--)
        {
            DestroyChild(root.GetChild(i).gameObject);
        }
    }

    private void DestroyChild(GameObject child)
    {
        child.transform.SetParent(null, false); // Destroy 是延遲的，先脫離避免與新生成的混在一起
        if (Application.isPlaying) Destroy(child);
        else DestroyImmediate(child);
    }

    #endregion
}
