using UnityEngine;

/// <summary>
/// 章節選擇（ChapterCarouselController）↔ 章節劇情節點圖（StoryNodeMapController）的連接元件。
///
/// 流程：在章節選擇畫面選定大章節 → 打開該章節對應的節點圖（不直接播放對話）；
/// 在節點圖按「返回章節選擇」 → 回到章節選擇畫面。
///
/// 章節與節點圖的對應「直接讀章節列表資訊表 (ChapterListData)」：
/// 每個大章節 (ChapterEntry) 身上就掛著自己的 StoryNodeGraphData，因此本元件不需要另外維護一份對照表，
/// 選到第幾章就去該章節的 nodeGraph 取用即可。
///
/// 使用方式：把本元件掛在場景任一常駐物件上（或 EventSystem 上），指定 carousel 與 nodeMap。
/// 其餘接線由本元件在執行期自動完成：
///   ‧ 自動把 carousel 切成「交給外部接手」（不在輪播那邊直接播對話）
///   ‧ 訂閱 carousel.onChapterChosen → 依 ChapterListData 找到該章節的節點圖並開啟
///   ‧ 訂閱 nodeMap.onReturn → 回到章節選擇畫面
///
/// 此元件是由 A_Good_Ink 使用 AI 生成的工具。
/// </summary>
public class ChapterStoryMapBridge : MonoBehaviour
{
    [Header("兩端畫面")]
    [SerializeField]
    [Tooltip("章節選擇畫面的主控。留空會自動在場景中尋找。")]
    private ChapterCarouselController carousel;
    [SerializeField]
    [Tooltip("章節劇情節點圖的主控。留空會自動在場景中尋找。")]
    private StoryNodeMapController nodeMap;

    [Header("章節資料")]
    [SerializeField]
    [Tooltip("章節列表資訊表；由此依章節索引取得該章節的節點圖。留空 = 用 carousel 目前的章節列表。")]
    private ChapterListData chapterList;
    [SerializeField]
    [Tooltip("章節本身沒指定節點圖時使用的備援節點圖（可留空）。")]
    private StoryNodeGraphData fallbackGraph;

    [Header("啟動設定")]
    [SerializeField]
    [Tooltip("啟動時自動把章節選擇畫面切成「交給外部接手」（選章不直接播對話，改開節點圖）。")]
    private bool forceDeferOnCarousel = true;
    [SerializeField]
    [Tooltip("啟動時先隱藏節點圖畫面，等玩家選了章節才開啟。")]
    private bool hideNodeMapOnStart = true;

    private bool subscribed;

    private void Awake()
    {
        if (carousel == null) carousel = FindFirstObjectByType<ChapterCarouselController>(FindObjectsInactive.Include);
        if (nodeMap == null) nodeMap = FindFirstObjectByType<StoryNodeMapController>(FindObjectsInactive.Include);

        if (carousel == null)
            Debug.LogWarning($"{name} 的 ChapterStoryMapBridge 找不到 ChapterCarouselController，選章不會開啟節點圖。");
        if (nodeMap == null)
            Debug.LogWarning($"{name} 的 ChapterStoryMapBridge 找不到 StoryNodeMapController，選章不會開啟節點圖。");

        if (forceDeferOnCarousel && carousel != null) carousel.SetDeferChapterToExternal(true);
    }

    private void OnEnable()
    {
        Subscribe();
    }

    private void Start()
    {
        // 在所有 OnEnable 之後才關節點圖：節點圖 OnEnable 會先建好一次，這裡再收起來等選章
        if (hideNodeMapOnStart && nodeMap != null) nodeMap.Close();
    }

    private void OnDisable()
    {
        Unsubscribe();
    }

    private void Subscribe()
    {
        if (subscribed) return;
        if (carousel != null && carousel.onChapterChosen != null) carousel.onChapterChosen.AddListener(OpenForChapter);
        if (nodeMap != null && nodeMap.onReturn != null) nodeMap.onReturn.AddListener(BackToCarousel);
        subscribed = true;
    }

    private void Unsubscribe()
    {
        if (!subscribed) return;
        if (carousel != null && carousel.onChapterChosen != null) carousel.onChapterChosen.RemoveListener(OpenForChapter);
        if (nodeMap != null && nodeMap.onReturn != null) nodeMap.onReturn.RemoveListener(BackToCarousel);
        subscribed = false;
    }

    /// <summary>選定某章節：依章節資料找到該章節的節點圖並開啟。</summary>
    public void OpenForChapter(int chapterIndex)
    {
        ChapterListData list = chapterList != null ? chapterList
            : (carousel != null ? carousel.ChapterList : null);

        StoryNodeGraphData graph = list != null ? list.GetNodeGraph(chapterIndex) : null;
        if (graph == null) graph = fallbackGraph;

        if (graph == null)
        {
            Debug.LogWarning($"[ChapterStoryMapBridge] 章節索引 {chapterIndex} 的章節沒有指定 nodeGraph（且無備援），已退回章節選擇畫面。" +
                             "請在章節列表資訊表 (ChapterListData) 的該章節上指定劇情節點圖。");
            if (carousel != null) carousel.ReturnToChapterSelect();
            return;
        }
        if (nodeMap == null)
        {
            Debug.LogWarning("[ChapterStoryMapBridge] 缺少 StoryNodeMapController，無法開啟節點圖。");
            return;
        }

        nodeMap.Open(graph);
    }

    /// <summary>從節點圖返回章節選擇畫面。</summary>
    public void BackToCarousel()
    {
        if (carousel != null) carousel.ReturnToChapterSelect();
    }
}
