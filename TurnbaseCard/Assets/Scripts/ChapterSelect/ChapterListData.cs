using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Assets.Scripts.ChapterSelect;
using Assets.Scripts.Dialogue;

/// <summary>
/// 章節列表資訊表：一份章節選單的完整資料（ScriptableObject）。
/// 由 Assets 右鍵 → Create → Dialogue → 章節列表資訊表 建立。
///
/// 每個章節可設定：章節編號（自動依順序 / 手動指定）、標題、對應的對話資訊表 (DialogueData)、
/// 是否已解鎖、章節縮圖與簡介。
/// 按鈕上顯示的文字由 numberFormat + titleFormat 組出，例如「第 1 章　旅途的起點」。
/// </summary>
[CreateAssetMenu(fileName = "NewChapterListData", menuName = "Dialogue/章節列表資訊表 (ChapterListData)")]
public class ChapterListData : ScriptableObject
{
    /// <summary>單一章節的資料。</summary>
    [Serializable]
    public class ChapterEntry
    {
        [Tooltip("勾選 = 章節編號依列表順序自動計算（第 1、2、3… 章）；取消 = 使用下方手動指定的編號。")]
        public bool useAutoNumber = true;

        [Tooltip("手動指定的章節編號（僅在取消自動編號時生效）。可用於序章 0、外傳等特殊編號。")]
        public int chapterNumber = 1;

        [Tooltip("章節標題，例如「旅途的起點」。留空則按鈕只顯示章節編號。")]
        public string title;

        [Tooltip("章節副標，顯示在標題下方（可留空）。")]
        public string subtitle;

        [Tooltip("此章節要播放的對話資訊表 (DialogueData)。留空 = 點擊後不會播放任何劇情。適用於「選章即直接播一段對話」的章節。")]
        public DialogueData dialogueData;

        [Tooltip("此大章節的劇情節點圖 (StoryNodeGraphData)：內含此章節一段一段的小節點。\n" +
                 "適用於「選章後先進節點圖挑劇情」的章節；連接元件 (ChapterStoryMapBridge) 會直接用它找到對應的節點圖。")]
        public StoryNodeGraphData nodeGraph;

        [Tooltip("是否已解鎖。未解鎖的章節按鈕會變暗、顯示鎖定標記且無法點擊。")]
        public bool unlocked = true;

        [Tooltip("章節縮圖（可留空；留空時按鈕上的縮圖區會自動隱藏，輪播卡片則改用大圖）。")]
        public Sprite thumbnail;

        [Tooltip("大圖預覽用的主視覺（留空 = 沿用縮圖）。")]
        public Sprite heroImage;

        [Tooltip("全螢幕主題背景圖（章節選擇畫面最底層純黑背景之上、切換章節時淡入淡出）。\n" +
                 "留空 = 依序沿用 blurredImage → heroImage → thumbnail，未填也不會破圖。")]
        public Sprite backgroundImage;

        [Tooltip("預先模糊好的圖，用於大圖背景與未選中卡片的景深（留空 = 不啟用景深）。uGUI 無法即時 blur，請放事先模糊過的圖。")]
        public Sprite blurredImage;

        [Tooltip("此章節專用的縮圖卡片 Prefab（留空 = 使用輪播器上設定的預設卡片 Prefab）。新增章節時只要補上 Prefab 就能換外觀，不必改程式。")]
        public GameObject customCardPrefab;

        [Tooltip("章節簡介，顯示在標題下方（可留空；留空時簡介文字會自動隱藏）。")]
        [TextArea(1, 3)]
        public string summary;
    }

    [Tooltip("這份章節列表的劇情類型（主線 / 支線 / 其他）。")]
    public DialogueType chapterType = DialogueType.MainStory;

    [Tooltip("章節選擇畫面的標題文字。")]
    public string menuTitle = "章節選擇";

