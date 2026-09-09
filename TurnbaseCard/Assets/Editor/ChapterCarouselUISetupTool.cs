using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Assets.Scripts.ChapterSelect;
using Assets.Scripts.Dialogue;

/// <summary>
/// 一鍵產生「全螢幕主題背景 + 左側標題 + 中心聚焦縮圖輪播」的章節選擇畫面（參考明日方舟樂章 coverflow 排版）。
/// 疊層由下而上：
///   Backdrop（全螢幕純黑背景圖，最底層，切換空檔不透出後方畫面）、
///   ChapterBackground（全螢幕主題背景圖，切換章節時交叉淡入淡出；上方墊一層可調暗化 DimScrim 提升可讀性 + 右下頁碼）、
///   ChapterCarouselPanel（透明、不擋點擊）內含 MetaPanel（左側標題 / 副標 / 描述，左對齊）
///   與 SelectorPanel（偏右的方形樂章封面輪播，中央放大兩側漸小；掛 RectMask2D 讓兩端淡出）。
/// 導覽方式：點兩側封面移到中央、拖曳、或鍵盤方向鍵（不使用左右箭頭）。
/// 背景與標題文字用描邊材質保證疊在圖上仍可讀；背景圖來源為章節資料的 backgroundImage（未填則依序退回 blurred → hero → 縮圖）。
///
/// 使用方式：Unity 選單 → Tools → ChapterSelect → Generate Chapter Carousel UI (大圖輪播選擇器)
/// 產出：
///   Assets/Image/UI/               ChapterGlow / ChapterCardOutline（程序化生成的 UI 圖）
///   Assets/Prefabs/ChapterSelect/  ChapterCard / ChapterHeroLayer / ChapterBackground / ChapterCarouselPanel / ChapterCarouselCanvas
///   Assets/Data/ChapterSelect/     MainStoryChapters.asset（與章節清單版共用同一份資料）
///   Assets/Scenes/ChapterCarouselSample.unity（輪播 + 對話 UI 的範例場景）
/// 可重複執行：會覆蓋更新既有產出（GUID 不變，引用不會斷）。
///
/// 新增章節時只要在章節列表資訊表加一筆、補上圖（需要時再補一個專用卡片 Prefab），不必改程式也不必重跑本工具。
///
/// 此工具是由 A_Good_Ink 使用 AI 生成的工具。
/// </summary>
public static class ChapterCarouselUISetupTool
{
    private const string PrefabDir = "Assets/Prefabs/ChapterSelect";
    private const string ImageDir = "Assets/Image/UI";
    private const string SceneDir = "Assets/Scenes";
    private const string ScenePath = SceneDir + "/ChapterCarouselSample.unity";

    // 章節資料（與章節清單版共用同一份；原為 ChapterSelectUISetupTool 產生，該工具移除後併入本工具）
    private const string DataDir = "Assets/Data/ChapterSelect";
    private const string ChapterDataPath = DataDir + "/MainStoryChapters.asset";
    private const string SampleDialoguePath = "Assets/Data/Dialogue/SampleDialogue.asset";

    // 既有對話系統的產出（本工具直接重用）
    private const string DialogueCanvasPrefabPath = "Assets/Prefabs/Dialogue/DialogueCanvas.prefab";

    // 中文字型
    private const string FontSourcePath = "Assets/Font/TaipeiSansTCBeta-Regular.ttf";
    private const string FontAssetPath = "Assets/Font/TaipeiSansTCBeta-Regular SDF.asset";
    private const string HeroTextMaterialPath = "Assets/Font/ChapterHeroText SDF Outline.mat";
    private static TMP_FontAsset s_font;
    private static Material s_heroTextMaterial;

    // 程序化生成的 UI 圖
    private const string GlowPath = ImageDir + "/ChapterGlow.png";
    private const string OutlinePath = ImageDir + "/ChapterCardOutline.png";

    // 排列參數（與 ChapterCarouselController 的預設值一致）
    // 參考圖 2/3/5：方形樂章封面、中央放大、兩側漸小，Step = 中央與相鄰卡片中心的距離
    private const float Step = 360f;
    private const float CardWidth = 340f;
    private const float CardHeight = 340f;

    // 版面尺寸（1920 x 1080 參考解析度）
    // 背景圖層 Prefab 的初始尺寸（執行期會由 AspectRatioFitter 依實際比例覆寫，這裡只是佔位）
    private const float HeroWidth = 1280f;
    private const float HeroHeight = 520f;

    // 左側標題區（MetaPanel）：靠左、垂直置中，文字左對齊
    private const float MetaWidth = 760f;
    private const float MetaHeight = 360f;
    private const float MetaLeft = 140f;
    private const float MetaCenterY = 40f;

