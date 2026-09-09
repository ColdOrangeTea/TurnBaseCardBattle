using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 單一章節按鈕（ChapterButton Prefab）的控制器：
/// 顯示「第幾章 + 標題」、章節簡介與縮圖，並在點擊時把自己的索引回報給 ChapterSelectController。
/// 未解鎖的章節會變暗、顯示鎖定標記且無法點擊。
/// </summary>
public class ChapterButtonController : MonoBehaviour
{
    [Header("組件")]
    [SerializeField] private Button button;            // 按鈕本體
    [SerializeField] private TMP_Text titleText;       // 標題文字（第幾章 + 標題）
    [SerializeField] private TMP_Text summaryText;     // 章節簡介（可為空）
    [SerializeField] private Image thumbnailImage;     // 章節縮圖（可為空）
    [SerializeField] private GameObject lockedMark;    // 未解鎖標記（可為空）

    [Header("外觀")]
    [SerializeField] private Color unlockedTextColor = Color.white;                      // 已解鎖的文字顏色
    [SerializeField] private Color lockedTextColor = new Color(1f, 1f, 1f, 0.35f);       // 未解鎖的文字顏色

    /// <summary>此按鈕對應的章節索引（在 ChapterListData.chapters 中的位置）。</summary>
    public int Index { get; private set; } = -1;

    /// <summary>此章節是否已解鎖。</summary>
    public bool Unlocked { get; private set; } = true;

    private Action<int> onClicked; // 點擊回呼（由 ChapterSelectController 注入）

    private void Awake()
    {
        // 允許 Prefab 未在 Inspector 指定引用時自動補齊，避免整顆按鈕失效
        if (button == null) button = GetComponent<Button>();
        if (titleText == null) titleText = GetComponentInChildren<TMP_Text>(true);
    }

    /// <summary>
    /// 設定此按鈕的內容與點擊行為（由 ChapterSelectController 於生成按鈕後呼叫）。
    /// </summary>
    /// <param name="index">章節索引</param>
    /// <param name="displayTitle">顯示文字（第幾章 + 標題）</param>
    /// <param name="summary">章節簡介，可為空</param>
    /// <param name="thumbnail">章節縮圖，可為 null</param>
    /// <param name="unlocked">是否已解鎖</param>
    /// <param name="onClicked">點擊回呼，會帶回章節索引</param>
    public void Setup(int index, string displayTitle, string summary, Sprite thumbnail,
        bool unlocked, Action<int> onClicked)
    {
        Index = index;
        Unlocked = unlocked;
        this.onClicked = onClicked;

        if (titleText != null)
        {
            titleText.text = displayTitle;
            titleText.color = unlocked ? unlockedTextColor : lockedTextColor;
        }
        else
        {
            Debug.LogWarning($"{name} 的 ChapterButtonController 缺少 titleText 引用，章節標題無法顯示。");
        }

        // 簡介：沒有內容就整個隱藏，讓版面自動縮回
        if (summaryText != null)
        {
            bool hasSummary = !string.IsNullOrEmpty(summary);
            summaryText.gameObject.SetActive(hasSummary);
            if (hasSummary) summaryText.text = summary;
        }

        // 縮圖：沒有圖就隱藏縮圖區
        if (thumbnailImage != null)
        {
            bool hasThumbnail = thumbnail != null;
            thumbnailImage.gameObject.SetActive(hasThumbnail);
            if (hasThumbnail) thumbnailImage.sprite = thumbnail;
        }

        if (lockedMark != null) lockedMark.SetActive(!unlocked);

        // 點擊綁定：先清掉舊的（按鈕可能被重複利用）再綁新的
        if (button != null)
        {
            button.onClick.RemoveListener(HandleClick);
            button.onClick.AddListener(HandleClick);
            button.interactable = unlocked;
        }
        else
        {
            Debug.LogWarning($"{name} 的 ChapterButtonController 缺少 Button 組件，章節按鈕無法點擊。");
        }
    }

    private void OnDestroy()
    {
        if (button != null) button.onClick.RemoveListener(HandleClick);
    }

    private void HandleClick()
    {
        if (!Unlocked) return;      // 未解鎖：保險起見再擋一次
        if (Index < 0) return;      // 尚未 Setup
        onClicked?.Invoke(Index);
    }
}
