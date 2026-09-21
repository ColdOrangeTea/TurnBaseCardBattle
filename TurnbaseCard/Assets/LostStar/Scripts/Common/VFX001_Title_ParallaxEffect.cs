using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 標題畫面的滑鼠視差背景效果（致敬《雨世界 Rain World》的標題畫面演出）。
///
/// 把標題背景拆成數個「由遠到近」的圖層，隨滑鼠位置以不同幅度平移做出景深；
/// 每層還可獨立設定「呼吸飄移」——不碰滑鼠也會緩慢漂動，讓畫面待機時仍然是活的。
///
/// 使用方式：
/// 1. 掛在標題背景的父物件上。
/// 2. 元件右鍵選單 →「由子物件自動建立圖層」，一鍵把子物件依 Hierarchy 順序填進圖層清單。
/// 3. 逐層調整 depth（深度權重，越大動得越多）、追隨軸向、呼吸飄移。
///
/// 設計重點：
/// - 圖層改為資料驅動，不再靠「索引 0 = 背景、1 = 彗星」這種只寫在註解裡的隱性約定。
/// - 平滑改用與幀率無關的指數收斂，144Hz 與 60Hz 的手感一致。
/// - 同時支援世界物件（Transform.localPosition）與 UI（RectTransform.anchoredPosition），自動判斷。
/// - 基準點只在執行期記錄、不序列化，反覆進出 Play Mode 不會累積位移。
/// - 不依賴 Camera.main，也不在 Update 內做任何 Find。
/// </summary>
[DisallowMultipleComponent]
public class VFX001_Title_ParallaxEffect : MonoBehaviour
{
    /// <summary>單一視差圖層的設定。</summary>
    [Serializable]
    public class ParallaxLayer
    {
        [Tooltip("這一層要移動的物件。世界物件放一般 Transform，UI 圖層放 RectTransform 也可以（會自動改用 anchoredPosition）。")]
        public Transform target;

        [Tooltip("深度權重：越大代表越靠近鏡頭、跟滑鼠移動的幅度越大。0 = 完全不動（最遠的底圖）。")]
        public float depth = 1f;

        [Tooltip("是否跟著滑鼠左右移動。")]
        public bool followX = true;

        [Tooltip("是否跟著滑鼠上下移動。（例：彗星只想吃 Y 軸，就把 followX 取消。）")]
        public bool followY = true;

        [Tooltip("追隨速度：越大越快貼上目標位置，越小越黏、越有重量感。舊版行為約等於 2。")]
        public float followSpeed = 2f;

        [Tooltip("位移上限（世界單位 / UI 像素），X、Y 分開限制。0 = 不限制。用來避免前景層被拉出畫面外露出邊緣。")]
        public Vector2 maxOffset = Vector2.zero;

        [Header("呼吸飄移（不需滑鼠也會動）")]
        [Tooltip("飄移振幅。X 走 Sin、Y 走 Cos，兩軸都給值會漂成橢圓。0 = 這層不飄。")]
        public Vector2 driftAmplitude = Vector2.zero;

        [Tooltip("飄移速度（弧度／秒）。建議 0.1 ~ 0.5，太快會變成在抖。")]
        public Vector2 driftSpeed = new Vector2(0.2f, 0.15f);

        [Tooltip("飄移相位偏移（秒）。每層給不同值，各層才不會整齊劃一地一起晃。")]
        public float driftPhase;

        // ---- 以下為執行期狀態，不序列化（避免 Inspector 存檔後累積髒資料）----

        /// <summary>基準點：Transform 存 localPosition，RectTransform 存 anchoredPosition（z 未使用）。</summary>
        [NonSerialized] public Vector3 restPosition;

        /// <summary>target 為 UI 時的 RectTransform 快取，非 UI 時為 null。</summary>
        [NonSerialized] public RectTransform rect;

        /// <summary>是否已記錄過基準點。</summary>
        [NonSerialized] public bool initialized;
    }

