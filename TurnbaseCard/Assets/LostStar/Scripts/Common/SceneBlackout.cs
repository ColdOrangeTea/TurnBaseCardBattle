using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 常駐黑底（SceneBlackout）：全螢幕的純色底板，永遠開著。
///
/// 目的：劇情段落沒有背景圖、背景圖切換的空檔、或 Scene 切換的中間，
/// 避免直接透出 Unity 的 Scene 畫面（Camera 沒畫到的地方）。
///
/// 它是「永遠在的底板」，不是轉場元素：
///   ● 一律保持 active，不要 SetActive(false)
///   ● 不要放進任何會淡出的 CanvasGroup 底下（偵測到會出中文警告）
///   ● 不受任何轉場動畫的 alpha 影響
///
/// RectTransform 一律鎖成 Anchor (0,0)-(1,1)、offset 全 0，任何解析度與視窗比例都自動鋪滿，
/// 不需要程式再調整；Image 一律是純色、無 Sprite、Raycast Target = false（絕不擋點擊）。
///
/// 層級：
///   ● 自成一層 Canvas（建議做法，常駐 Root Canvas + DontDestroyOnLoad）→ 由 sortingOrder 決定。
///   ● 與其他 UI 共用同一個父物件 → 由 stayAboveThis / stayBelowThis 決定 siblingIndex
///     （uGUI 是 siblingIndex 越大越上層）。
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(RectTransform), typeof(CanvasRenderer), typeof(Image))]
[ExecuteAlways]
public class SceneBlackout : MonoBehaviour
{
    [Header("外觀")]
    [SerializeField]
    [Tooltip("底板顏色。需要換成其他底色時直接調（alpha 請保持 1，否則就墊不住了）。")]
    private Color backdropColor = Color.black;

    [Header("層級（自成一層 Canvas 時）")]
    [SerializeField]
    [Tooltip("本底板所屬的 Canvas。留空會自動往父物件尋找。")]
    private Canvas ownCanvas;
    [SerializeField]
    [Tooltip("由本元件管理 Canvas 的 sortingOrder。")]
    private bool manageSortingOrder = true;
    [SerializeField]
    [Tooltip("Canvas 的 sortingOrder。預設 -100 = 墊在所有 UI 之下；" +
             "若要嚴格夾在章節選擇層與劇情層之間，改成兩者之間的值（本專案為 5 與 20，例如 10）——" +
             "但那會連章節選擇畫面一起蓋住，除非章節選擇層本來就在更上層。")]
    private int sortingOrder = -100;

    [Header("層級（與其他 UI 共用父物件時）")]
    [SerializeField]
    [Tooltip("要蓋在誰之上（例：章節選擇層）。需與本物件同一個父物件。可為空。")]
    private Transform stayAboveThis;
    [SerializeField]
    [Tooltip("要壓在誰之下（例：劇情層）。需與本物件同一個父物件。可為空。")]
    private Transform stayBelowThis;
    [SerializeField]
    [Tooltip("上面兩個都留空時，直接壓到父物件的最底層（siblingIndex = 0）。")]
    private bool moveToBottomSibling = true;

    [Header("常駐")]
    [SerializeField]
    [Tooltip("Scene 切換時保留（對根物件呼叫 DontDestroyOnLoad），確保切換的空檔也有東西墊著。")]
    private bool persistAcrossScenes = true;
    [SerializeField]
    [Tooltip("重複載入時只保留一份，多出來的自動銷毀。")]
    private bool keepSingleInstance = true;

    [Header("相機")]
    [SerializeField]
    [Tooltip("保險起見，把相機的 Clear Flags 設為 Solid Color、背景色設成底板顏色。")]
    private bool forceCameraSolidColor = true;
    [SerializeField]
    [Tooltip("要設定的相機。留空 = Camera.main。")]
    private Camera targetCamera;

    private static SceneBlackout instance;

    private Image image;

    /// <summary>目前的底板顏色；執行期指定會立刻套用（含相機背景色）。</summary>
    public Color BackdropColor
    {
        get => backdropColor;
        set
        {
            backdropColor = value;
            ApplyAppearance();
            ApplyCamera();
        }
    }

    private void Awake()
    {
        if (!Application.isPlaying)
        {
            // 編輯器內只維持外觀與版面，不去動場景的相機設定
            ApplyAppearance();
            ApplyRect();
            ApplyLayerOrder();
            return;
        }

        if (keepSingleInstance && instance != null && instance != this)
        {
            // 已經有一份常駐黑底了，這份多餘 → 連同它的根物件一起收掉
            Destroy(transform.root.gameObject);
            return;
        }
        instance = this;

        if (persistAcrossScenes)
        {
            DontDestroyOnLoad(transform.root.gameObject);
        }

        ApplyAll();
        WarnIfFadeable();
    }

    private void OnEnable()
    {
        if (!Application.isPlaying)
        {
            ApplyAppearance();
            ApplyRect();
            ApplyLayerOrder();
            return;
        }
        ApplyAll();
    }

