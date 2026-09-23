using System;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Dialogue;
using Assets.Scripts.StoryNodeMap;

/// <summary>
/// 章節劇情節點圖資訊表：一個大章節內所有劇情節點（段落 / 分支 / 結局）與其連線的完整資料（ScriptableObject）。
/// 由 Assets 右鍵 → Create → Dialogue → 章節劇情節點圖資訊表 建立。
///
/// 這份資料驅動「選完大章節後，挑選該章節哪一段劇情來讀」的節點圖畫面：
/// 每個節點有編號 (id)、標題、段落劇情大綱、類型（主線 / 分支 / 分支結局 / 結局）、
/// 在圖上的座標，以及要播放的對話資訊表 (DialogueData)；節點之間以 connections（索引）連線。
///
/// 新增 / 調整劇情節點時只要改這份資料（加節點、連線、補大綱與對話表），不必改程式。
/// </summary>
[CreateAssetMenu(fileName = "NewStoryNodeGraph", menuName = "SO/Dialogue/章節劇情節點圖資訊表 (StoryNodeGraphData)")]
public class StoryNodeGraphData : ScriptableObject
{
    /// <summary>單一劇情節點的資料。</summary>
    [Serializable]
    public class StoryNode
    {
        [Tooltip("節點編號，顯示在標題前面，例如「1-3」「1-4-1」。可留空。")]
        public string id = "1-1";

        [Tooltip("節點標題。留空則詳情面板只顯示編號。")]
        public string title;

        [Tooltip("段落劇情大綱，顯示在詳情面板中段（可留空）。")]
        [TextArea(2, 5)]
        public string summary;

        [Tooltip("節點類型：主線 / 分支 / 分支結局 / 結局。決定詳情面板上方的分類文字。")]
        public StoryNodeType type = StoryNodeType.MainLine;

        [Tooltip("自訂分類文字（留空 = 依節點類型自動顯示，例如「分支」「結局」）。")]
        public string categoryOverride;

        [Tooltip("節點在圖上的座標（像素）：x 往右為正、y 以畫面中央為 0 往上為正。橫向捲動時整張圖左右移動。")]
        public Vector2 graphPosition;

        [Tooltip("此節點按下「閱讀」時要播放的對話資訊表 (DialogueData)。留空 = 沒有可讀劇情，閱讀鈕會變灰。")]
        public DialogueData dialogueData;

        [Tooltip("是否已解鎖。未解鎖的節點會變暗，且無法閱讀。")]
        public bool unlocked = true;

        [Tooltip("此節點連往哪些節點（填其他節點在下方清單中的索引）。連線只畫一次，方向不影響外觀。")]
        public List<int> connections = new List<int>();

        [Tooltip("此節點專用的節點 Prefab（留空 = 使用主控上設定的預設節點 Prefab）。")]
        public GameObject customNodePrefab;
    }

    [Tooltip("節點圖畫面上方黑邊要顯示的標題，例如「(1) 本章節」。")]
    public string mapTitle = "(數字)本章節";

    [Tooltip("詳情面板標題的格式，{0} = 編號、{1} = 標題。例：「{0}　{1}」。")]
    public string nodeTitleFormat = "{0}　{1}";

    [Tooltip("劇情節點清單。索引即 connections 參照的編號。")]
    public List<StoryNode> nodes = new List<StoryNode>();

    /// <summary>節點總數。</summary>
    public int NodeCount => nodes != null ? nodes.Count : 0;

    /// <summary>取得指定索引的節點；越界回傳 null。</summary>
    public StoryNode GetNode(int index)
    {
        if (nodes == null || index < 0 || index >= nodes.Count) return null;
        return nodes[index];
    }

    /// <summary>取得詳情面板要顯示的標題（編號 + 標題）。</summary>
    public string GetNodeTitle(int index)
    {
        StoryNode node = GetNode(index);
        if (node == null) return string.Empty;

        bool hasId = !string.IsNullOrEmpty(node.id);
        bool hasTitle = !string.IsNullOrEmpty(node.title);

        if (hasId && hasTitle) return SafeFormat(nodeTitleFormat, "{0}　{1}", node.id, node.title);
        if (hasId) return node.id;
        return hasTitle ? node.title : string.Empty;
    }

    /// <summary>取得詳情面板上方的分類文字（節點自訂優先，否則依類型）。</summary>
    public string GetCategory(int index)
    {
        StoryNode node = GetNode(index);
        if (node == null) return string.Empty;
        return string.IsNullOrEmpty(node.categoryOverride)
            ? DefaultCategoryLabel(node.type)
            : node.categoryOverride;
    }

    /// <summary>取得段落劇情大綱（未解鎖節點不顯示，避免劇透）。</summary>
    public string GetSummary(int index)
    {
        StoryNode node = GetNode(index);
        if (node == null || !node.unlocked) return string.Empty;
        return node.summary;
    }

    /// <summary>節點是否已解鎖。</summary>
    public bool IsUnlocked(int index)
    {
        StoryNode node = GetNode(index);
        return node != null && node.unlocked;
    }

    /// <summary>
    /// 檢查指定節點是否可閱讀（存在、已解鎖、有對話資訊表），並回傳可播放的節點與不可時的中文原因。
    /// </summary>
    public bool TryGetReadableNode(int index, out StoryNode node, out string reason)
    {
        node = GetNode(index);
        if (node == null)
        {
            reason = $"節點索引 {index} 超出節點圖範圍。";
            return false;
        }
        if (!node.unlocked)
        {
            reason = $"節點「{GetNodeTitle(index)}」尚未解鎖。";
            return false;
        }
        if (node.dialogueData == null)
        {
            reason = $"節點「{GetNodeTitle(index)}」尚未指定對話資訊表 (DialogueData)，沒有劇情可讀。";
            return false;
        }
        reason = string.Empty;
        return true;
    }

    /// <summary>節點類型對應的預設分類文字。</summary>
    public static string DefaultCategoryLabel(StoryNodeType type)
    {
        switch (type)
        {
            case StoryNodeType.Branch: return "分支";
            case StoryNodeType.BranchEnding: return "分支結局";
            case StoryNodeType.Ending: return "結局";
            case StoryNodeType.MainLine:
            default: return "本章節";
        }
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
            Debug.LogWarning($"[StoryNodeGraphData] 格式字串「{used}」的佔位符有誤，已改用預設格式。");
            return string.Format(fallback, args);
        }
    }
}