    [Tooltip("章節編號的顯示樣式：阿拉伯數字（第 1 章）或中文數字（第一章）。")]
    public ChapterNumberStyle numberStyle = ChapterNumberStyle.Arabic;

    [Tooltip("章節編號的格式，{0} = 編號文字。例：「第 {0} 章」。")]
    public string numberFormat = "第 {0} 章";

    [Tooltip("按鈕文字的格式，{0} = 編號文字、{1} = 章節標題。例：「{0}　{1}」。")]
    public string titleFormat = "{0}　{1}";

    [Tooltip("未解鎖章節顯示的替代標題（編號仍會正常顯示）。")]
    public string lockedTitle = "？？？";

    [Tooltip("章節列表，由上到下依序顯示。")]
    public List<ChapterEntry> chapters = new List<ChapterEntry>();

    /// <summary>章節總數。</summary>
    public int ChapterCount => chapters != null ? chapters.Count : 0;

    /// <summary>取得指定索引的章節；越界回傳 null。</summary>
    public ChapterEntry GetChapter(int index)
    {
        if (chapters == null || index < 0 || index >= chapters.Count) return null;
        return chapters[index];
    }

    /// <summary>取得指定章節的劇情節點圖 (StoryNodeGraphData)；未指定或越界回傳 null。</summary>
    public StoryNodeGraphData GetNodeGraph(int index)
    {
        ChapterEntry entry = GetChapter(index);
        return entry != null ? entry.nodeGraph : null;
    }

    /// <summary>此章節是否有可進入的劇情節點圖。</summary>
    public bool HasNodeGraph(int index)
    {
        return GetNodeGraph(index) != null;
    }

    /// <summary>取得指定索引章節的編號（自動編號時 = 索引 + 1）。</summary>
    public int GetChapterNumber(int index)
    {
        ChapterEntry entry = GetChapter(index);
        if (entry == null) return index + 1;
        return entry.useAutoNumber ? index + 1 : entry.chapterNumber;
    }

    /// <summary>取得按鈕上要顯示的文字（第幾章 + 標題）。</summary>
    public string GetDisplayTitle(int index)
    {
        ChapterEntry entry = GetChapter(index);
        if (entry == null) return string.Empty;

        // 編號文字：依樣式轉成阿拉伯或中文數字，再套用 numberFormat
        int number = GetChapterNumber(index);
        string numberText = numberStyle == ChapterNumberStyle.Chinese
            ? ToChineseNumber(number)
            : number.ToString();
        string numberPart = SafeFormat(numberFormat, "第 {0} 章", numberText);

        // 標題：未解鎖時以 lockedTitle 取代
        string titleText = entry.unlocked ? entry.title : lockedTitle;
        if (string.IsNullOrEmpty(titleText)) return numberPart;

        return SafeFormat(titleFormat, "{0}　{1}", numberPart, titleText);
    }

    /// <summary>取得副標文字（未解鎖章節不顯示，避免劇透）。</summary>
    public string GetSubtitle(int index)
    {
        ChapterEntry entry = GetChapter(index);
        if (entry == null || !entry.unlocked) return string.Empty;
        return entry.subtitle;
    }

    /// <summary>取得大圖預覽用的主視覺（未指定時沿用縮圖，可能為 null）。</summary>
    public Sprite GetHeroSprite(int index)
    {
        ChapterEntry entry = GetChapter(index);
        if (entry == null) return null;
        return entry.heroImage != null ? entry.heroImage : entry.thumbnail;
    }

    /// <summary>
    /// 取得全螢幕主題背景圖：優先用專用的 backgroundImage，
    /// 未指定時依序退回 blurredImage → heroImage → thumbnail（可能為 null）。
    /// </summary>
    public Sprite GetBackgroundSprite(int index)
    {
        ChapterEntry entry = GetChapter(index);
        if (entry == null) return null;
        if (entry.backgroundImage != null) return entry.backgroundImage;
        if (entry.blurredImage != null) return entry.blurredImage;
        if (entry.heroImage != null) return entry.heroImage;
        return entry.thumbnail;
    }

