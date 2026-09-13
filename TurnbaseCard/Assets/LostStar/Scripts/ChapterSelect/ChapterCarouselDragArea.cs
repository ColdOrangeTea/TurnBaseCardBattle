using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 縮圖輪播的拖曳感應區（掛在 SelectorPanel 上）。
/// 輪播主控 ChapterCarouselController 掛在 Canvas 根物件上（面板隱藏時仍要運作），
/// 因此拖曳事件由這個小元件在面板上接收後轉發過去。
/// 需要同物件上有一個 raycastTarget = true 的 Graphic 才收得到事件（產生工具會鋪一張全透明 Image）。
/// </summary>
public class ChapterCarouselDragArea : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField]
    [Tooltip("要轉發拖曳事件的輪播主控。留空會自動往父物件尋找。")]
    private ChapterCarouselController carousel;

    private void Awake()
    {
        if (carousel == null) carousel = GetComponentInParent<ChapterCarouselController>();
        if (carousel == null)
        {
            Debug.LogWarning($"{name} 的 ChapterCarouselDragArea 找不到 ChapterCarouselController，拖曳將無作用。");
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (carousel != null) carousel.OnCarouselBeginDrag(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (carousel != null) carousel.OnCarouselDrag(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (carousel != null) carousel.OnCarouselEndDrag(eventData);
    }
}