    [Header("圖層（建議由遠到近排列）")]
    [SerializeField]
    [Tooltip("所有參與視差的圖層。清單順序本身不影響計算，實際幅度由各層的 depth 決定；排序只是為了好讀（與自動深度用）。")]
    private List<ParallaxLayer> layers = new List<ParallaxLayer>();

    [Header("整體參數")]
    [SerializeField]
    [Tooltip("移動力道基準值。各層實際位移 = 滑鼠偏移 × 此值 × 該層 depth。")]
    private float moveModifier = 25f;

    [SerializeField]
    [Tooltip("勾選 = 以畫面正中央為靜止基準，滑鼠往四個方向都會偏移（雨世界的手感）。取消 = 沿用舊版，以畫面左下角為基準，只往右上偏移。")]
    private bool centerOnScreenMiddle = true;

    [SerializeField]
    [Tooltip("滑鼠移出視窗時把座標夾在畫面內，避免游標離開後圖層被拉飛。")]
    private bool clampPointerToScreen = true;

    [SerializeField]
    [Tooltip("滑鼠座標本身的平滑速度（之後還會再經過各層 followSpeed 二次平滑）。越小越像鏡頭有慣性。")]
    private float pointerFollowSpeed = 8f;

    [SerializeField]
    [Tooltip("使用不受 Time.timeScale 影響的時間。標題畫面 / 暫停選單建議勾選，這樣 timeScale = 0 時視差仍會動。")]
    private bool useUnscaledTime = true;

    [Header("自動深度")]
    [SerializeField]
    [Tooltip("勾選 = 忽略各層自填的 depth，改用「索引 × 深度間隔」自動計算（等同舊版的 modWeight × i）。")]
    private bool useAutoDepth = false;

    [SerializeField]
    [Tooltip("自動深度的每層間隔。舊版等效值為 0.5。")]
    private float autoDepthStep = 0.5f;

    [Header("鏡頭（選填）")]
    [SerializeField]
    [Tooltip("換算滑鼠視埠座標用的鏡頭。留空 = 直接用 Screen 尺寸換算（多數情況留空即可，不需要也不會去找 Camera.main）。鏡頭有自訂 Viewport Rect 時才需要指定。")]
    private Camera parallaxCamera;

    /// <summary>
    /// 視差強度（0 = 完全不隨滑鼠移動、1 = 正常）。
    /// 供進場 / 退場演出淡入淡出視差用；呼吸飄移不受此值影響，畫面不會整個死掉。
    /// </summary>
    public float Intensity { get; set; } = 1f;

    /// <summary>平滑後的滑鼠位置（置中模式為 -0.5 ~ 0.5，否則為 0 ~ 1）。</summary>
    private Vector2 smoothedPointer;

    /// <summary>沒有圖層時只警告一次，避免每幀洗版。</summary>
    private bool hasWarnedNoLayer;

    private void Awake()
    {
        ValidateLayers();
        CaptureRestPositions();
    }

    private void OnEnable()
    {
        // 啟用當下直接對齊滑鼠，避免每次開啟都從畫面中心「掃」過去一次
        smoothedPointer = ReadPointer();
    }