    private void OnDisable()
    {
        // 這是永遠在的底板；被關掉就等於失去保護，明確講出來而不是默默失效
        if (!Application.isPlaying) return;
        Debug.LogWarning($"{name} 的 SceneBlackout 被停用了。常駐黑底應一律保持 active，" +
                         "否則沒有背景圖的段落會直接透出 Scene 畫面。");
    }

    private void OnDestroy()
    {
        if (instance == this) instance = null;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        // 編輯器調整參數時即時反映外觀與層級（不動相機，避免在 OnValidate 改到別的物件）
        ApplyAppearance();
        ApplyRect();
        ApplyLayerOrder();
    }
#endif

    /// <summary>重新套用全部設定（外觀 / 版面 / 層級 / 相機）。外部改完參數可直接呼叫。</summary>
    public void ApplyAll()
    {
        ApplyAppearance();
        ApplyRect();
        ApplyLayerOrder();
        ApplyCamera();
    }

    /// <summary>Image：純色、無 Sprite、不接收 Raycast。</summary>
    private void ApplyAppearance()
    {
        if (image == null) image = GetComponent<Image>();
        if (image == null) return;

        image.sprite = null;
        image.color = backdropColor;
        image.raycastTarget = false; // 絕不擋住任何點擊
        image.enabled = true;
    }

    /// <summary>RectTransform：鋪滿父容器，任何解析度與視窗比例都不用再調。</summary>
    private void ApplyRect()
    {
        var rt = transform as RectTransform;
        if (rt == null) return;

        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        rt.localScale = Vector3.one;
        rt.localRotation = Quaternion.identity;
    }

    /// <summary>層級：自成一層時用 sortingOrder，共用父物件時用 siblingIndex。</summary>
    private void ApplyLayerOrder()
    {
        if (ownCanvas == null) ownCanvas = GetComponentInParent<Canvas>();

        if (manageSortingOrder && ownCanvas != null)
        {
            // 巢狀 Canvas 要打開 overrideSorting，sortingOrder 才會生效
            if (!ownCanvas.isRootCanvas) ownCanvas.overrideSorting = true;
            ownCanvas.sortingOrder = sortingOrder;
        }

        ApplySiblingOrder();
    }

    /// <summary>
    /// 夾在 stayAboveThis 之上、stayBelowThis 之下（uGUI siblingIndex 越大越上層）。
    /// 兩者都沒指定時，依設定壓到最底層。
    /// </summary>
    private void ApplySiblingOrder()
    {
        Transform parent = transform.parent;
        if (parent == null) return;

        int myIndex = transform.GetSiblingIndex();
        int target = -1;

        if (stayAboveThis != null)
        {
            if (stayAboveThis.parent != parent)
            {
                Debug.LogWarning($"{name} 的 SceneBlackout：stayAboveThis「{stayAboveThis.name}」與黑底不是同一個父物件，" +
                                 "無法用 siblingIndex 排序（若兩者分屬不同 Canvas，請改用 sortingOrder）。");
            }
            else
            {
                // 先扣掉「自己被移走後對方會往前遞補」的位移，再排到它上面一格
                int aboveIndex = stayAboveThis.GetSiblingIndex();
                if (myIndex < aboveIndex) aboveIndex--;
                target = aboveIndex + 1;
            }
        }

        if (stayBelowThis != null)
        {
            if (stayBelowThis.parent != parent)
            {
                Debug.LogWarning($"{name} 的 SceneBlackout：stayBelowThis「{stayBelowThis.name}」與黑底不是同一個父物件，" +
                                 "無法用 siblingIndex 排序（若兩者分屬不同 Canvas，請改用 sortingOrder）。");
            }
            else
            {
                int belowIndex = stayBelowThis.GetSiblingIndex();
                if (myIndex < belowIndex) belowIndex--;
                target = target < 0 ? belowIndex : Mathf.Min(target, belowIndex);
            }
        }

        if (target < 0 && moveToBottomSibling) target = 0;
        if (target < 0) return;

        transform.SetSiblingIndex(Mathf.Clamp(target, 0, parent.childCount - 1));
    }

    /// <summary>相機保險：Clear Flags = Solid Color、背景色 = 底板顏色。</summary>
    private void ApplyCamera()
    {
        if (!forceCameraSolidColor) return;

        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = backdropColor;
    }

    /// <summary>檢查是否被放進會淡出的 CanvasGroup 底下（那會讓底板跟著消失）。</summary>
    private void WarnIfFadeable()
    {
        var group = GetComponentInParent<CanvasGroup>();
        if (group == null) return;

        Debug.LogWarning($"{name} 的 SceneBlackout 位於 CanvasGroup「{group.name}」底下，" +
                         "轉場淡出時會連黑底一起淡掉。請把它移到不會被淡化的常駐節點下。");
    }
}
