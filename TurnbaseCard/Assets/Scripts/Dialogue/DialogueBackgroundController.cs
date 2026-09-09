using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 對話背景控制器：放置 / 替換對話背景圖（由 DialogueData.backgroundImage 提供）。
///
/// 自適應原理（Cover 填滿模式）：
///   子物件 Image 掛 AspectRatioFitter（EnvelopeParent 模式），
///   依 Sprite 的原始長寬比「等比」放大到包住整個父區域——
///   視窗 / 解析度怎麼 Resize 都能填滿背景不露餡；
///   因為是等比縮放（不做非等比拉伸），圖像不會被壓扁 / 拉長而變形模糊，
///   超出畫面的部分由根物件的 RectMask2D 裁切。
/// </summary>
public class DialogueBackgroundController : MonoBehaviour
{
    [Header("顯示組件")]
    [SerializeField] private Image backgroundImage;        // 顯示背景用的 Image（子物件）
    [SerializeField] private AspectRatioFitter aspectFitter; // 子物件上的 AspectRatioFitter（EnvelopeParent）

    private void Awake()
    {
        // 尚未設定背景圖時隱藏，避免顯示白色色塊
        if (backgroundImage == null || backgroundImage.sprite == null)
        {
            Hide();
        }
        else
        {
            ApplyAspect(backgroundImage.sprite);
        }
    }

    /// <summary>
    /// 設定 / 替換背景圖。sprite 為 null 時隱藏背景。
    /// 會依 Sprite 原始長寬比更新 AspectRatioFitter，維持等比填滿。
    /// </summary>
    public void SetBackground(Sprite sprite)
    {
        if (backgroundImage == null) return;

        if (sprite == null)
        {
            Hide();
            return;
        }

        backgroundImage.sprite = sprite;
        ApplyAspect(sprite);
        Show();
    }

    /// <summary>依 Sprite 的原始像素尺寸更新長寬比（等比縮放的關鍵）。</summary>
    private void ApplyAspect(Sprite sprite)
    {
        if (aspectFitter == null || sprite == null) return;
        aspectFitter.aspectRatio = sprite.rect.width / Mathf.Max(1f, sprite.rect.height);
    }

    /// <summary>隱藏背景。</summary>
    public void Hide()
    {
        gameObject.SetActive(false);
    }

    /// <summary>顯示背景。</summary>
    public void Show()
    {
        gameObject.SetActive(true);
    }
}
