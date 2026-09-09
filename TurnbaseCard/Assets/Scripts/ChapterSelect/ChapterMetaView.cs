using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// 章節文案區（MetaPanel）：標題 / 副標 / 描述。
/// 換章節時先淡出、換字、再淡入；連續切換時同一個協程會中途接手（不排隊）。
/// 容器高度由 Prefab 固定，描述文字以 Ellipsis 截斷，文案長短不會造成跳版。
/// </summary>
public class ChapterMetaView : MonoBehaviour
{
    [Header("組件")]
    [SerializeField]
    [Tooltip("整區文案的透明度控制。留空會自動補上。")]
    private CanvasGroup canvasGroup;
    [SerializeField]
    [Tooltip("章節標題文字。")]
    private TMP_Text titleText;
    [SerializeField]
    [Tooltip("章節副標文字。可為空。")]
    private TMP_Text subtitleText;
    [SerializeField]
    [Tooltip("章節描述文字（2～3 行）。可為空。")]
    private TMP_Text descriptionText;

    [Header("動效設定")]
    [SerializeField]
    [Tooltip("淡出（換字前）與淡入（換字後）各自的時長（秒）。")]
    [Min(0f)]
    private float fadeDuration = 0.17f;

    private Coroutine swapRoutine;

    private void Awake()
    {
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    /// <summary>換一組文案（淡出 → 換字 → 淡入）。</summary>
    public void Show(string title, string subtitle, string description)
    {
        Show(title, subtitle, description, false);
    }

    /// <summary>換一組文案；instant = true 時直接換掉不做淡化。</summary>
    public void Show(string title, string subtitle, string description, bool instant)
    {
        if (swapRoutine != null)
        {
            StopCoroutine(swapRoutine); // 中途接手：從當前透明度繼續，不排隊
            swapRoutine = null;
        }

        if (instant || fadeDuration <= 0f || !isActiveAndEnabled)
        {
            ApplyTexts(title, subtitle, description);
            if (canvasGroup != null) canvasGroup.alpha = 1f;
            return;
        }
        swapRoutine = StartCoroutine(SwapRoutine(title, subtitle, description));
    }

    private IEnumerator SwapRoutine(string title, string subtitle, string description)
    {
        yield return FadeTo(0f);
        ApplyTexts(title, subtitle, description);
        yield return FadeTo(1f);
        swapRoutine = null;
    }

    private IEnumerator FadeTo(float target)
    {
        if (canvasGroup == null) yield break;

        float start = canvasGroup.alpha;
        // 依實際差距縮短時間，避免中途接手時整段重跑一次完整長度
        float duration = fadeDuration * Mathf.Abs(target - start);
        if (duration <= 0f)
        {
            canvasGroup.alpha = target;
            yield break;
        }

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            canvasGroup.alpha = Mathf.Lerp(start, target, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        canvasGroup.alpha = target;
    }

    private void ApplyTexts(string title, string subtitle, string description)
    {
        if (titleText != null) titleText.text = title ?? string.Empty;

        // 副標 / 描述沒有內容時清空而不是隱藏物件，維持固定高度不跳版
        if (subtitleText != null) subtitleText.text = subtitle ?? string.Empty;
        if (descriptionText != null) descriptionText.text = description ?? string.Empty;
    }
}