    private void Update()
    {
        if (layers == null || layers.Count == 0)
        {
            if (!hasWarnedNoLayer)
            {
                hasWarnedNoLayer = true;
                Debug.LogWarning($"[VFX001_Title_ParallaxEffect] 物件「{name}」沒有設定任何視差圖層，效果不會作用。可用元件右鍵選單「由子物件自動建立圖層」快速建立。", this);
            }
            return;
        }

        float deltaTime = useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
        float time = useUnscaledTime ? Time.unscaledTime : Time.time;

        smoothedPointer = Vector2.Lerp(smoothedPointer, ReadPointer(), ExpStep(pointerFollowSpeed, deltaTime));

        float intensity = Mathf.Max(0f, Intensity);

        for (int i = 0; i < layers.Count; i++)
        {
            ParallaxLayer layer = layers[i];
            if (layer == null || layer.target == null) continue;   // 缺物件的圖層直接跳過，不讓 NullReference 炸出來
            if (!layer.initialized) CaptureRest(layer);

            float depth = useAutoDepth ? i * autoDepthStep : layer.depth;

            // 滑鼠帶來的位移
            Vector2 offset = new Vector2(
                layer.followX ? smoothedPointer.x * moveModifier * depth : 0f,
                layer.followY ? smoothedPointer.y * moveModifier * depth : 0f) * intensity;

            // 呼吸飄移：X 走 Sin、Y 走 Cos，兩軸都有振幅時會漂成橢圓
            offset.x += Mathf.Sin((time + layer.driftPhase) * layer.driftSpeed.x) * layer.driftAmplitude.x;
            offset.y += Mathf.Cos((time + layer.driftPhase) * layer.driftSpeed.y) * layer.driftAmplitude.y;

            if (layer.maxOffset.x > 0f) offset.x = Mathf.Clamp(offset.x, -layer.maxOffset.x, layer.maxOffset.x);
            if (layer.maxOffset.y > 0f) offset.y = Mathf.Clamp(offset.y, -layer.maxOffset.y, layer.maxOffset.y);

            ApplyOffset(layer, offset, ExpStep(layer.followSpeed, deltaTime));
        }
    }

