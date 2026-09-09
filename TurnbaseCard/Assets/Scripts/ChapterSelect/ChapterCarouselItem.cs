using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 章節輪播的單張直式縮圖卡片（ChapterThumbnail Prefab）。
/// 位置 / 縮放 / 亮度 / 透明度全部由 ChapterCarouselController 依浮點 offset 算好後推進來，
/// 卡片本身只負責套用外觀、切換選中態（描邊 + 光暈）與回報點擊。
///
/// 景深模糊：uGUI 無法即時 blur，未置中的卡片改用「預先模糊好的 Sprite」，
/// 由 ChapterListData.blurredImage 提供；沒提供時就一直用清晰圖。
/// </summary>
public class ChapterCarouselItem : MonoBehaviour, IPointerClickHandler
{
    [Header("組件")]
    [SerializeField]
    [Tooltip("卡片的透明度控制（由輪播器依距離設定 alpha）。留空會自動補上。")]
    private CanvasGroup canvasGroup;
    [SerializeField]
    [Tooltip("顯示章節縮圖的 Image。")]
    private Image thumbnailImage;
    [SerializeField]
    [Tooltip("選中時啟用的高亮描邊。可為空。")]
    private GameObject selectedOutline;
    [SerializeField]
    [Tooltip("選中時啟用的微光暈。可為空。")]
    private GameObject selectedGlow;
    [SerializeField]
    [Tooltip("未解鎖時啟用的鎖定遮罩。可為空。")]
    private GameObject lockedMark;
    [SerializeField]
    [Tooltip("卡片上的章節編號文字。可為空。")]
    private TMP_Text numberText;
    [SerializeField]
    [Tooltip("要套用亮度乘算的圖（留空會自動使用縮圖本身）。亮度是乘在各自原本的顏色上，不會把深色底板洗成灰色。")]
    private Graphic[] tintTargets;

    [Header("外觀")]
    [SerializeField]
    [Tooltip("章節沒有指定縮圖時，縮圖區顯示的佔位顏色。")]
    private Color placeholderColor = new Color(0.32f, 0.34f, 0.42f, 1f);

    /// <summary>此卡片對應的章節索引。</summary>
    public int Index { get; private set; } = -1;

    private RectTransform rectTransform;
    private Color[] baseColors;             // 各 tintTarget 的原始顏色（亮度以乘算套用其上）
    private Sprite sharpSprite;             // 清晰版縮圖
    private Sprite blurredSprite;           // 預先模糊好的版本（可為 null）
    private bool usingBlurred;              // 目前顯示的是否為模糊版（避免每幀重設 sprite）
    private bool isSelected;
    private Action<int> onClicked;

    private void Awake()
    {
        rectTransform = (RectTransform)transform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (thumbnailImage == null) thumbnailImage = GetComponent<Image>();
        if (tintTargets == null || tintTargets.Length == 0)
        {
            tintTargets = thumbnailImage != null ? new Graphic[] { thumbnailImage } : Array.Empty<Graphic>();
        }

        // 記下原始顏色，亮度才能「乘」上去而不是把顏色整個換掉
        baseColors = new Color[tintTargets.Length];
        for (int i = 0; i < tintTargets.Length; i++)
        {
            baseColors[i] = tintTargets[i] != null ? tintTargets[i].color : Color.white;
        }
    }

    /// <summary>更新某個 tintTarget 的原始顏色（亮度會以此為基準乘算）。</summary>
    private void SetBaseColor(Graphic graphic, Color color)
    {
        if (graphic == null) return;
        graphic.color = color;
        if (baseColors == null) return;

        for (int i = 0; i < tintTargets.Length; i++)
        {
            if (tintTargets[i] == graphic) baseColors[i] = color;
        }
    }

    /// <summary>
    /// 綁定此卡片要顯示的章節內容（由輪播器在生成卡片後呼叫）。
    /// </summary>
    /// <param name="index">章節索引</param>
    /// <param name="thumbnail">清晰版縮圖，可為 null</param>
    /// <param name="blurred">預先模糊好的縮圖，可為 null</param>
    /// <param name="numberLabel">卡片上要顯示的編號文字，可為空</param>
    /// <param name="unlocked">是否已解鎖</param>
    /// <param name="onClicked">點擊回呼，會帶回章節索引</param>
    public void Bind(int index, Sprite thumbnail, Sprite blurred, string numberLabel,
        bool unlocked, Action<int> onClicked)
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;

