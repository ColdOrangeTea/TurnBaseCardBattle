using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Dialogue;

/// <summary>
/// ADV 式人物立繪控制器：置換顯示的人物圖像。
/// 一般流程由 DialogueLine 直接指定 Sprite（SetPortrait(Sprite)，
/// 立繪來源為人物風格資訊表 CharacterStyleData 或自定義）。
/// 亦保留舊的「說話者 + 表情」查表模式（Inspector 的 portraits 清單）。
/// </summary>
public class CharacterPortraitController : MonoBehaviour
{
    /// <summary>單一立繪登錄項：角色 + 表情 對應一張 Sprite。</summary>
    [Serializable]
    public class PortraitEntry
    {
        public DialogueUnitType unit;              // 說話者
        public CharacterExpressionType expression; // 表情
        public Sprite sprite;                      // 對應的立繪圖
    }

    [Header("顯示組件")]
    [SerializeField] private Image portraitImage;  // 顯示立繪用的 Image
    [SerializeField] private GameObject placeholder; // 尚未設定立繪時顯示的佔位提示（可為空）

    [Header("立繪登錄表")]
    [SerializeField] private List<PortraitEntry> portraits = new List<PortraitEntry>();

    [Header("備用圖")]
    [SerializeField] private Sprite defaultSprite; // 找不到對應立繪時的備用圖（可為空）

    [Header("大小 / 位置")]
    [Tooltip("立繪大小 = Sprite 原始像素尺寸 × 此倍率（各張圖大小不一時自動跟著原圖調整）。")]
    [SerializeField][Min(0.01f)] private float portraitScale = 1f;

    private Vector2 defaultAnchoredPosition; // Prefab 原本的位置（未指定自訂位置時還原用）
    private bool defaultPosCaptured;

    private void Awake()
    {
        if (transform is RectTransform rt)
        {
            defaultAnchoredPosition = rt.anchoredPosition;
            defaultPosCaptured = true;
        }
    }

    /// <summary>
    /// 直接以 Sprite 置換立繪（由 DialogueLine 指定立繪時使用）。
    /// sprite 為 null 時退回 defaultSprite；仍為 null 則維持現狀（不清空畫面）。
    /// 大小會依 Sprite 原始像素尺寸自動調整（× portraitScale）。
    /// </summary>
    public void SetPortrait(Sprite sprite)
    {
        if (portraitImage == null) return;

        if (sprite == null) sprite = defaultSprite;
        if (sprite == null) return; // 找不到就維持現狀

        portraitImage.sprite = sprite;
        portraitImage.color = Color.white; // 蓋掉佔位用的半透明色
        ApplyNativeSize(sprite);
        if (placeholder != null) placeholder.SetActive(false);
        Show();
    }

    /// <summary>設定立繪位置（RectTransform 的 anchoredPosition）。</summary>
    public void SetPosition(Vector2 anchoredPosition)
    {
        if (transform is RectTransform rt)
        {
            rt.anchoredPosition = anchoredPosition;
        }
    }

    /// <summary>還原立繪為 Prefab 原本的位置。</summary>
    public void ResetPosition()
    {
        if (defaultPosCaptured && transform is RectTransform rt)
        {
            rt.anchoredPosition = defaultAnchoredPosition;
        }
    }

    /// <summary>依 Sprite 原始像素尺寸調整立繪大小（各張立繪大小不一時跟著原圖走，不會被拉伸變形）。</summary>
    private void ApplyNativeSize(Sprite sprite)
    {
        if (portraitImage == null || sprite == null) return;
        portraitImage.rectTransform.sizeDelta = sprite.rect.size * portraitScale;
    }

    /// <summary>
    /// 依說話者與表情置換立繪。
    /// 查找順序：完全符合 → 同角色的 Noexpression → 同角色任一張 → defaultSprite。
    /// 全部找不到時維持現狀（不清空畫面）。
    /// </summary>
    public void SetPortrait(DialogueUnitType unit, CharacterExpressionType expression)
    {
        if (portraitImage == null) return;

        Sprite sprite = FindSprite(unit, expression)
                        ?? FindSprite(unit, CharacterExpressionType.Noexpression)
                        ?? FindFirstSpriteOfUnit(unit)
                        ?? defaultSprite;

        if (sprite == null) return; // 找不到就維持現狀

        portraitImage.sprite = sprite;
        portraitImage.color = Color.white; // 蓋掉佔位用的半透明色
        ApplyNativeSize(sprite);
        if (placeholder != null) placeholder.SetActive(false);
        Show();
    }

    /// <summary>隱藏立繪（例如旁白、無人說話時）。</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>顯示立繪。</summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }

    private Sprite FindSprite(DialogueUnitType unit, CharacterExpressionType expression)
    {
        foreach (var entry in portraits)
        {
            if (entry.unit == unit && entry.expression == expression && entry.sprite != null)
                return entry.sprite;
        }
        return null;
    }

    private Sprite FindFirstSpriteOfUnit(DialogueUnitType unit)
    {
        foreach (var entry in portraits)
        {
            if (entry.unit == unit && entry.sprite != null)
                return entry.sprite;
        }
        return null;
    }
}
