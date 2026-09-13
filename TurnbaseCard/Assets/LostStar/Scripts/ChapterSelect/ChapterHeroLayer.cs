using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 章節大圖預覽的單一圖層（ChapterHeroLayer Prefab）。
/// 純粹一張 Image + AspectRatioFitter：不加遮罩、不加漸層、不加底色板。
/// 由 ChapterHeroView 依章節數量生成並重複利用（切換章節列表時回收再用，不重新建物件）。
///
/// 切圖時把該張 Sprite 的比例寫進 AspectRatioFitter（aspectRatio = 寬 / 高），
/// 絕不各自去改 width / height——那才會破圖。
/// 尺寸適配模式（FitInParent / EnvelopeParent）由 ChapterHeroView 統一指定並套進 fitter。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class ChapterHeroLayer : MonoBehaviour
{
    [Header("組件")]
    [SerializeField]
    [Tooltip("這一層的透明度控制（交叉淡化用）。留空會自動補上。")]
    private CanvasGroup canvasGroup;
    [SerializeField]
    [Tooltip("這一層唯一的大圖 Image。留空會自動補上。")]
    private Image image;
    [SerializeField]
    [Tooltip("依 Sprite 比例縮放大圖的 AspectRatioFitter；絕不各自去改 width / height。留空會自動補上。")]
    private AspectRatioFitter fitter;

    /// <summary>此圖層目前對應的章節索引（-1 = 未使用）。</summary>
    public int Index { get; private set; } = -1;

    private void Awake()
    {
        EnsureComponents();
    }

    /// <summary>這一層的透明度。</summary>
    public float Alpha
    {
        get
        {
            EnsureComponents();
            return canvasGroup != null ? canvasGroup.alpha : 1f;
        }
        set
        {
            EnsureComponents();
            if (canvasGroup != null) canvasGroup.alpha = value;
        }
    }

    /// <summary>設定這一層的尺寸適配模式（由 ChapterHeroView 統一指定）。</summary>
    public void SetFitMode(AspectRatioFitter.AspectMode mode)
    {
        EnsureComponents();
        if (fitter != null) fitter.aspectMode = mode;
    }

    /// <summary>
    /// 設定這一層要顯示的章節圖（由 ChapterHeroView 呼叫）。
    /// 會把 Sprite 的實際比例寫進 AspectRatioFitter，讓 Fit / Envelope 依比例縮放而不破圖。
    /// </summary>
    /// <param name="index">章節索引</param>
    /// <param name="sprite">大圖主視覺，可為 null</param>
    /// <param name="placeholderColor">沒有主視覺時的佔位顏色</param>
    public void SetChapter(int index, Sprite sprite, Color placeholderColor)
    {
        EnsureComponents();

        Index = index;
        name = $"HeroLayer_{index}";

        if (image != null)
        {
            image.sprite = sprite;
            // 沒有大圖時用佔位色填滿，不要出現破圖或空洞
            image.color = sprite != null ? Color.white : placeholderColor;
            // 比例交給 AspectRatioFitter；Image 自己不要再 preserveAspect（會重複套用）
            image.preserveAspect = false;
        }
        else
        {
            Debug.LogWarning($"{name} 的 ChapterHeroLayer 缺少 image 引用，大圖無法顯示。");
        }

        if (fitter != null)
        {
            // aspectRatio = 該張 Sprite 的寬 / 高；沒有圖時給安全值 1，避免除以 0
            fitter.aspectRatio = sprite != null && sprite.rect.height > 0f
                ? sprite.rect.width / sprite.rect.height
                : 1f;
        }
    }

    /// <summary>回收這一層（隱藏並清掉圖，等待下次重用）。</summary>
    public void Release()
    {
        EnsureComponents();

        Index = -1;
        name = "HeroLayer_Pooled";
        Alpha = 0f;
        if (image != null) image.sprite = null;
        gameObject.SetActive(false);
    }

    /// <summary>缺少引用時就地補齊，避免 Prefab 沒接好就整層失效。</summary>
    private void EnsureComponents()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        if (image == null) image = GetComponent<Image>();
        if (image == null) image = gameObject.AddComponent<Image>();
        if (fitter == null) fitter = GetComponent<AspectRatioFitter>();
        if (fitter == null) fitter = gameObject.AddComponent<AspectRatioFitter>();
    }
}
