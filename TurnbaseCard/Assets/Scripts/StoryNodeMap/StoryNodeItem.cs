using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 章節劇情節點圖上的單一節點（圓點，StoryNode Prefab）。
/// 外觀由主控 StoryNodeMapController 依資料設定；節點本身只負責切換選中態（光暈 + 換色）、
/// 反映解鎖狀態，並在被點擊時回報自己的索引。
///
/// 這裡刻意不使用 Button：節點放在可橫向捲動的 ScrollRect 內，
/// 用 IPointerClickHandler 收點擊，捲動時的拖曳會自然被判定為非點擊而不誤觸。
/// </summary>
public class StoryNodeItem : MonoBehaviour, IPointerClickHandler
{
    [Header("組件")]
    [SerializeField]
    [Tooltip("節點圓點的主要 Image（會依選中 / 未解鎖切換顏色）。留空會自動使用本物件上的 Image。")]
    private Image nodeImage;
    [SerializeField]
    [Tooltip("選中時啟用的柔光暈。可為空。")]
    private GameObject selectedGlow;

    [Header("外觀顏色")]
    [SerializeField]
    [Tooltip("一般（未選中、已解鎖）節點顏色。")]
    private Color normalColor = new Color(0.62f, 0.62f, 0.64f, 1f);
    [SerializeField]
    [Tooltip("選中節點的顏色（淡黃 / 米白）。")]
    private Color selectedColor = new Color(0.98f, 0.96f, 0.80f, 1f);
    [SerializeField]
    [Tooltip("未解鎖節點的顏色（偏暗）。")]
    private Color lockedColor = new Color(0.40f, 0.40f, 0.44f, 1f);

    /// <summary>此節點對應的節點索引。</summary>
    public int Index { get; private set; } = -1;

    /// <summary>此節點是否已解鎖。</summary>
    public bool Unlocked { get; private set; } = true;

    private bool isSelected;
    private Action<int> onClicked;

    private void Awake()
    {
        if (nodeImage == null) nodeImage = GetComponent<Image>();
    }

    /// <summary>綁定此節點的索引與解鎖狀態（由主控在生成節點後呼叫）。</summary>
    /// <param name="index">節點索引</param>
    /// <param name="unlocked">是否已解鎖</param>
    /// <param name="onClicked">點擊回呼，會帶回節點索引</param>
    public void Bind(int index, bool unlocked, Action<int> onClicked)
    {
        Index = index;
        Unlocked = unlocked;
        this.onClicked = onClicked;
        SetSelected(false);
    }

    /// <summary>切換選中態（光暈 + 換色；未解鎖時即使選中也維持暗色）。</summary>
    public void SetSelected(bool selected)
    {
        isSelected = selected;
        if (selectedGlow != null) selectedGlow.SetActive(selected);

        if (nodeImage != null)
        {
            nodeImage.color = !Unlocked ? lockedColor
                : selected ? selectedColor
                : normalColor;
        }
    }

    /// <summary>目前是否為選中節點。</summary>
    public bool IsSelected => isSelected;

    /// <summary>節點點擊（EventSystem 呼叫）。</summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        NotifyClicked();
    }

    /// <summary>對外的點擊入口（也可讓外部或測試直接呼叫）。</summary>
    public void NotifyClicked()
    {
        if (Index < 0) return;
        onClicked?.Invoke(Index);
    }
}
