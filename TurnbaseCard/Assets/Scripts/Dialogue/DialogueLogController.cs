using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 對話紀錄（Log）控制器：由上到下記錄「說話者名稱 + 對白」，可用滾輪 / 拖曳捲動觀看。
/// 由 DialogueTypingEffect 在每行對白開始顯示時呼叫 AddEntry()，開始新對話時呼叫 Clear()。
/// </summary>
public class DialogueLogController : MonoBehaviour
{
    [Header("組件")]
    [SerializeField] private ScrollRect scrollRect;     // 捲動視圖
    [SerializeField] private RectTransform contentRoot; // 紀錄條目的父物件（掛 VerticalLayoutGroup）
    [SerializeField] private GameObject entryPrefab;    // 單筆紀錄的 Prefab（含 TMP_Text）

    [Header("外觀")]
    [SerializeField] private string nameColorHex = "#F2D388"; // 說話者名稱的預設顯示顏色（未指定顏色時使用）

    private readonly List<GameObject> entries = new List<GameObject>();

    private TMP_FontAsset entryFontOverride; // 對話資訊表指定的字型（null = 使用 entryPrefab 原本的字型）

    /// <summary>
    /// 設定紀錄條目使用的字型（由 DialogueTypingEffect 依對話資訊表指定）。
    /// 傳入 null 表示使用 entryPrefab 原本的字型。
    /// </summary>
    public void SetEntryFont(TMP_FontAsset font)
    {
        entryFontOverride = font;
    }

    private void OnEnable()
    {
        // 開啟面板時捲到最新一筆（等 Layout 計算完成後）
        StartCoroutine(ScrollToBottomNextFrame());
    }

    /// <summary>新增一筆紀錄（說話者名稱 + 對白），名稱使用預設顏色 nameColorHex。</summary>
    public void AddEntry(string speakerName, string text)
    {
        Color nameColor;
        if (!ColorUtility.TryParseHtmlString(nameColorHex, out nameColor)) nameColor = Color.white;
        AddEntry(speakerName, text, nameColor);
    }

    /// <summary>新增一筆紀錄（說話者名稱 + 對白），名稱使用指定顏色（人物主題色）。</summary>
    public void AddEntry(string speakerName, string text, Color nameColor)
    {
        if (entryPrefab == null || contentRoot == null) return;

        var go = Instantiate(entryPrefab, contentRoot);
        var tmp = go.GetComponent<TMP_Text>();
        if (tmp != null)
        {
            if (entryFontOverride != null) tmp.font = entryFontOverride;
            tmp.text = string.IsNullOrEmpty(speakerName)
                ? text
                : $"<color=#{ColorUtility.ToHtmlStringRGB(nameColor)}>{speakerName}</color>\n{text}";
        }
        entries.Add(go);

        // 面板開著時新增紀錄，跟著捲到最底
        if (isActiveAndEnabled)
        {
            StartCoroutine(ScrollToBottomNextFrame());
        }
    }

    /// <summary>清空所有紀錄（開始新對話時呼叫）。</summary>
    public void Clear()
    {
        foreach (var entry in entries)
        {
            if (entry != null) Destroy(entry);
        }
        entries.Clear();
    }

    private IEnumerator ScrollToBottomNextFrame()
    {
        yield return null; // 等一幀讓 VerticalLayoutGroup / ContentSizeFitter 完成高度計算
        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 0f; // 0 = 最底部（最新一筆）
        }
    }
}
