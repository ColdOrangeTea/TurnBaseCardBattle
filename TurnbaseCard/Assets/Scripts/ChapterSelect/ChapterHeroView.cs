using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 章節大圖預覽（HeroPanel）的交叉淡化控制器。
/// 每個章節各自佔用一層 ChapterHeroLayer（由 Prefab 生成並重複利用）疊在同一個容器裡；
/// 切換章節時新層 0→1、舊層 1→0 同時 Lerp，不是「先淡出再淡入」。
/// 連續切換時同一個協程會直接從當前透明度接手，不排隊。
///
/// 純圖片、不加任何遮罩層：不要 scrim、不要漸層、不要底色板。HeroPanel 底下只有大圖層群組
/// （每層一張 Image + AspectRatioFitter），其餘留空。頁碼 / 標題等文字若要疊在圖上，
/// 自行加 TMP_Text 並用描邊 (Outline) 或陰影保證可讀，不要用漸層遮罩去壓圖。
///
/// 裁切溢出由 HeroPanel 上的 RectMask2D 負責（不用它做柔邊）；每張圖依比例縮放由各層的
/// AspectRatioFitter 負責。切圖時只寫入 fitter.aspectRatio，絕不各自去改 width / height。
/// </summary>
[RequireComponent(typeof(RectTransform))]
public class ChapterHeroView : MonoBehaviour
{
    /// <summary>大圖的尺寸適配模式。</summary>
    public enum HeroFitMode
    {
        /// <summary>完整顯示，兩側或上下留空（AspectMode = FitInParent）。</summary>
        KeepAspect,
        /// <summary>填滿 HeroPanel、比例不變，超出部分由 RectMask2D 裁掉（AspectMode = EnvelopeParent）。</summary>
        FillCrop,
        /// <summary>HeroPanel 鋪滿整個 Canvas，圖用 EnvelopeParent 填滿；縮圖列與文字區疊在其上層。</summary>
        FullScreen
    }

    /// <summary>這個大圖層要顯示章節資料裡的哪個圖片來源。</summary>
    public enum SpriteSource
    {
        /// <summary>章節主視覺（heroImage，未指定時退回縮圖）。</summary>
        HeroImage,
        /// <summary>全螢幕主題背景圖（backgroundImage，未指定時退回 blurred → hero → 縮圖）。</summary>
        BackgroundImage
    }

    [Header("組件")]
    [SerializeField]
    [Tooltip("大圖層 Prefab（需含 ChapterHeroLayer）。每個章節會生成一份，切換章節列表時回收重用。")]
    private ChapterHeroLayer layerPrefab;
    [SerializeField]
    [Tooltip("大圖層的父物件。留空則使用本物件。")]
    private RectTransform layersRoot;
    [SerializeField]
    [Tooltip("右下角的頁碼文字。可為空。")]
    private TMP_Text pageLabel;

    [Header("尺寸適配")]
    [SerializeField]
    [Tooltip("大圖適配模式：KeepAspect 完整留邊 / FillCrop 填滿裁切 / FullScreen 鋪滿整個 Canvas。")]
    private HeroFitMode fitMode = HeroFitMode.KeepAspect;
    [SerializeField]
    [Tooltip("圖片來源：HeroImage 章節主視覺 / BackgroundImage 全螢幕主題背景。")]
    private SpriteSource spriteSource = SpriteSource.HeroImage;
    [SerializeField]
    [Tooltip("FullScreen 模式時是否自動把本區塊壓到最底層（當作整個 Canvas 的背景）。\n" +
             "當作全螢幕主題背景、且階層順序由外部（產生工具）管理時請關掉，避免蓋掉更底層的純黑背景圖。")]
    private bool sendToBackInFullScreen = true;
    [SerializeField]
    [Range(0.1f, 1f)]
    [Tooltip("HeroPanel 佔 Canvas 高度的比例（KeepAspect / FillCrop 模式用；FullScreen 會鋪滿）。")]
    private float heightPercent = 0.45f;
    [SerializeField]
    [Tooltip("設計用的參考解析度（同時寫回 Canvas Scaler，並用來判斷極端比例）。")]
    private Vector2 referenceResolution = new Vector2(1920f, 1080f);
    [SerializeField]
    [Tooltip("KeepAspect / FillCrop 模式時，HeroPanel 距 Canvas 頂端的位移（負值往下）。")]
    private float topOffset = -60f;