    /// <summary>取得縮圖（未指定時沿用大圖，可能為 null）。</summary>
    public Sprite GetThumbnailSprite(int index)
    {
        ChapterEntry entry = GetChapter(index);
        if (entry == null) return null;
        return entry.thumbnail != null ? entry.thumbnail : entry.heroImage;
    }

    /// <summary>取得預先模糊好的圖（未提供時回傳 null，呼叫端應自行退回清晰圖）。</summary>
    public Sprite GetBlurredSprite(int index)
    {
        ChapterEntry entry = GetChapter(index);
        return entry != null ? entry.blurredImage : null;
    }

    /// <summary>
    /// 檢查指定章節是否可以播放（存在、已解鎖、有對話資訊表），
    /// 供各種章節選擇畫面共用同一套判斷與提示訊息。
    /// </summary>
    /// <param name="index">章節索引</param>
    /// <param name="entry">可播放時回傳該章節資料</param>
    /// <param name="reason">不可播放時的中文原因說明</param>
    public bool TryGetPlayableChapter(int index, out ChapterEntry entry, out string reason)
    {
        entry = GetChapter(index);
        if (entry == null)
        {
            reason = $"章節索引 {index} 超出章節列表範圍。";
            return false;
        }
        if (!entry.unlocked)
        {
            reason = $"章節「{GetDisplayTitle(index)}」尚未解鎖。";
            return false;
        }
        if (entry.dialogueData == null)
        {
            reason = $"章節「{GetDisplayTitle(index)}」尚未指定對話資訊表 (DialogueData)，沒有劇情可播放。";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    /// <summary>取得簡介文字（未解鎖章節不顯示簡介，避免劇透）。</summary>
    public string GetSummary(int index)
    {
        ChapterEntry entry = GetChapter(index);
        if (entry == null || !entry.unlocked) return string.Empty;
        return entry.summary;
    }

    /// <summary>string.Format 的防禦版：格式字串為空或含錯誤佔位符時退回預設格式。</summary>
    private static string SafeFormat(string format, string fallback, params object[] args)
    {
        string used = string.IsNullOrEmpty(format) ? fallback : format;
        try
        {
            return string.Format(used, args);
        }
        catch (FormatException)
        {
            Debug.LogWarning($"[ChapterListData] 格式字串「{used}」的佔位符有誤，已改用預設格式。");
            return string.Format(fallback, args);
        }
    }

    /// <summary>
    /// 將整數轉為中文數字（支援 0 ~ 9999，例：15 → 十五、203 → 二百零三）。
    /// 超出支援範圍或負數時直接回傳阿拉伯數字，不會丟出例外。
    /// </summary>
    public static string ToChineseNumber(int value)
    {
        string[] digits = { "零", "一", "二", "三", "四", "五", "六", "七", "八", "九" };
        string[] units = { "", "十", "百", "千" };

        if (value < 0 || value > 9999) return value.ToString();
        if (value == 0) return digits[0];

        string source = value.ToString();
        var sb = new StringBuilder();
        bool zeroPending = false; // 中間出現 0 時，補一個「零」（連續 0 只補一次）

        for (int i = 0; i < source.Length; i++)
        {
            int digit = source[i] - '0';
            int unitIndex = source.Length - 1 - i;

            if (digit == 0)
            {
                zeroPending = sb.Length > 0;
                continue;
            }
            if (zeroPending)
            {
                sb.Append(digits[0]);
                zeroPending = false;
            }
            // 「一十五」習慣寫成「十五」：十位為 1 且是最高位時省略「一」
            if (!(digit == 1 && unitIndex == 1 && sb.Length == 0))
            {
                sb.Append(digits[digit]);
            }
            sb.Append(units[unitIndex]);
        }
        return sb.ToString();
    }
}
