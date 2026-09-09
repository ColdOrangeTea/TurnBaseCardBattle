using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 節點圖的背景點擊感應（掛在 ScrollRect 的 Viewport 上）。
/// 點到節點時，點擊會被節點自己（StoryNodeItem）接走，不會冒泡到這裡；
/// 點到空白背景（或連線等非互動處）才會走到這裡，用來取消目前的節點選取。
/// 拖曳捲動不算點擊，因此拖曳不會誤取消選取。
///
/// 需要同物件上有一個 raycastTarget = true 的 Graphic 才收得到事件（產生工具會鋪一張全透明 Image）。
/// </summary>
public class StoryNodeMapBackground : MonoBehaviour, IPointerClickHandler
{
    [SerializeField]
    [Tooltip("要通知取消選取的節點圖主控。留空會自動往父物件尋找。")]
    private StoryNodeMapController controller;

    private void Awake()
    {
        if (controller == null) controller = GetComponentInParent<StoryNodeMapController>();
    }

    /// <summary>由主控指定要通知的對象（執行期自動補件時使用）。</summary>
    public void Setup(StoryNodeMapController owner)
    {
        controller = owner;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller != null) controller.DeselectNode();
    }
}
