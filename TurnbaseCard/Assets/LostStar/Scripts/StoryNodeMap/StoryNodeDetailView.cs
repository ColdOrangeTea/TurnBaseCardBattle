using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 章節劇情節點圖右側的詳情面板：分類（分支 / 結局…）、編號 + 標題、段落劇情大綱，以及「閱讀」按鈕。
/// 選中節點時從畫面右側滑入並填入資料；取消選取（點背景）時向右滑出畫面外並關閉點擊。
/// 沒有可讀對話的節點（未解鎖或未指定對話表）閱讀鈕會變灰。
/// </summary>
public class StoryNodeDetailView : MonoBehaviour
{
    [Header("組件")]
    [SerializeField]
    [Tooltip("整片詳情面板的透明度 / 點擊控制。留空會自動補上。")]
    private CanvasGroup canvasGroup;
    [SerializeField]
    [Tooltip("上方分類文字（分支 / 分支結局 / 結局…）。")]
    private TMP_Text categoryText;
    [SerializeField]
    [Tooltip("編號 + 標題文字。")]
    private TMP_Text titleText;
    [SerializeField]
    [Tooltip("段落劇情大綱文字。")]
    private TMP_Text summaryText;
    [SerializeField]
    [Tooltip("「閱讀」按鈕。沒有可讀劇情時會設為不可互動。")]
    private Button readButton;

    [Header("滑動設定")]
    [SerializeField]
    [Tooltip("滑入 / 滑出的時長（秒）。")]
    [Min(0f)]
    private float slideDuration = 0.24f;
    [SerializeField]
    [Tooltip("滑出時額外往右移的距離（像素），確保連陰影一起移出畫面外。0 = 自動用面板寬度。")]
    [Min(0f)]
    private float extraSlide = 48f;

    private RectTransform rectTransform;
    private Vector2 shownPos;   // 版面上的定位（滑入的終點）
    private Vector2 hiddenPos;  // 畫面右外的位置（滑出的終點）
    private Coroutine slideRoutine;
    private bool initialized;

    /// <summary>「閱讀」按鈕（供主控在生成時取用綁定）。</summary>
    public Button ReadButton => readButton;

    private void Awake()
    {
        EnsureInit();
        // 起始為隱藏（滑到畫面外、淡出、不接點擊）
        ApplyImmediate(hiddenPos, 0f, false);
    }

    private void EnsureInit()
    {
        if (initialized) return;
        rectTransform = (RectTransform)transform;
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();

        shownPos = rectTransform.anchoredPosition;
        float slide = rectTransform.rect.width;
        if (slide <= 1f) slide = 700f; // 版面尚未計算出寬度時的保底值
        hiddenPos = shownPos + new Vector2(slide + extraSlide, 0f);
        initialized = true;
    }

    /// <summary>填入一個節點的資料並從右側滑入。</summary>
    /// <param name="category">分類文字（分支 / 結局…）</param>
    /// <param name="title">編號 + 標題</param>
    /// <param name="summary">段落劇情大綱</param>
    /// <param name="canRead">是否有可讀劇情（決定閱讀鈕是否可按）</param>
    /// <param name="instant">是否略過滑入動畫直接顯示</param>
    public void Show(string category, string title, string summary, bool canRead, bool instant = false)
    {
        EnsureInit();

        if (categoryText != null) categoryText.text = category ?? string.Empty;
        if (titleText != null) titleText.text = title ?? string.Empty;
        if (summaryText != null) summaryText.text = summary ?? string.Empty;
        if (readButton != null) readButton.interactable = canRead;

        SlideTo(shownPos, 1f, true, instant);
    }

    /// <summary>向右滑出畫面外並關閉點擊（取消選取時）。</summary>
    public void Hide(bool instant = false)
    {
        EnsureInit();
        SlideTo(hiddenPos, 0f, false, instant);
    }

    private void SlideTo(Vector2 targetPos, float targetAlpha, bool interactable, bool instant)
    {
        if (slideRoutine != null)
        {
            StopCoroutine(slideRoutine);
            slideRoutine = null;
        }

        // 淡出 / 不可按時馬上關掉點擊；淡入時等就定位再看 alpha（這裡直接開，滑入過程也允許點）
        canvasGroup.blocksRaycasts = interactable;
        canvasGroup.interactable = interactable;

        if (instant || slideDuration <= 0f || !isActiveAndEnabled)
        {
            ApplyImmediate(targetPos, targetAlpha, interactable);
            return;
        }
        slideRoutine = StartCoroutine(SlideRoutine(targetPos, targetAlpha));
    }

    private void ApplyImmediate(Vector2 pos, float alpha, bool interactable)
    {
        if (rectTransform != null) rectTransform.anchoredPosition = pos;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = alpha;
            canvasGroup.blocksRaycasts = interactable;
            canvasGroup.interactable = interactable;
        }
    }

    private IEnumerator SlideRoutine(Vector2 targetPos, float targetAlpha)
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        float startAlpha = canvasGroup.alpha;

        float elapsed = 0f;
        while (elapsed < slideDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = EaseOutCubic(Mathf.Clamp01(elapsed / slideDuration));
            rectTransform.anchoredPosition = Vector2.LerpUnclamped(startPos, targetPos, t);
            canvasGroup.alpha = Mathf.Lerp(startAlpha, targetAlpha, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetPos;
        canvasGroup.alpha = targetAlpha;
        slideRoutine = null;
    }

    private static float EaseOutCubic(float t)
    {
        float inv = 1f - t;
        return 1f - inv * inv * inv;
    }
}