    [Header("視窗適配（Canvas Scaler）")]
    [SerializeField]
    [Tooltip("由本元件管理 Canvas Scaler：套用參考解析度與極端比例下的 Match 動態調整。")]
    private bool manageCanvasScaler = true;
    [SerializeField]
    [Tooltip("Canvas Scaler。留空會自動往父物件尋找。")]
    private CanvasScaler canvasScaler;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("一般比例時的 Screen Match Mode = Match Width Or Height 的 Match 值。")]
    private float defaultMatch = 0.5f;
    [SerializeField]
    [Tooltip("極端比例（超寬 / 直式）時，依畫面比例把 Match 動態調成 1（鎖寬）或 0（鎖高）。")]
    private bool dynamicMatchForExtremeAspect = true;
    [SerializeField]
    [Range(0f, 1f)]
    [Tooltip("畫面比例相對參考比例偏離多少才算「極端」，超過才啟用動態 Match。")]
    private float extremeAspectTolerance = 0.25f;

    [Header("外觀設定")]
    [SerializeField]
    [Tooltip("頁碼格式，{0} = 目前頁、{1} = 總頁數。")]
    private string pageFormat = "{0} / {1}";
    [SerializeField]
    [Tooltip("交叉淡化時長（秒）。")]
    [Min(0f)]
    private float crossfadeDuration = 0.55f;
    [SerializeField]
    [Tooltip("章節沒有指定大圖時，該層顯示的佔位顏色。")]
    private Color placeholderColor = new Color(0.16f, 0.16f, 0.20f, 1f);

    // 已生成的圖層（含回收待用的）；前 activeCount 個為目前使用中
    private readonly List<ChapterHeroLayer> layers = new List<ChapterHeroLayer>();
    private readonly List<float> startAlphas = new List<float>();

    private int activeCount;
    private Coroutine fadeRoutine;
    private int currentIndex = -1;

    private RectTransform selfRect;
    private RectMask2D rectMask;    // HeroPanel 的裁切遮罩（負責裁切溢出，不做柔邊）
    private Canvas rootCanvas;      // 換算 Canvas 高度用
    private bool ready;             // Build 之後才允許 ApplyFit（避免生成期間亂算）

    /// <summary>目前顯示的章節索引（-1 = 尚未指定）。</summary>
    public int CurrentIndex => currentIndex;

    /// <summary>目前的大圖適配模式。</summary>
    public HeroFitMode FitMode => fitMode;

    private void Awake()
    {
        EnsureComponents();
    }

    private void OnEnable()
    {
        // 啟用時（例如面板重新開啟）依當下視窗大小重算一次
        if (ready) ApplyFit();
    }

    /// <summary>切換視窗大小 / 解析度時即時跟上：重算 HeroPanel 尺寸與 Match。</summary>
    private void OnRectTransformDimensionsChange()
    {
        if (ready) ApplyFit();
    }

    /// <summary>
    /// 依章節列表準備大圖層（每個章節一層）。可重複呼叫：
    /// 既有的圖層會被回收重用，只在數量不夠時才從 Prefab 生成新的。
    /// </summary>
    public void Build(ChapterListData data)
    {
        EnsureComponents();

        StopFade();
        currentIndex = -1;

        int needed = data != null ? data.ChapterCount : 0;
        if (needed > 0 && layerPrefab == null)
        {
            Debug.LogWarning($"{name} 的 ChapterHeroView 尚未指定大圖層 Prefab (ChapterHeroLayer)，大圖預覽不會顯示。" +
                             "請在 Inspector 指定，或執行 Tools → ChapterSelect → Generate Chapter Carousel UI 重新產生。");
            needed = 0;
        }

        for (int i = 0; i < needed; i++)
        {
            ChapterHeroLayer layer = GetOrCreateLayer(i);
            if (layer == null)
            {
                needed = i; // 生成失敗就停在這裡，已生成的仍可正常運作
                break;
            }

            layer.gameObject.SetActive(true);
            layer.transform.SetSiblingIndex(i);
            Sprite sprite = spriteSource == SpriteSource.BackgroundImage
                ? data.GetBackgroundSprite(i)
                : data.GetHeroSprite(i);
            layer.SetChapter(i, sprite, placeholderColor);
            layer.Alpha = 0f;
        }

        // 多出來的圖層回收待用，不銷毀（換章節列表時可以直接再拿來用）
        for (int i = needed; i < layers.Count; i++)
        {
            if (layers[i] != null) layers[i].Release();
        }

        activeCount = needed;
        ready = true;
        ApplyFit();
        UpdatePageLabel();
    }

    /// <summary>切換到指定章節的大圖（交叉淡化）。</summary>
    public void Show(int index)
    {
        Show(index, false);
    }

    /// <summary>切換到指定章節的大圖；instant = true 時直接跳到定位不做淡化。</summary>
    public void Show(int index, bool instant)
    {
        if (activeCount == 0) return;
        if (index < 0 || index >= activeCount) return;
        if (index == currentIndex && fadeRoutine == null) return;

        currentIndex = index;
        layers[index].transform.SetAsLastSibling(); // 新層疊在最上面
        UpdatePageLabel();

        StopFade(); // 中途接手：不排隊，直接從現在的透明度續接

        if (instant || crossfadeDuration <= 0f || !isActiveAndEnabled)
        {
            ApplyAlphasInstant(index);
            return;
        }
        fadeRoutine = StartCoroutine(CrossfadeRoutine(index));
    }

    #region 尺寸適配

    /// <summary>
    /// 依 fitMode 設定每層的 AspectMode，並調整 HeroPanel 的 RectTransform 與 Canvas Scaler。
    /// </summary>
    private void ApplyFit()
    {
        EnsureComponents();

        // 每層依模式決定完整留邊(FitInParent) 或 填滿裁切(EnvelopeParent)
        AspectRatioFitter.AspectMode mode = fitMode == HeroFitMode.KeepAspect
            ? AspectRatioFitter.AspectMode.FitInParent
            : AspectRatioFitter.AspectMode.EnvelopeParent;
        for (int i = 0; i < layers.Count; i++)
        {
            if (layers[i] != null) layers[i].SetFitMode(mode);
        }

        if (selfRect != null)
        {
            if (fitMode == HeroFitMode.FullScreen)
            {
                // 鋪滿整個 Canvas：錨點 (0,0)-(1,1)、offset 全 0
                selfRect.anchorMin = Vector2.zero;
                selfRect.anchorMax = Vector2.one;
                selfRect.pivot = new Vector2(0.5f, 0.5f);
                selfRect.offsetMin = Vector2.zero;
                selfRect.offsetMax = Vector2.zero;
                // 縮圖列與文字區改為疊在其上層 → 壓到最底當背景。
                // 當作全螢幕主題背景時由外部管理階層（要疊在黑底之上而非最底），此時關掉不自動下壓。
                if (sendToBackInFullScreen) selfRect.SetAsFirstSibling();
            }
            else
            {
                // 一條橫向鋪滿、頂端對齊的帶狀區，高度 = Canvas 高 * heightPercent
                float h = GetCanvasHeight() * Mathf.Clamp(heightPercent, 0.1f, 1f);
                selfRect.anchorMin = new Vector2(0f, 1f);
                selfRect.anchorMax = new Vector2(1f, 1f);
                selfRect.pivot = new Vector2(0.5f, 1f);
                selfRect.sizeDelta = new Vector2(0f, h);          // 錨點置中、橫向鋪滿
                selfRect.anchoredPosition = new Vector2(0f, topOffset);
            }
        }

        if (rectMask != null) rectMask.softness = Vector2Int.zero; // 只裁切、不柔邊

        UpdateCanvasScaler();
    }

    /// <summary>取得 Canvas 目前的高度（螢幕改變時會被 OnRectTransformDimensionsChange 重算）。</summary>
    private float GetCanvasHeight()
    {
        if (rootCanvas != null && rootCanvas.transform is RectTransform crt && crt.rect.height > 0f)
        {
            return crt.rect.height;
        }
        return referenceResolution.y > 0f ? referenceResolution.y : 1080f;
    }

    /// <summary>
    /// 套用 Canvas Scaler：參考解析度用本元件的參數；極端比例下依畫面比例動態調 Match。
    /// 一般比例維持 defaultMatch（預設 0.5）。
    /// </summary>
    private void UpdateCanvasScaler()
    {
        if (!manageCanvasScaler || canvasScaler == null) return;

        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        if (referenceResolution.x > 0f && referenceResolution.y > 0f)
        {
            canvasScaler.referenceResolution = referenceResolution;
        }

        if (!dynamicMatchForExtremeAspect || Screen.height <= 0 || referenceResolution.y <= 0f)
        {
            canvasScaler.matchWidthOrHeight = defaultMatch;
            return;
        }

        float screenAspect = (float)Screen.width / Screen.height;
        float refAspect = referenceResolution.x / referenceResolution.y;
        float ratio = refAspect > 0f ? screenAspect / refAspect : 1f;

        if (ratio > 1f + extremeAspectTolerance)
        {
            canvasScaler.matchWidthOrHeight = 1f; // 超寬螢幕：以寬度為準
        }
        else if (ratio < 1f - extremeAspectTolerance)
        {
            canvasScaler.matchWidthOrHeight = 0f; // 直式螢幕：以高度為準
        }
        else
        {
            canvasScaler.matchWidthOrHeight = defaultMatch; // 一般比例維持預設
        }
    }

    #endregion

    private IEnumerator CrossfadeRoutine(int index)
    {
        startAlphas.Clear();
        for (int i = 0; i < activeCount; i++) startAlphas.Add(layers[i].Alpha);

        float elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / crossfadeDuration);
            for (int i = 0; i < activeCount; i++)
            {
                float target = i == index ? 1f : 0f; // 新層 0→1、舊層 1→0，同時進行
                layers[i].Alpha = Mathf.Lerp(startAlphas[i], target, t);
            }
            yield return null;
        }

        ApplyAlphasInstant(index);
        fadeRoutine = null;
    }

    private void ApplyAlphasInstant(int index)
    {
        for (int i = 0; i < activeCount; i++)
        {
            layers[i].Alpha = i == index ? 1f : 0f;
        }
    }

    private void StopFade()
    {
        if (fadeRoutine == null) return;
        StopCoroutine(fadeRoutine);
        fadeRoutine = null;
    }

    /// <summary>取得第 index 層：先找池子裡回收的，沒有才從 Prefab 生成。</summary>
    private ChapterHeroLayer GetOrCreateLayer(int index)
    {
        if (index < layers.Count)
        {
            if (layers[index] != null) return layers[index];
            layers.RemoveAt(index); // 引用已失效（例如被外部刪除），改為重新生成
        }

        if (layerPrefab == null) return null;

        ChapterHeroLayer layer = Instantiate(layerPrefab, layersRoot, false);
        layers.Insert(Mathf.Min(index, layers.Count), layer);
        return layer;
    }

    private void UpdatePageLabel()
    {
        if (pageLabel == null) return;
        if (activeCount <= 0 || currentIndex < 0)
        {
            pageLabel.text = string.Empty;
            return;
        }

        string format = string.IsNullOrEmpty(pageFormat) ? "{0} / {1}" : pageFormat;
        pageLabel.text = string.Format(format, currentIndex + 1, activeCount);
    }

    /// <summary>補齊本元件會用到的引用（RectTransform、RectMask2D、Canvas、Canvas Scaler）。</summary>
    private void EnsureComponents()
    {
        if (selfRect == null) selfRect = (RectTransform)transform;
        if (layersRoot == null) layersRoot = selfRect;

        // HeroPanel 掛 RectMask2D：負責裁切溢出（FillCrop / FullScreen 時裁掉超出的部分）
        if (rectMask == null) rectMask = GetComponent<RectMask2D>();
        if (rectMask == null) rectMask = gameObject.AddComponent<RectMask2D>();

        if (rootCanvas == null) rootCanvas = GetComponentInParent<Canvas>();
        if (rootCanvas != null) rootCanvas = rootCanvas.rootCanvas;
        if (manageCanvasScaler && canvasScaler == null && rootCanvas != null)
        {
            canvasScaler = rootCanvas.GetComponent<CanvasScaler>();
        }
    }
}