        Index = index;
        sharpSprite = thumbnail;
        blurredSprite = blurred;
        usingBlurred = false;
        this.onClicked = onClicked;

        if (thumbnailImage != null)
        {
            thumbnailImage.sprite = sharpSprite;
            thumbnailImage.enabled = true;
            // 有圖就用白色（不染色）顯示原圖，沒圖則退回佔位色，不會變成刺眼的白塊
            SetBaseColor(thumbnailImage, sharpSprite != null ? Color.white : placeholderColor);
        }
        else
        {
            Debug.LogWarning($"{name} 的 ChapterCarouselItem 缺少 thumbnailImage 引用，章節縮圖無法顯示。");
        }

        if (numberText != null)
        {
            bool hasLabel = !string.IsNullOrEmpty(numberLabel);
            numberText.gameObject.SetActive(hasLabel);
            if (hasLabel) numberText.text = numberLabel;
        }

        if (lockedMark != null) lockedMark.SetActive(!unlocked);

        SetSelected(false);
    }

    /// <summary>
    /// 套用輪播器算好的版面（由輪播器每幀呼叫）。
    /// </summary>
    /// <param name="x">anchoredPosition.x</param>
    /// <param name="scale">縮放</param>
    /// <param name="brightness">亮度（乘算到 tintTargets 的顏色上）</param>
    /// <param name="alpha">CanvasGroup.alpha（不夾下限，遠處直接溶進背景）</param>
    /// <param name="blocksRaycasts">是否接收點擊</param>
    public void ApplyLayout(float x, float scale, float brightness, float alpha, bool blocksRaycasts)
    {
        if (rectTransform == null) rectTransform = (RectTransform)transform;

        Vector2 pos = rectTransform.anchoredPosition;
        pos.x = x;
        rectTransform.anchoredPosition = pos;
        rectTransform.localScale = new Vector3(scale, scale, 1f);

        for (int i = 0; i < tintTargets.Length; i++)
        {
            Graphic g = tintTargets[i];
            if (g == null) continue;
            Color b = baseColors != null && i < baseColors.Length ? baseColors[i] : Color.white;
            // 亮度乘算在原始顏色上（透明度交給 CanvasGroup，不在這裡動）
            g.color = new Color(b.r * brightness, b.g * brightness, b.b * brightness, b.a);
        }

        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = blocksRaycasts;
        }
    }

    /// <summary>切換選中態（描邊 + 光暈只在選中時啟用）。</summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedOutline != null) selectedOutline.SetActive(selected);
        if (selectedGlow != null) selectedGlow.SetActive(selected);
    }

    /// <summary>
    /// 切換清晰 / 模糊版縮圖（景深）。沒有提供模糊版時永遠維持清晰圖。
    /// </summary>
    public void SetFocused(bool focused)
    {
        if (thumbnailImage == null || blurredSprite == null) return;

        bool wantBlurred = !focused;
        if (wantBlurred == usingBlurred) return; // 狀態沒變就不動 sprite，避免每幀重建網格

        usingBlurred = wantBlurred;
        thumbnailImage.sprite = wantBlurred ? blurredSprite : sharpSprite;
    }

    /// <summary>目前是否為選中卡片。</summary>
    public bool IsSelected => isSelected;

    /// <summary>
    /// 卡片點擊（由子物件的 Graphic 冒泡上來）。
    /// 這裡刻意不使用 Button：拖曳結束時 Button 仍會送出 onClick，
    /// 改由輪播器在收到點擊時判斷剛才是否正在拖曳。
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        NotifyClicked();
    }

    /// <summary>對外的點擊入口（也可讓外部按鈕或測試直接呼叫）。</summary>
    public void NotifyClicked()
    {
        if (Index < 0) return;
        onClicked?.Invoke(Index);
    }
}