    // 中央縮圖輪播區（SelectorPanel）：橫向鋪滿、垂直置中；輪播中心右移，讓出左側標題空間（貼近圖 2/3/5）
    private const float SelectorHeight = 460f;   // 需容得下方形卡片 + 選中光暈
    private const float SelectorCenterY = 0f;
    private const float SelectorItemsCenterX = 240f; // 卡片群中心相對畫面中央往右的位移
    private const int SelectorMaskSoftness = 120;

    // 全螢幕主題背景之上的暗化層（提升上方文字 / 卡片對比；黑底已在最底層，這層只負責可讀性）
    private const float BackgroundScrimAlpha = 0.32f;

    // 配色
    private static readonly Color CardFrameColor = new Color(0.13f, 0.13f, 0.16f, 1f);
    private static readonly Color AccentColor = new Color(0.95f, 0.80f, 0.40f, 1f);
    private static readonly Color SubtitleColor = new Color(1f, 1f, 1f, 0.70f);
    private static readonly Color DescriptionColor = new Color(1f, 1f, 1f, 0.55f);

    [MenuItem("Tools/ChapterSelect/Generate Chapter Carousel UI (大圖輪播選擇器)")]
    public static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabDir);
        EnsureFolder(ImageDir);
        EnsureFolder(SceneDir);

        s_font = LoadOrCreateChineseFontAsset();
        s_heroTextMaterial = LoadOrCreateHeroTextMaterial();

        // 章節資料與章節清單版共用同一份，兩種選擇畫面可以互換使用
        ChapterListData chapterData = CreateOrLoadChapterListData();

        Sprite glow = GenerateGlowSprite();
        Sprite outline = GenerateOutlineSprite();

        GameObject cardPrefab = CreateChapterCardPrefab(glow, outline);
        ChapterHeroLayer heroLayerPrefab = CreateHeroLayerPrefab();
        GameObject backgroundPrefab = CreateBackgroundPrefab(heroLayerPrefab);
        GameObject panelPrefab = CreateCarouselPanelPrefab();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject canvasGo = BuildCarouselCanvas(panelPrefab, backgroundPrefab, cardPrefab, chapterData);
        GameObject canvasPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            canvasGo, PrefabDir + "/ChapterCarouselCanvas.prefab", InteractionMode.AutomatedAction);

        GameObject dialogueCanvasGo = InstantiateDialogueCanvas();
        bool dialogueLinked = BindDialogueSystem(canvasGo, dialogueCanvasGo);

        new GameObject("EventSystem", typeof(InteractionOfUI), typeof(StandaloneInputModule));

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = canvasPrefab;

        EditorUtility.DisplayDialog("Chapter Carousel UI",
            "產生完成！\n\n" +
            "Prefabs：" + PrefabDir + "\n" +
            "　　ChapterCard.prefab（方形樂章封面卡片）\n" +
            "　　ChapterHeroLayer.prefab（背景圖層，依章節數量生成並重複利用）\n" +
            "　　ChapterBackground.prefab（全螢幕主題背景，切章節交叉淡化）\n" +
            "　　ChapterCarouselPanel.prefab（左側標題 Meta + 中央 Selector）\n" +
            "　　ChapterCarouselCanvas.prefab（整組畫面）\n" +
            "UI 圖：" + ImageDir + "\n" +
            "章節列表資訊表：" + ChapterDataPath + "\n" +
            "範例場景：" + ScenePath + "\n\n" +
            (dialogueLinked
                ? "已自動接上場景中的 DialogueCanvas。\n按 Play 後：點兩側封面移到中央、點中央封面開始劇情，\n也可用鍵盤方向鍵或拖曳切換（已移除左右箭頭）。"
                : "找不到 " + DialogueCanvasPrefabPath + "\n請先執行 Tools → Dialogue → Generate Dialogue UI，再重跑本工具以自動接上對話系統。"),
            "OK");
    }

    #region 資料夾與字型

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static TMP_FontAsset LoadOrCreateChineseFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        if (sourceFont == null)
        {
            Debug.LogWarning("[ChapterCarousel] 找不到 " + FontSourcePath + "，文字將使用 TMP 預設字型（中文可能顯示為方框）。");
            return null;
        }

        var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 90, 9,
            GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
        fontAsset.name = Path.GetFileNameWithoutExtension(FontAssetPath);

        AssetDatabase.CreateAsset(fontAsset, FontAssetPath);
        fontAsset.atlasTexture.name = fontAsset.name + " Atlas";
        fontAsset.material.name = fontAsset.name + " Material";
        AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
        AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
        AssetDatabase.SaveAssets();
        return fontAsset;
    }

    /// <summary>
    /// 產生（或載入）頁碼 / 標題疊在圖上的描邊 + 柔陰影材質，取代舊的漸層遮罩來保證可讀。
    /// 由中文字型的預設材質複製而來（共用同一張 SDF Atlas），存成獨立資產（GUID 穩定、引用不斷）。
    /// </summary>
    private static Material LoadOrCreateHeroTextMaterial()
    {
        var existing = AssetDatabase.LoadAssetAtPath<Material>(HeroTextMaterialPath);
        if (existing != null) return existing;

        if (s_font == null || s_font.material == null)
        {
            Debug.LogWarning("[ChapterCarousel] 缺少中文字型材質，頁碼將使用預設材質（疊在圖上可能較不清楚）。");
            return null;
        }

        var mat = new Material(s_font.material) { name = "ChapterHeroText SDF Outline" };
        // 描邊：黑色細邊，讓白字疊在任何底圖上都有輪廓
        mat.EnableKeyword("OUTLINE_ON");
        mat.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0f, 0f, 0f, 1f));
        mat.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.2f);
        // 柔陰影（Underlay）：再補一層可讀性，往右下偏一點
        mat.EnableKeyword("UNDERLAY_ON");
        mat.SetColor(ShaderUtilities.ID_UnderlayColor, new Color(0f, 0f, 0f, 0.7f));
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetX, 0.6f);
        mat.SetFloat(ShaderUtilities.ID_UnderlayOffsetY, -0.6f);
        mat.SetFloat(ShaderUtilities.ID_UnderlaySoftness, 0.3f);

        AssetDatabase.CreateAsset(mat, HeroTextMaterialPath);
        AssetDatabase.SaveAssets();
        return mat;
    }

    /// <summary>
    /// 建立範例章節列表資訊表。已存在且有章節時不覆蓋（保留使用者的編輯）。
    /// 原為 ChapterSelectUISetupTool 的共用方法，該工具移除後併入本工具。
    /// </summary>
    private static ChapterListData CreateOrLoadChapterListData()
    {
        EnsureFolder(DataDir);

        var data = AssetDatabase.LoadAssetAtPath<ChapterListData>(ChapterDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<ChapterListData>();
            AssetDatabase.CreateAsset(data, ChapterDataPath);
        }

        if (data.chapters == null || data.chapters.Count == 0) // 不覆蓋使用者已編輯的內容
        {
            var sampleDialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(SampleDialoguePath);

            data.chapterType = DialogueType.MainStory;
            data.menuTitle = "章節選擇";
            data.numberStyle = ChapterNumberStyle.Arabic;
            data.numberFormat = "第 {0} 章";
            data.titleFormat = "{0}　{1}";
            data.lockedTitle = "？？？";
            data.chapters = new List<ChapterListData.ChapterEntry>
            {
                new ChapterListData.ChapterEntry
                {
                    title = "旅途的起點",
                    summary = "旅人在伊菲爾的邊境醒來，遇見了自稱嚮導的瑟拉菲斯。",
                    dialogueData = sampleDialogue,
                    unlocked = true,
                },
                new ChapterListData.ChapterEntry
                {
                    title = "穿過霧之谷",
                    summary = "濃霧散去之前，沒有人能分辨方向——包括嚮導。",
                    dialogueData = sampleDialogue,
                    unlocked = true,
                },
                new ChapterListData.ChapterEntry
                {
                    title = "無名的旅店",
                    summary = "旅店的招牌上什麼也沒寫，門後卻擠滿了說著同一句話的旅人。",
                    dialogueData = sampleDialogue,
                    unlocked = true,
                },
                new ChapterListData.ChapterEntry
                {
                    title = "回不去的路",
                    summary = "（尚未解鎖）",
                    dialogueData = sampleDialogue,
                    unlocked = false,
                },
                new ChapterListData.ChapterEntry
                {
                    title = "終末之前",
                    summary = "（尚未解鎖）",
                    dialogueData = sampleDialogue,
                    unlocked = false,
                },
            };
            EditorUtility.SetDirty(data);
        }
        return data;
    }

    #endregion

    #region 程序化生成的 UI 圖

    /// <summary>
    /// 產生（或覆蓋更新）一張 PNG 並匯入為 Sprite。
    /// 覆寫同一個檔案路徑，.meta 保留 → GUID 不變，既有引用不會斷。
    /// </summary>
    private static Sprite GenerateSprite(string assetPath, int width, int height,
        Func<int, int, Color> pixelFunc, Vector4 border)
    {
        if (width <= 0 || height <= 0 || pixelFunc == null) return null;

        var tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        var pixels = new Color[width * height];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                pixels[y * width + x] = pixelFunc(x, y);
            }
        }
        tex.SetPixels(pixels);
        tex.Apply();

        byte[] png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        if (png == null)
        {
            Debug.LogWarning("[ChapterCarousel] 產生 " + assetPath + " 失敗（PNG 編碼失敗）。");
            return null;
        }

        string fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
        string dir = Path.GetDirectoryName(fullPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir)) Directory.CreateDirectory(dir);
        File.WriteAllBytes(fullPath, png);

        AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);

        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.filterMode = FilterMode.Bilinear;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteBorder = border;   // 9-slice 邊界（不需要時傳 Vector4.zero）
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null) Debug.LogWarning("[ChapterCarousel] " + assetPath + " 匯入後找不到 Sprite。");
        return sprite;
    }

    /// <summary>選中卡片的微光暈（徑向柔邊白光，用 Image.color 上色）。</summary>
    private static Sprite GenerateGlowSprite()
    {
        const int size = 128;
        float center = (size - 1) * 0.5f;
        return GenerateSprite(GlowPath, size, size, (x, y) =>
        {
            float dx = (x - center) / center;
            float dy = (y - center) / center;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.6f);
            return new Color(1f, 1f, 1f, a);
        }, Vector4.zero);
    }

    /// <summary>選中卡片的高亮描邊（中空 9-slice 方框，邊寬 4px）。</summary>
    private static Sprite GenerateOutlineSprite()
    {
        const int size = 24;
        const int border = 4;
        return GenerateSprite(OutlinePath, size, size, (x, y) =>
        {
            bool onBorder = x < border || x >= size - border || y < border || y >= size - border;
            return onBorder ? Color.white : new Color(1f, 1f, 1f, 0f);
        }, new Vector4(border, border, border, border));
    }

    #endregion

    #region Prefab 生成

    /// <summary>
    /// 直式縮圖卡片 Prefab。位置 / 縮放 / 亮度 / 透明度都由 ChapterCarouselController 推進來，
    /// 這裡只負責外觀組成：光暈（選中）→ 底板 → 縮圖 → 描邊（選中）→ 鎖定遮罩 → 編號。
    /// </summary>
    private static GameObject CreateChapterCardPrefab(Sprite glow, Sprite outline)
    {
        var go = new GameObject("ChapterCard",
            typeof(RectTransform), typeof(CanvasGroup), typeof(ChapterCarouselItem));
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(CardWidth, CardHeight);

        // 光暈：比卡片大一圈，選中時才啟用
        var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowGo.transform.SetParent(go.transform, false);
        var glowRt = (RectTransform)glowGo.transform;
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(CardWidth + 96f, CardHeight + 96f);
        var glowImg = glowGo.GetComponent<Image>();
        glowImg.sprite = glow;
        glowImg.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.45f);
        glowImg.raycastTarget = false;
        glowGo.SetActive(false);

        // 底板
        var frameGo = new GameObject("Frame", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        frameGo.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)frameGo.transform);
        var frameImg = frameGo.GetComponent<Image>();
        frameImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        frameImg.type = Image.Type.Sliced;
        frameImg.color = CardFrameColor;
        frameImg.raycastTarget = false;

        // 縮圖：唯一接收點擊的 Graphic（點擊由 ChapterCarouselItem 以 IPointerClickHandler 接）
        var thumbGo = new GameObject("Thumbnail", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        thumbGo.transform.SetParent(go.transform, false);
        var thumbRt = (RectTransform)thumbGo.transform;
        thumbRt.anchorMin = Vector2.zero;
        thumbRt.anchorMax = Vector2.one;
        thumbRt.offsetMin = new Vector2(6f, 6f);
        thumbRt.offsetMax = new Vector2(-6f, -6f);
        var thumbImg = thumbGo.GetComponent<Image>();
        thumbImg.color = new Color(0.32f, 0.34f, 0.42f, 1f); // 未指定縮圖時的佔位色
        thumbImg.raycastTarget = true;
        thumbImg.preserveAspect = false;

        // 高亮描邊：選中時才啟用
        var outlineGo = new GameObject("SelectedOutline", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        outlineGo.transform.SetParent(go.transform, false);
        var outlineRt = (RectTransform)outlineGo.transform;
        outlineRt.anchorMin = Vector2.zero;
        outlineRt.anchorMax = Vector2.one;
        outlineRt.offsetMin = new Vector2(-3f, -3f);
        outlineRt.offsetMax = new Vector2(3f, 3f);
        var outlineImg = outlineGo.GetComponent<Image>();
        outlineImg.sprite = outline;
        outlineImg.type = Image.Type.Sliced;
        outlineImg.color = AccentColor;
        outlineImg.raycastTarget = false;
        outlineGo.SetActive(false);

        // 鎖定遮罩
        var lockGo = new GameObject("LockedMark", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        lockGo.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)lockGo.transform);
        var lockImg = lockGo.GetComponent<Image>();
        lockImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        lockImg.type = Image.Type.Sliced;
        lockImg.color = new Color(0f, 0f, 0f, 0.62f);
        lockImg.raycastTarget = false;

        var lockLabel = CreateTMP(lockGo.transform, "LockedLabel", "未解鎖", 22,
            TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.8f));
        StretchFull((RectTransform)lockLabel.transform);
        lockGo.SetActive(false);

        // 章節編號
        var numberText = CreateTMP(go.transform, "NumberText", "1", 24,
            TextAlignmentOptions.BottomLeft, new Color(1f, 1f, 1f, 0.85f));
        numberText.fontStyle = FontStyles.Bold;
        var numberRt = (RectTransform)numberText.transform;
        numberRt.anchorMin = new Vector2(0f, 0f);
        numberRt.anchorMax = new Vector2(1f, 0f);
        numberRt.pivot = new Vector2(0.5f, 0f);
        numberRt.offsetMin = new Vector2(12f, 10f);
        numberRt.offsetMax = new Vector2(-12f, 42f);

        var so = new SerializedObject(go.GetComponent<ChapterCarouselItem>());
        so.FindProperty("canvasGroup").objectReferenceValue = go.GetComponent<CanvasGroup>();
        so.FindProperty("thumbnailImage").objectReferenceValue = thumbImg;
        so.FindProperty("selectedOutline").objectReferenceValue = outlineGo;
        so.FindProperty("selectedGlow").objectReferenceValue = glowGo;
        so.FindProperty("lockedMark").objectReferenceValue = lockGo;
        so.FindProperty("numberText").objectReferenceValue = numberText;

        // 亮度乘算的對象：底板 + 縮圖 + 編號（各自乘在原本的顏色上）
        SerializedProperty tint = so.FindProperty("tintTargets");
        tint.arraySize = 3;
        tint.GetArrayElementAtIndex(0).objectReferenceValue = frameImg;
        tint.GetArrayElementAtIndex(1).objectReferenceValue = thumbImg;
        tint.GetArrayElementAtIndex(2).objectReferenceValue = numberText;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SaveAsPrefab(go, PrefabDir + "/ChapterCard.prefab");
    }

    /// <summary>
    /// 大圖層 Prefab：純一張 Image + AspectRatioFitter，共用一個 CanvasGroup 淡化。
    /// 不加模糊底圖、不加遮罩；切圖時把 Sprite 比例寫進 fitter，絕不各自改 width / height。
    /// ChapterHeroView 會依章節數量生成並重複利用；要改大圖區的樣式改這個 Prefab 即可。
    /// </summary>
    private static ChapterHeroLayer CreateHeroLayerPrefab()
    {
        var go = new GameObject("ChapterHeroLayer",
            typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer),
            typeof(Image), typeof(AspectRatioFitter), typeof(ChapterHeroLayer));
        var rt = (RectTransform)go.transform;
        // 錨點置中：交給 AspectRatioFitter 依比例縮放並置中
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(HeroWidth, HeroHeight);
        go.GetComponent<CanvasGroup>().alpha = 0f; // 生成時不顯示，由 ChapterHeroView 淡入

        var img = go.GetComponent<Image>();
        img.raycastTarget = false;
        img.preserveAspect = false; // 比例交給 AspectRatioFitter，Image 自己不要再套

        var fitter = go.GetComponent<AspectRatioFitter>();
        fitter.aspectMode = AspectRatioFitter.AspectMode.FitInParent; // 預設 KeepAspect
        fitter.aspectRatio = HeroWidth / HeroHeight;                  // 執行期會依實際 Sprite 覆寫

        var so = new SerializedObject(go.GetComponent<ChapterHeroLayer>());
        so.FindProperty("canvasGroup").objectReferenceValue = go.GetComponent<CanvasGroup>();
        so.FindProperty("image").objectReferenceValue = img;
        so.FindProperty("fitter").objectReferenceValue = fitter;
        so.ApplyModifiedPropertiesWithoutUndo();

        GameObject asset = SaveAsPrefab(go, PrefabDir + "/ChapterHeroLayer.prefab");
        return asset != null ? asset.GetComponent<ChapterHeroLayer>() : null;
    }

    /// <summary>
    /// 前景面板 Prefab：本身透明、不擋點擊（背景圖從下層透出）。
    /// 只含 MetaPanel（左側標題區）與 SelectorPanel（中央縮圖輪播）。
    /// </summary>
    private static GameObject CreateCarouselPanelPrefab()
    {
        var go = new GameObject("ChapterCarouselPanel", typeof(RectTransform));
        StretchFull((RectTransform)go.transform);

        BuildMetaPanel(go.transform);
        BuildSelectorPanel(go.transform);

        return SaveAsPrefab(go, PrefabDir + "/ChapterCarouselPanel.prefab");
    }

    /// <summary>
    /// 全螢幕主題背景 Prefab：一個 FullScreen 模式的 ChapterHeroView，
    /// 依章節生成並重複利用 ChapterHeroLayer（EnvelopeParent 填滿裁切），切章節時交叉淡入淡出。
    /// 圖片來源設為 backgroundImage（未填則依序退回 blurred → hero → 縮圖）。
    /// 結構：本物件掛 RectMask2D 裁切溢出；子物件 Layers（大圖層容器）→ DimScrim（可讀性暗化）→ PageLabel（右下頁碼）。
    /// sendToBackInFullScreen 關閉：階層順序改由組裝 Canvas 時管理（要疊在最底層純黑背景之上，而非壓到黑底之下）。
    /// </summary>
    private static GameObject CreateBackgroundPrefab(ChapterHeroLayer heroLayerPrefab)
    {
        var go = new GameObject("ChapterBackground",
            typeof(RectTransform), typeof(RectMask2D), typeof(ChapterHeroView));
        StretchFull((RectTransform)go.transform);
        go.GetComponent<RectMask2D>().softness = Vector2Int.zero; // 只裁切、不柔邊

        // 大圖層容器：每個章節一層，由 ChapterHeroView 在執行期生成
        var layersGo = new GameObject("Layers", typeof(RectTransform));
        layersGo.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)layersGo.transform);

        // 暗化層：疊在背景圖「之上、面板之下」，只壓背景不壓上層卡片，提升文字可讀性
        var scrimGo = new GameObject("DimScrim", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        scrimGo.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)scrimGo.transform);
        var scrimImg = scrimGo.GetComponent<Image>();
        scrimImg.sprite = null;
        scrimImg.color = new Color(0f, 0f, 0f, BackgroundScrimAlpha);
        scrimImg.raycastTarget = false;

        // 右下頁碼：疊在圖上，用描邊 + 柔陰影材質保證可讀
        var page = CreateTMP(go.transform, "PageLabel", "1 / 1", 26,
            TextAlignmentOptions.BottomRight, new Color(1f, 1f, 1f, 0.9f));
        if (s_heroTextMaterial != null) page.fontSharedMaterial = s_heroTextMaterial;
        var pageRt = (RectTransform)page.transform;
        pageRt.anchorMin = pageRt.anchorMax = new Vector2(1f, 0f);
        pageRt.pivot = new Vector2(1f, 0f);
        pageRt.anchoredPosition = new Vector2(-40f, 28f);
        pageRt.sizeDelta = new Vector2(260f, 40f);

        var so = new SerializedObject(go.GetComponent<ChapterHeroView>());
        so.FindProperty("layerPrefab").objectReferenceValue = heroLayerPrefab;
        so.FindProperty("layersRoot").objectReferenceValue = layersGo.GetComponent<RectTransform>();
        so.FindProperty("pageLabel").objectReferenceValue = page;
        so.FindProperty("fitMode").enumValueIndex = (int)ChapterHeroView.HeroFitMode.FullScreen;
        so.FindProperty("spriteSource").enumValueIndex = (int)ChapterHeroView.SpriteSource.BackgroundImage;
        so.FindProperty("sendToBackInFullScreen").boolValue = false; // 階層由 Canvas 組裝時管理（疊在黑底之上）
        so.FindProperty("referenceResolution").vector2Value = new Vector2(1920f, 1080f);
        so.FindProperty("crossfadeDuration").floatValue = 0.6f;
        so.FindProperty("placeholderColor").colorValue = new Color(0.03f, 0.03f, 0.05f, 1f); // 無圖時退回近黑
        so.ApplyModifiedPropertiesWithoutUndo();

        return SaveAsPrefab(go, PrefabDir + "/ChapterBackground.prefab");
    }

    /// <summary>
    /// 左側標題區（參考圖 2/3/5 的「Main Story / 主題曲 / 章節名」標題塊）：
    /// 靠左、垂直置中，標題 / 副標 / 描述由上而下左對齊堆疊；容器高度固定、描述以 Ellipsis 截斷，文案長短不會跳版。
    /// 標題套描邊材質，疊在明亮背景圖上仍可讀。
    /// </summary>
    private static GameObject BuildMetaPanel(Transform parent)
    {
        var go = new GameObject("MetaPanel",
            typeof(RectTransform), typeof(CanvasGroup), typeof(ChapterMetaView));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0f, 0.5f); // 靠左、垂直置中
        rt.pivot = new Vector2(0f, 0.5f);
        rt.sizeDelta = new Vector2(MetaWidth, MetaHeight);
        rt.anchoredPosition = new Vector2(MetaLeft, MetaCenterY);

        var title = CreateTMP(go.transform, "TitleText", "第 1 章　章節標題", 54,
            TextAlignmentOptions.TopLeft, Color.white);
        title.fontStyle = FontStyles.Bold;
        title.overflowMode = TextOverflowModes.Ellipsis;
        if (s_heroTextMaterial != null) title.fontSharedMaterial = s_heroTextMaterial;
        PlaceStretched(title, 0f, 72f);

        var subtitle = CreateTMP(go.transform, "SubtitleText", "章節副標", 28,
            TextAlignmentOptions.TopLeft, SubtitleColor);
        subtitle.overflowMode = TextOverflowModes.Ellipsis;
        PlaceStretched(subtitle, -78f, 38f);

        var description = CreateTMP(go.transform, "DescriptionText", "章節描述（2～3 行）。", 24,
            TextAlignmentOptions.TopLeft, DescriptionColor);
        description.overflowMode = TextOverflowModes.Ellipsis; // 超過固定高度就截斷，不撐開容器
        description.lineSpacing = 8f;
        PlaceStretched(description, -124f, 120f);

        var so = new SerializedObject(go.GetComponent<ChapterMetaView>());
        so.FindProperty("canvasGroup").objectReferenceValue = go.GetComponent<CanvasGroup>();
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("subtitleText").objectReferenceValue = subtitle;
        so.FindProperty("descriptionText").objectReferenceValue = description;
        so.FindProperty("fadeDuration").floatValue = 0.17f;
        so.ApplyModifiedPropertiesWithoutUndo();

        return go;
    }

    /// <summary>
    /// 中央樂章封面輪播區：RectMask2D + Softness 讓兩端的卡片是淡掉而不是被硬邊切斷。
    /// 卡片群中心相對畫面中央往右位移 SelectorItemsCenterX，讓出左側標題空間（貼近圖 2/3/5）。
    /// 導覽方式：點兩側卡片移到中央、拖曳、或鍵盤方向鍵；不使用左右箭頭。
    /// </summary>
    private static GameObject BuildSelectorPanel(Transform parent)
    {
        var go = new GameObject("SelectorPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(RectMask2D), typeof(ChapterCarouselDragArea));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        // 橫向鋪滿、垂直置中（選中卡片落在偏右的中心，左右卡片往兩側展開並淡出）
        rt.anchorMin = new Vector2(0f, 0.5f);
        rt.anchorMax = new Vector2(1f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(0f, SelectorHeight);
        rt.anchoredPosition = new Vector2(0f, SelectorCenterY);

        // 全透明但可接收 Raycast，讓整個區域都能拖曳
        var dragSurface = go.GetComponent<Image>();
        dragSurface.color = new Color(1f, 1f, 1f, 0f);
        dragSurface.raycastTarget = true;

        // 兩端淡出（不是硬邊裁切）
        var mask = go.GetComponent<RectMask2D>();
        mask.softness = new Vector2Int(SelectorMaskSoftness, 0);

        // 卡片容器：卡片以 anchoredPosition.x 相對這裡的中心排列；容器整體往右偏，讓出左側標題
        var itemsGo = new GameObject("ItemsRoot", typeof(RectTransform));
        itemsGo.transform.SetParent(go.transform, false);
        var itemsRt = (RectTransform)itemsGo.transform;
        itemsRt.anchorMin = itemsRt.anchorMax = new Vector2(0.5f, 0.5f);
        itemsRt.pivot = new Vector2(0.5f, 0.5f);
        itemsRt.sizeDelta = Vector2.zero;
        itemsRt.anchoredPosition = new Vector2(SelectorItemsCenterX, 0f);

        return go;
    }

    private static GameObject SaveAsPrefab(GameObject temp, string path)
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(temp, path);
        UnityEngine.Object.DestroyImmediate(temp);
        return asset;
    }

    #endregion

    #region Canvas 組裝與對話系統接線

    private static GameObject BuildCarouselCanvas(GameObject panelPrefab, GameObject backgroundPrefab,
        GameObject cardPrefab, ChapterListData chapterData)
    {
        var canvasGo = new GameObject("ChapterCarouselCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(ChapterCarouselController));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // 對話 UI 疊在選擇畫面之上

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 最底層墊一張全螢幕純黑背景圖（取代舊的共用 SceneBlackout），確保切換空檔不透出後方畫面
        AddCanvasBackdrop(canvasGo);

        // 全螢幕主題背景：疊在黑底之上、前景面板之下，切換章節時交叉淡化
        var backgroundGo = (GameObject)PrefabUtility.InstantiatePrefab(backgroundPrefab);
        backgroundGo.transform.SetParent(canvasGo.transform, false);
        StretchFull((RectTransform)backgroundGo.transform);
        backgroundGo.transform.SetSiblingIndex(1); // 黑底 Backdrop (0) 之上

        var panelGo = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab);
        panelGo.transform.SetParent(canvasGo.transform, false);
        StretchFull((RectTransform)panelGo.transform);

        Transform meta = panelGo.transform.Find("MetaPanel");
        Transform selector = panelGo.transform.Find("SelectorPanel");

        var controller = canvasGo.GetComponent<ChapterCarouselController>();

        var so = new SerializedObject(controller);
        so.FindProperty("chapterList").objectReferenceValue = chapterData;
        so.FindProperty("panelRoot").objectReferenceValue = panelGo;
        so.FindProperty("heroView").objectReferenceValue = backgroundGo.GetComponent<ChapterHeroView>();
        so.FindProperty("metaView").objectReferenceValue = meta.GetComponent<ChapterMetaView>();
        so.FindProperty("itemsRoot").objectReferenceValue = selector.Find("ItemsRoot").GetComponent<RectTransform>();
        so.FindProperty("defaultCardPrefab").objectReferenceValue = cardPrefab;
        // 已移除左右箭頭：導覽改為點兩側卡片 / 拖曳 / 鍵盤方向鍵（leftArrow / rightArrow 留空即可）
        so.FindProperty("parentCanvas").objectReferenceValue = canvas;
        so.FindProperty("step").floatValue = Step;
        so.FindProperty("decay").floatValue = 0.75f;
        so.FindProperty("brightnessDecay").floatValue = 0.74f;
        so.FindProperty("alphaDecay").floatValue = 0.62f;
        so.FindProperty("maxDistance").floatValue = 3f;
        so.FindProperty("raycastAlphaThreshold").floatValue = 0.12f;
        so.FindProperty("moveDuration").floatValue = 0.45f;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 拖曳感應區指回主控（面板是巢狀 Prefab 實例，這裡存成實例覆寫）
        var dragSo = new SerializedObject(selector.GetComponent<ChapterCarouselDragArea>());
        dragSo.FindProperty("carousel").objectReferenceValue = controller;
        dragSo.ApplyModifiedPropertiesWithoutUndo();

        return canvasGo;
    }

    private static GameObject InstantiateDialogueCanvas()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DialogueCanvasPrefabPath);
        if (prefab == null) return null;

        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.name = "DialogueCanvas";

        var canvas = go.GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = 20;

        Transform openBtn = go.transform.Find("OpenButton");
        if (openBtn != null) openBtn.gameObject.SetActive(false);

        return go;
    }

    private static bool BindDialogueSystem(GameObject carouselCanvasGo, GameObject dialogueCanvasGo)
    {
        if (dialogueCanvasGo == null)
        {
            Debug.LogWarning("[ChapterCarousel] 找不到 " + DialogueCanvasPrefabPath +
                             "，輪播畫面已產生但尚未接上對話系統。請先執行 Tools → Dialogue → Generate Dialogue UI。");
            return false;
        }

        var trigger = dialogueCanvasGo.GetComponent<TriggerDialogue>();
        var typer = dialogueCanvasGo.GetComponent<DialogueTypingEffect>();
        if (trigger == null && typer == null)
        {
            Debug.LogWarning("[ChapterCarousel] DialogueCanvas 上找不到 TriggerDialogue / DialogueTypingEffect，無法接上對話系統。");
            return false;
        }

        var so = new SerializedObject(carouselCanvasGo.GetComponent<ChapterCarouselController>());
        so.FindProperty("dialogueTrigger").objectReferenceValue = trigger;
        so.FindProperty("dialogueTyper").objectReferenceValue = typer;
        so.ApplyModifiedPropertiesWithoutUndo();
        return true;
    }

    #endregion

    #region 共用小工具

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (s_font != null) tmp.font = s_font; // 中文字型，避免顯示 □□□
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    /// <summary>把文字橫向撐滿父容器，並固定在指定的 y 位移與高度（維持固定版面）。</summary>
    private static void PlaceStretched(TMP_Text tmp, float y, float height)
    {
        var rt = (RectTransform)tmp.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(16f, 0f);
        rt.offsetMax = new Vector2(-16f, 0f);
        rt.sizeDelta = new Vector2(rt.sizeDelta.x, height);
        rt.anchoredPosition = new Vector2(0f, y);
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    /// <summary>
    /// 在 Canvas 最底層墊一張全螢幕純黑背景圖：沒有大圖 / 圖還沒淡入 / 畫面切換的空檔，
    /// 都不會透出後方的 Unity 場景。取代原本共用的 SceneBlackout，改由各 Canvas 自帶底板。
    /// </summary>
    private static void AddCanvasBackdrop(GameObject canvasGo)
    {
        if (canvasGo == null) return;
        var go = new GameObject("Backdrop", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(canvasGo.transform, false);
        StretchFull((RectTransform)go.transform);
        var img = go.GetComponent<Image>();
        img.sprite = null;
        img.color = Color.black;
        img.raycastTarget = false;
        go.transform.SetAsFirstSibling(); // 最底層，墊在所有 UI 之下
    }

    #endregion
}