    /// <summary>
    /// 以各圖層「目前的位置」重新記錄基準點。
    /// 若在執行期用程式碼／動畫搬動過背景（例如進場動畫結束後），呼叫這個讓視差改以新位置為中心。
    /// </summary>
    [ContextMenu("以目前位置設為基準點")]
    public void CaptureRestPositions()
    {
        if (layers == null) return;

        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] == null || layers[i].target == null) continue;
            CaptureRest(layers[i]);
        }
    }

    /// <summary>立刻把所有圖層歸回基準點（不做平滑）。場景切換、截圖、進場演出前很好用。</summary>
    [ContextMenu("立刻歸位")]
    public void ResetToRest()
    {
        if (layers == null) return;

        smoothedPointer = centerOnScreenMiddle ? Vector2.zero : new Vector2(0.5f, 0.5f);

        for (int i = 0; i < layers.Count; i++)
        {
            ParallaxLayer layer = layers[i];
            if (layer == null || layer.target == null) continue;
            if (!layer.initialized) CaptureRest(layer);
            ApplyOffset(layer, Vector2.zero, 1f);
        }
    }

    /// <summary>
    /// 由子物件自動建立圖層清單（依 Hierarchy 由上到下 = 由遠到近），並依序帶入自動深度。
    /// 已經在清單裡的物件會保留原設定，只補上還沒登記的子物件，不會蓋掉你調好的參數。
    /// </summary>
    [ContextMenu("由子物件自動建立圖層")]
    public void BuildLayersFromChildren()
    {
        if (layers == null) layers = new List<ParallaxLayer>();

        int added = 0;
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);
            if (ContainsTarget(child)) continue;

            layers.Add(new ParallaxLayer
            {
                target = child,
                depth = layers.Count * autoDepthStep,
                driftPhase = layers.Count * 1.7f   // 錯開相位，各層才不會同步晃
            });
            added++;
        }

        MarkDirtyInEditor();
        Debug.Log($"[VFX001_Title_ParallaxEffect] 「{name}」自動建立圖層完成：新增 {added} 層，目前共 {layers.Count} 層。", this);
    }

    /// <summary>把所有圖層的 depth 依清單索引重新配置成「索引 × 深度間隔」。</summary>
    [ContextMenu("依索引重新配置深度")]
    public void ReassignDepthByIndex()
    {
        if (layers == null || layers.Count == 0)
        {
            Debug.LogWarning($"[VFX001_Title_ParallaxEffect] 「{name}」目前沒有圖層可以配置深度。", this);
            return;
        }

        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] == null) continue;
            layers[i].depth = i * autoDepthStep;
        }

        MarkDirtyInEditor();
        Debug.Log($"[VFX001_Title_ParallaxEffect] 「{name}」已依索引重新配置 {layers.Count} 層的深度（間隔 {autoDepthStep}）。", this);
    }

    /// <summary>把位移套到圖層上（UI 走 anchoredPosition、世界物件走 localPosition，z 一律保留基準值）。</summary>
    private static void ApplyOffset(ParallaxLayer layer, Vector2 offset, float t)
    {
        if (layer.rect != null)
        {
            Vector2 uiTarget = new Vector2(layer.restPosition.x + offset.x, layer.restPosition.y + offset.y);
            layer.rect.anchoredPosition = Vector2.Lerp(layer.rect.anchoredPosition, uiTarget, t);
            return;
        }

        Vector3 worldTarget = new Vector3(layer.restPosition.x + offset.x, layer.restPosition.y + offset.y, layer.restPosition.z);
        layer.target.localPosition = Vector3.Lerp(layer.target.localPosition, worldTarget, t);
    }

    /// <summary>取得滑鼠在畫面上的相對位置。置中模式回傳 -0.5 ~ 0.5，否則回傳 0 ~ 1。</summary>
    private Vector2 ReadPointer()
    {
        Vector3 mouse = Input.mousePosition;
        Vector2 viewport;

        if (parallaxCamera != null)
        {
            viewport = parallaxCamera.ScreenToViewportPoint(mouse);
        }
        else
        {
            // 不依賴 Camera.main：直接用畫面尺寸換算，順便省掉每幀 FindGameObjectWithTag 的開銷
            viewport = new Vector2(mouse.x / Mathf.Max(1, Screen.width), mouse.y / Mathf.Max(1, Screen.height));
        }

        if (clampPointerToScreen)
        {
            viewport.x = Mathf.Clamp01(viewport.x);
            viewport.y = Mathf.Clamp01(viewport.y);
        }

        return centerOnScreenMiddle ? viewport - new Vector2(0.5f, 0.5f) : viewport;
    }

    /// <summary>記錄單一圖層的基準點，並判斷它是 UI 還是世界物件。</summary>
    private static void CaptureRest(ParallaxLayer layer)
    {
        layer.rect = layer.target as RectTransform;
        layer.restPosition = layer.rect != null
            ? new Vector3(layer.rect.anchoredPosition.x, layer.rect.anchoredPosition.y, 0f)
            : layer.target.localPosition;
        layer.initialized = true;
    }

    /// <summary>與幀率無關的指數收斂係數；speed 小於等於 0 視為不平滑（直接到位）。</summary>
    private static float ExpStep(float speed, float deltaTime)
    {
        if (speed <= 0f) return 1f;
        return 1f - Mathf.Exp(-speed * deltaTime);
    }

    /// <summary>清單裡是否已經有這個物件（避免自動建立時重複加入）。</summary>
    private bool ContainsTarget(Transform target)
    {
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] != null && layers[i].target == target) return true;
        }
        return false;
    }

    /// <summary>啟動時檢查圖層設定，缺東西就給明確的中文提示，而不是等它在 Update 裡出事。</summary>
    private void ValidateLayers()
    {
        if (layers == null || layers.Count == 0) return;

        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] == null)
            {
                Debug.LogWarning($"[VFX001_Title_ParallaxEffect] 「{name}」的第 {i} 個圖層是空的，該層會被略過。", this);
            }
            else if (layers[i].target == null)
            {
                Debug.LogWarning($"[VFX001_Title_ParallaxEffect] 「{name}」的第 {i} 個圖層沒有指定 target 物件，該層會被略過。", this);
            }
        }
    }

    /// <summary>右鍵選單改動清單後在編輯器內標記為已修改，關掉 Unity 才不會白改。</summary>
    private void MarkDirtyInEditor()
    {
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
#endif
    }
}
