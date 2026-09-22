using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

/// <summary>
/// 格子事件類型。註：<see cref="None"/> 放在第一位（列舉值 0），讓「沒有事件的普通格」成為預設，
/// 不是每一格都有事件。若日後要調整順序，注意場景/Prefab 已序列化的整數值會位移。
/// </summary>
public enum GridEventType
{
    None,       // 沒有事件（預設）：不觸發任何事件、也不顯示 icon
    BossCombat, // 戰鬥（接 V2 戰鬥）→ icon：OBJ_Mon
    Shop,       // 商店 → icon：OBJ_Store
    Event,      // 一般事件 → icon：OBJ_Ques
    Treasure,   // 寶箱 → icon：OBJ_Box（＋OBJ_HIghLight 裝飾）
    quest,      // 任務 → icon：OBJ_Ques（與 Event 同視覺）
    StageGate,  // 起點/終點的門 → icon：OBJ_Door（＋OBJ_Star 裝飾）；換關由 LevelMapManager 依 endGrid 判定
}

/// <summary>
/// 掛在「格子」上的事件標記，同時負責這格 Icon 要顯示哪個 sprite。
///
/// 玩家走到此格時由 <see cref="MapEventService"/> 依 <see cref="eventType"/> 觸發對應事件；
/// 而本元件則依 <see cref="eventType"/> 把對應圖示套到 <see cref="iconRenderer"/>（部分類型另有
/// <see cref="decorationRenderer"/> 裝飾層，如寶箱高光、門的星星）。<see cref="GridEventType.None"/>
/// 代表這格沒有事件，圖示與裝飾都隱藏。
///
/// 圖示 sprite 以序列化欄位注入（來源為 L1OBJ 圖集的切片），不在程式裡寫死路徑，方便日後替換美術。
/// </summary>
public class NodeEvent : MonoBehaviour
{
    [Header("事件")]
    [Tooltip("此格的事件類型（None＝沒有事件）")]
    public GridEventType eventType;

    [Tooltip("BossCombat 時要開打的敵人類型")]
    public EnemyType enemyType = EnemyType.Yarn;

    [Tooltip("是否只觸發一次（觸發後不再觸發）")]
    public bool triggerOnce = true;

    [Tooltip("執行期用：是否已觸發過")]
    [SerializeField] private bool consumed = false;

    [Header("Icon 呈現")]
    [Tooltip("主圖示的 SpriteRenderer（留空會自動找子物件 Icon）")]
    [SerializeField] private SpriteRenderer iconRenderer;
    [Tooltip("裝飾層 SpriteRenderer（寶箱高光／門星星用；可空）")]
    [SerializeField] private SpriteRenderer decorationRenderer;

    [Header("各類型圖示（來源：L1OBJ 圖集）")]
    [Tooltip("BossCombat：OBJ_Mon")]
    [SerializeField] private Sprite enemySprite;
    [Tooltip("Shop：OBJ_Store")]
    [SerializeField] private Sprite shopSprite;
    [Tooltip("Event／quest：OBJ_Ques")]
    [SerializeField] private Sprite eventSprite;
    [Tooltip("Treasure：OBJ_Box")]
    [SerializeField] private Sprite treasureSprite;
    [Tooltip("StageGate：OBJ_Door")]
    [SerializeField] private Sprite doorSprite;

    [Header("裝飾圖示")]
    [Tooltip("寶箱裝飾：OBJ_HIghLight")]
    [SerializeField] private Sprite treasureDecoration;
    [Tooltip("門裝飾：OBJ_Star")]
    [SerializeField] private Sprite doorDecoration;

    /// <summary>此格是否還能觸發（triggerOnce 且已觸發過則否）。</summary>
    public bool CanTrigger => !(triggerOnce && consumed);

    /// <summary>標記為已觸發，並隱藏這格的 icon（寶箱/敵人/任務等觸發後就該消失）。</summary>
    public void MarkConsumed()
    {
        consumed = true;
        ApplyIcon(); // 觸發後 icon 隱藏
    }

    private void Awake()
    {
        if (iconRenderer == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null) iconRenderer = icon.GetComponent<SpriteRenderer>();
        }
        ApplyIcon();
    }

    /// <summary>依 <see cref="eventType"/> 把對應圖示套到 Icon 與裝飾層。可由編輯器工具在設定類型後呼叫。</summary>
    public void ApplyIcon()
    {
        Sprite icon = null;
        Sprite deco = null;

        // 已觸發過（且只觸發一次）的格子不再顯示 icon——寶箱/敵人/任務等觸發後就消失
        bool hidden = triggerOnce && consumed;
        if (!hidden)
        switch (eventType)
        {
            case GridEventType.BossCombat: icon = enemySprite; break;
            case GridEventType.Shop:       icon = shopSprite; break;
            case GridEventType.Event:      icon = eventSprite; break;
            case GridEventType.quest:      icon = eventSprite; break;   // 任務與一般事件同視覺
            case GridEventType.Treasure:   icon = treasureSprite; deco = treasureDecoration; break;
            case GridEventType.StageGate:  icon = doorSprite;     deco = doorDecoration; break;
            case GridEventType.None:       icon = null; break;          // 無事件：不顯示
        }

        if (iconRenderer != null)
        {
            iconRenderer.sprite = icon;
            iconRenderer.enabled = icon != null;
        }
        if (decorationRenderer != null)
        {
            decorationRenderer.sprite = deco;
            decorationRenderer.enabled = deco != null;
        }
    }

#if UNITY_EDITOR
    // 編輯器中改變類型／指派圖示時，即時更新場景預覽（不影響執行期）。
    private void OnValidate()
    {
        if (Application.isPlaying) return;
        if (iconRenderer == null)
        {
            Transform icon = transform.Find("Icon");
            if (icon != null) iconRenderer = icon.GetComponent<SpriteRenderer>();
        }
        ApplyIcon();
    }
#endif
}
