using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;
using Assets.Scripts.StoryNodeMap;

/// <summary>
/// 一鍵產生「選完大章節後，挑選該章節哪一段劇情來讀」的節點圖畫面（story node map）。
/// 版面：上方黑邊（章節標題）、下方黑邊（返回章節選擇按鈕），中間是可橫向捲動的劇情節點圖
/// （節點圓點 + 連線，點節點右側淡入詳情面板：分類 / 編號標題 / 段落劇情大綱 / 閱讀鈕）。
///
/// 使用方式：Unity 選單 → Tools → StoryNodeMap → Generate Story Node Map UI (章節劇情節點圖)
/// 產出：
///   Assets/Image/UI/                程序化生成的 UI 圖（StoryNodeDot / StoryNodeGlow / StoryNodeLine / StoryNodeButtonFrame）
///   Assets/Prefabs/StoryNodeMap/    StoryNode / StoryNodeMapPanel / StoryNodeMapCanvas
///   Assets/Data/StoryNodeMap/       SampleStoryNodeGraph.asset（範例節點圖，已存在則不覆蓋內容）
///   Assets/Scenes/StoryNodeMapSample.unity（節點圖 + 對話 UI 的範例場景）
/// 可重複執行：會覆蓋更新既有產出（GUID 不變，引用不會斷）；範例節點圖已存在時保留使用者內容不覆寫。
///
/// 新增 / 調整劇情節點時只要改節點圖資訊表（加節點、連線、補大綱與對話表），不必改程式也不必重跑本工具。
///
/// 此工具是由 A_Good_Ink 使用 AI 生成的工具。
/// </summary>
public static class StoryNodeMapUISetupTool
{
    private const string PrefabDir = "Assets/Prefabs/StoryNodeMap";
    private const string ImageDir = "Assets/Image/UI";
    private const string DataDir = "Assets/Data/StoryNodeMap";
    private const string SceneDir = "Assets/Scenes";
    private const string ScenePath = SceneDir + "/StoryNodeMapSample.unity";
    private const string SampleGraphPath = DataDir + "/SampleStoryNodeGraph.asset";

    // 既有對話系統的產出（本工具直接重用）
    private const string DialogueCanvasPrefabPath = "Assets/Prefabs/Dialogue/DialogueCanvas.prefab";
    private const string SampleDialoguePath = "Assets/Data/Dialogue/SampleDialogue.asset";

    // 中文字型（與對話 / 章節選擇畫面共用同一份 SDF）
    private const string FontSourcePath = "Assets/Font/TaipeiSansTCBeta-Regular.ttf";
    private const string FontAssetPath = "Assets/Font/TaipeiSansTCBeta-Regular SDF.asset";
    private static TMP_FontAsset s_font;

    // 程序化生成的 UI 圖
    private const string DotPath = ImageDir + "/StoryNodeDot.png";
    private const string GlowPath = ImageDir + "/StoryNodeGlow.png";
    private const string LinePath = ImageDir + "/StoryNodeLine.png";
    private const string FramePath = ImageDir + "/StoryNodeButtonFrame.png";

    // 版面尺寸（1920 x 1080 參考解析度）
    private const float TopBarHeight = 76f;
    private const float BottomBarHeight = 76f;
    private const float DetailWidth = 660f;
    private const float NodeSize = 46f;
    private const float GlowSize = 132f;
    private const float EdgeThickness = 7f;
    private const float ContentRightPadding = 360f;

    // 配色
    private static readonly Color MapBackColor = new Color(0.30f, 0.32f, 0.42f, 1f);   // 中間節點圖底色（藍灰）
    private static readonly Color BarColor = new Color(0.02f, 0.02f, 0.03f, 1f);        // 上下黑邊
    private static readonly Color DetailBackColor = new Color(0.06f, 0.07f, 0.10f, 0.88f); // 右側詳情面板（半透明深色）
    private static readonly Color EdgeColor = new Color(0.05f, 0.05f, 0.07f, 1f);       // 連線
    private static readonly Color NodeNormalColor = new Color(0.62f, 0.62f, 0.64f, 1f);
    private static readonly Color NodeSelectedColor = new Color(0.98f, 0.96f, 0.80f, 1f);
    private static readonly Color NodeLockedColor = new Color(0.40f, 0.40f, 0.44f, 1f);
    private static readonly Color AccentColor = new Color(0.98f, 0.93f, 0.62f, 1f);     // 分類 / 閱讀
    private static readonly Color SummaryColor = new Color(1f, 1f, 1f, 0.72f);

    [MenuItem("Tools/StoryNodeMap/Generate Story Node Map UI (章節劇情節點圖)")]
    public static void Generate()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabDir);
        EnsureFolder(ImageDir);
        EnsureFolder(DataDir);
        EnsureFolder(SceneDir);

        s_font = LoadOrCreateChineseFontAsset();

        Sprite dot = GenerateDotSprite();
        Sprite glow = GenerateGlowSprite();
        Sprite line = GenerateLineSprite();
        Sprite frame = GenerateFrameSprite();

        StoryNodeGraphData graphData = CreateOrLoadSampleGraph();

        GameObject nodePrefab = CreateNodePrefab(dot, glow);
        GameObject panelPrefab = CreatePanelPrefab(line, frame);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject canvasGo = BuildCanvas(panelPrefab, nodePrefab, graphData, line);
        GameObject canvasPrefab = PrefabUtility.SaveAsPrefabAssetAndConnect(
            canvasGo, PrefabDir + "/StoryNodeMapCanvas.prefab", InteractionMode.AutomatedAction);

        GameObject dialogueCanvasGo = InstantiateDialogueCanvas();
        bool dialogueLinked = BindDialogueSystem(canvasGo, dialogueCanvasGo);

        new GameObject("EventSystem", typeof(InteractionOfUI), typeof(StandaloneInputModule));

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Selection.activeObject = canvasPrefab;

        EditorUtility.DisplayDialog("Story Node Map UI",
            "產生完成！\n\n" +
            "Prefabs：" + PrefabDir + "\n" +
            "　　StoryNode.prefab（節點圓點）\n" +
            "　　StoryNodeMapPanel.prefab（黑邊 + 捲動節點圖 + 詳情面板）\n" +
            "　　StoryNodeMapCanvas.prefab（整組畫面）\n" +
            "UI 圖：" + ImageDir + "\n" +
            "範例節點圖：" + SampleGraphPath + "\n" +
            "範例場景：" + ScenePath + "\n\n" +
            (dialogueLinked
                ? "已自動接上場景中的 DialogueCanvas。\n按 Play 後：點節點會發光並置中視野、右側詳情面板滑入；\n點空白背景取消選取、面板滑出；按「閱讀」播放該段劇情。"
                : "找不到 " + DialogueCanvasPrefabPath + "\n請先執行 Tools → Dialogue → Generate Dialogue UI，再重跑本工具以自動接上對話系統。") + "\n\n" +
            "接章節選擇：\n1) 在章節列表資訊表 (ChapterListData) 的各章節上，把此章節的節點圖指定到 nodeGraph 欄位。\n" +
            "2) 場景加一個空物件掛 ChapterStoryMapBridge，指定 carousel 與 nodeMap 即可\n（選章時會自動依 ChapterListData 找到該章節的 nodeGraph 並開啟節點圖）。",
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
            Debug.LogWarning("[StoryNodeMap] 找不到 " + FontSourcePath + "，文字將使用 TMP 預設字型（中文可能顯示為方框）。");
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

    #endregion

    #region 程序化生成的 UI 圖

    /// <summary>
    /// 產生（或覆蓋更新）一張 PNG 並匯入為 Sprite。覆寫同路徑、.meta 保留 → GUID 不變、引用不斷。
    /// </summary>
    private static Sprite GenerateSprite(string assetPath, int width, int height,
        System.Func<int, int, Color> pixelFunc, Vector4 border)
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
        Object.DestroyImmediate(tex);
        if (png == null)
        {
            Debug.LogWarning("[StoryNodeMap] 產生 " + assetPath + " 失敗（PNG 編碼失敗）。");
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
            settings.spriteBorder = border;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite == null) Debug.LogWarning("[StoryNodeMap] " + assetPath + " 匯入後找不到 Sprite。");
        return sprite;
    }

    /// <summary>節點圓點（實心、柔邊圓形）。</summary>
    private static Sprite GenerateDotSprite()
    {
        const int size = 64;
        float center = (size - 1) * 0.5f;
        float radius = center - 1f;
        return GenerateSprite(DotPath, size, size, (x, y) =>
        {
            float dx = x - center;
            float dy = y - center;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Clamp01(radius - d + 0.5f); // 邊緣 1px 抗鋸齒
            return new Color(1f, 1f, 1f, a);
        }, Vector4.zero);
    }

    /// <summary>選中節點的柔光暈（徑向柔邊白光，用 Image.color 上色）。</summary>
    private static Sprite GenerateGlowSprite()
    {
        const int size = 128;
        float center = (size - 1) * 0.5f;
        return GenerateSprite(GlowPath, size, size, (x, y) =>
        {
            float dx = (x - center) / center;
            float dy = (y - center) / center;
            float d = Mathf.Sqrt(dx * dx + dy * dy);
            float a = Mathf.Pow(Mathf.Clamp01(1f - d), 2.4f);
            return new Color(1f, 1f, 1f, a);
        }, Vector4.zero);
    }

    /// <summary>連線用的純白方塊（Image 以 Simple 拉伸成線段，端點乾淨不圓角）。</summary>
    private static Sprite GenerateLineSprite()
    {
        const int size = 8;
        return GenerateSprite(LinePath, size, size, (x, y) => Color.white, Vector4.zero);
    }

    /// <summary>返回按鈕的細框（中空 9-slice 方框，邊寬 3px）。</summary>
    private static Sprite GenerateFrameSprite()
    {
        const int size = 24;
        const int border = 3;
        return GenerateSprite(FramePath, size, size, (x, y) =>
        {
            bool onBorder = x < border || x >= size - border || y < border || y >= size - border;
            return onBorder ? Color.white : new Color(1f, 1f, 1f, 0f);
        }, new Vector4(border, border, border, border));
    }

    #endregion

    #region 範例節點圖資料

    /// <summary>
    /// 載入或建立範例節點圖。已存在時保留使用者內容不覆寫（GUID 穩定）；不存在時建一份示意分支圖。
    /// </summary>
    private static StoryNodeGraphData CreateOrLoadSampleGraph()
    {
        var existing = AssetDatabase.LoadAssetAtPath<StoryNodeGraphData>(SampleGraphPath);
        if (existing != null) return existing;

        var data = ScriptableObject.CreateInstance<StoryNodeGraphData>();
        data.mapTitle = "(1) 本章節";
        data.nodeTitleFormat = "{0}　{1}";

        var sampleDialogue = AssetDatabase.LoadAssetAtPath<DialogueData>(SampleDialoguePath);

        // 索引：0~4 主線；5,6 上分支（會匯回主線）；7 下分支結局（死路）
        //   1-3 於岔路同時連往主線 1-4(3)、上分支起點(5)、下分支結局(7)
        //   上分支 1-3-1(5) → 1-3-2(6) → 匯回 1-4(3)
        data.nodes = new List<StoryNodeGraphData.StoryNode>
        {
            Node("1-1",   "序幕",   StoryNodeType.MainLine,     new Vector2(240f, 0f),     sampleDialogue, 1),
            Node("1-2",   "啟程",   StoryNodeType.MainLine,     new Vector2(540f, 0f),     sampleDialogue, 2),
            Node("1-3",   "岔路",   StoryNodeType.MainLine,     new Vector2(840f, 0f),     sampleDialogue, 3, 5, 7),
            Node("1-4",   "匯流",   StoryNodeType.MainLine,     new Vector2(1240f, 0f),    sampleDialogue, 4),
            Node("1-5",   "終幕",   StoryNodeType.Ending,       new Vector2(1680f, 0f),    sampleDialogue),
            Node("1-3-1", "岔路支線", StoryNodeType.Branch,       new Vector2(1040f, 240f),  sampleDialogue, 6),
            Node("1-3-2", "支線續章", StoryNodeType.Branch,       new Vector2(1400f, 240f),  sampleDialogue, 3),
            Node("1-4-1", "支線結局", StoryNodeType.BranchEnding,  new Vector2(1040f, -240f), sampleDialogue),
        };

        AssetDatabase.CreateAsset(data, SampleGraphPath);
        AssetDatabase.SaveAssets();
        return data;
    }

    private static StoryNodeGraphData.StoryNode Node(string id, string title, StoryNodeType type,
        Vector2 pos, DialogueData dialogue, params int[] connections)
    {
        return new StoryNodeGraphData.StoryNode
        {
            id = id,
            title = title,
            summary = "段落劇情大綱……（在節點圖資訊表填入此段落的簡介，未解鎖時自動隱藏）。",
            type = type,
            graphPosition = pos,
            dialogueData = dialogue,
            unlocked = true,
            connections = new List<int>(connections),
        };
    }

    #endregion

    #region Prefab 生成

    /// <summary>
    /// 節點圓點 Prefab：容器（掛 StoryNodeItem）底下放光暈（選中才亮）與圓點（唯一可點）。
    /// 光暈排在圓點之前 → 畫在圓點下方。
    /// </summary>
    private static GameObject CreateNodePrefab(Sprite dot, Sprite glow)
    {
        var go = new GameObject("StoryNode", typeof(RectTransform), typeof(StoryNodeItem));
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = new Vector2(NodeSize, NodeSize);

        // 光暈：比圓點大一圈，選中時才啟用
        var glowGo = new GameObject("Glow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        glowGo.transform.SetParent(go.transform, false);
        var glowRt = (RectTransform)glowGo.transform;
        glowRt.anchorMin = glowRt.anchorMax = new Vector2(0.5f, 0.5f);
        glowRt.sizeDelta = new Vector2(GlowSize, GlowSize);
        var glowImg = glowGo.GetComponent<Image>();
        glowImg.sprite = glow;
        glowImg.color = new Color(AccentColor.r, AccentColor.g, AccentColor.b, 0.5f);
        glowImg.raycastTarget = false;
        glowGo.SetActive(false);

        // 圓點：唯一接收點擊的 Graphic（點擊冒泡給容器上的 StoryNodeItem）
        var dotGo = new GameObject("Dot", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        dotGo.transform.SetParent(go.transform, false);
        StretchFull((RectTransform)dotGo.transform);
        var dotImg = dotGo.GetComponent<Image>();
        dotImg.sprite = dot;
        dotImg.color = NodeNormalColor;
        dotImg.raycastTarget = true;
        dotImg.preserveAspect = true;

        var so = new SerializedObject(go.GetComponent<StoryNodeItem>());
        so.FindProperty("nodeImage").objectReferenceValue = dotImg;
        so.FindProperty("selectedGlow").objectReferenceValue = glowGo;
        so.FindProperty("normalColor").colorValue = NodeNormalColor;
        so.FindProperty("selectedColor").colorValue = NodeSelectedColor;
        so.FindProperty("lockedColor").colorValue = NodeLockedColor;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SaveAsPrefab(go, PrefabDir + "/StoryNode.prefab");
    }

    /// <summary>整個節點圖面板 Prefab：藍灰底 + 捲動節點圖 + 右側詳情面板 + 上下黑邊。</summary>
    private static GameObject CreatePanelPrefab(Sprite line, Sprite frame)
    {
        var go = new GameObject("StoryNodeMapPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        StretchFull((RectTransform)go.transform);
        var bg = go.GetComponent<Image>();
        bg.color = MapBackColor;
        bg.raycastTarget = true;

        BuildScrollArea(go.transform);
        BuildDetailPanel(go.transform);
        BuildTopBar(go.transform);
        BuildBottomBar(go.transform, frame);

        return SaveAsPrefab(go, PrefabDir + "/StoryNodeMapPanel.prefab");
    }

    /// <summary>中間可橫向捲動的節點圖區（ScrollRect + Viewport + Content + Edges）。</summary>
    private static void BuildScrollArea(Transform parent)
    {
        var scrollGo = new GameObject("NodeScroll",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(ScrollRect));
        scrollGo.transform.SetParent(parent, false);
        var scrollRt = (RectTransform)scrollGo.transform;
        scrollRt.anchorMin = Vector2.zero;
        scrollRt.anchorMax = Vector2.one;
        scrollRt.offsetMin = new Vector2(0f, BottomBarHeight);
        scrollRt.offsetMax = new Vector2(0f, -TopBarHeight);
        var scrollSurface = scrollGo.GetComponent<Image>(); // 透明但可接 Raycast，空白處也能拖曳捲動
        scrollSurface.color = new Color(1f, 1f, 1f, 0f);
        scrollSurface.raycastTarget = true;

        var viewportGo = new GameObject("Viewport",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(RectMask2D), typeof(StoryNodeMapBackground));
        viewportGo.transform.SetParent(scrollGo.transform, false);
        StretchFull((RectTransform)viewportGo.transform);
        var viewportImg = viewportGo.GetComponent<Image>();
        viewportImg.color = new Color(1f, 1f, 1f, 0f);
        viewportImg.raycastTarget = true; // 空白背景可接點擊（取消選取）與拖曳（捲動）

        var contentGo = new GameObject("Content", typeof(RectTransform));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = (RectTransform)contentGo.transform;
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(0f, 1f);
        contentRt.pivot = new Vector2(0f, 0.5f);
        contentRt.sizeDelta = new Vector2(1920f, 0f); // 執行期由控制器依節點重算寬度
        contentRt.anchoredPosition = Vector2.zero;

        // 連線容器：排在 Content 最前面 → 線畫在節點之下
        var edgesGo = new GameObject("Edges", typeof(RectTransform));
        edgesGo.transform.SetParent(contentGo.transform, false);
        var edgesRt = (RectTransform)edgesGo.transform;
        edgesRt.anchorMin = edgesRt.anchorMax = new Vector2(0f, 0.5f);
        edgesRt.pivot = new Vector2(0f, 0.5f);
        edgesRt.sizeDelta = Vector2.zero;
        edgesRt.anchoredPosition = Vector2.zero;

        var scroll = scrollGo.GetComponent<ScrollRect>();
        scroll.content = contentRt;
        scroll.viewport = (RectTransform)viewportGo.transform;
        scroll.horizontal = true;
        scroll.vertical = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.inertia = true;
        scroll.decelerationRate = 0.135f;
        scroll.scrollSensitivity = 30f;
    }

    /// <summary>右側詳情面板：半透明深色底 + 分類 / 編號標題 / 段落大綱 / 閱讀鈕；未選節點時整片淡出。</summary>
    private static void BuildDetailPanel(Transform parent)
    {
        var go = new GameObject("DetailPanel",
            typeof(RectTransform), typeof(CanvasGroup), typeof(CanvasRenderer),
            typeof(Image), typeof(StoryNodeDetailView));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(1f, 0f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.offsetMin = new Vector2(-DetailWidth, BottomBarHeight);
        rt.offsetMax = new Vector2(0f, -TopBarHeight);

        var bg = go.GetComponent<Image>();
        bg.color = DetailBackColor;
        bg.raycastTarget = true; // 面板上的點擊不穿透到後面的節點

        go.GetComponent<CanvasGroup>().alpha = 0f; // 一開始淡出，等選中節點才顯示

        // 分類（分支 / 分支結局 / 結局…）
        var category = CreateTMP(go.transform, "CategoryText", "分支", 30,
            TextAlignmentOptions.TopLeft, AccentColor);
        PlaceTop(category, -60f, 46f, 60f);

        // 編號 + 標題
        var title = CreateTMP(go.transform, "TitleText", "1-3　標題", 52,
            TextAlignmentOptions.TopLeft, Color.white);
        title.fontStyle = FontStyles.Bold;
        title.overflowMode = TextOverflowModes.Ellipsis;
        PlaceTop(title, -116f, 92f, 60f);

        // 段落劇情大綱
        var summary = CreateTMP(go.transform, "SummaryText", "段落劇情大綱……", 26,
            TextAlignmentOptions.TopLeft, SummaryColor);
        summary.overflowMode = TextOverflowModes.Ellipsis;
        summary.lineSpacing = 10f;
        var summaryRt = (RectTransform)summary.transform;
        summaryRt.anchorMin = new Vector2(0f, 0f);
        summaryRt.anchorMax = new Vector2(1f, 1f);
        summaryRt.offsetMin = new Vector2(60f, 200f); // 底部留給閱讀鈕
        summaryRt.offsetMax = new Vector2(-60f, -260f);

        // 閱讀按鈕（純文字，TMP 自身即點擊目標）
        var readGo = new GameObject("ReadButton", typeof(RectTransform), typeof(Button));
        readGo.transform.SetParent(go.transform, false);
        var readRt = (RectTransform)readGo.transform;
        readRt.anchorMin = readRt.anchorMax = new Vector2(0.5f, 0f);
        readRt.pivot = new Vector2(0.5f, 0f);
        readRt.sizeDelta = new Vector2(280f, 92f);
        readRt.anchoredPosition = new Vector2(0f, 70f);

        var readLabel = CreateTMP(readGo.transform, "Label", "閱讀", 46,
            TextAlignmentOptions.Center, AccentColor);
        readLabel.fontStyle = FontStyles.Bold;
        readLabel.raycastTarget = true; // 作為按鈕的點擊目標
        StretchFull((RectTransform)readLabel.transform);

        var readBtn = readGo.GetComponent<Button>();
        readBtn.targetGraphic = readLabel;
        var colors = readBtn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 1f, 1f, 0.75f);
        colors.pressedColor = new Color(1f, 1f, 1f, 0.55f);
        colors.disabledColor = new Color(1f, 1f, 1f, 0.30f);
        colors.fadeDuration = 0.08f;
        readBtn.colors = colors;

        var so = new SerializedObject(go.GetComponent<StoryNodeDetailView>());
        so.FindProperty("canvasGroup").objectReferenceValue = go.GetComponent<CanvasGroup>();
        so.FindProperty("categoryText").objectReferenceValue = category;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("summaryText").objectReferenceValue = summary;
        so.FindProperty("readButton").objectReferenceValue = readBtn;
        so.FindProperty("slideDuration").floatValue = 0.24f;
        so.FindProperty("extraSlide").floatValue = 48f;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>上方黑邊：章節標題（靠左）。</summary>
    private static void BuildTopBar(Transform parent)
    {
        var go = new GameObject("TopBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.sizeDelta = new Vector2(0f, TopBarHeight);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = BarColor;
        img.raycastTarget = true;

        var title = CreateTMP(go.transform, "TitleText", "(數字)本章節", 40,
            TextAlignmentOptions.Left, Color.white);
        var titleRt = (RectTransform)title.transform;
        titleRt.anchorMin = new Vector2(0f, 0.5f);
        titleRt.anchorMax = new Vector2(1f, 0.5f);
        titleRt.pivot = new Vector2(0f, 0.5f);
        titleRt.sizeDelta = new Vector2(-80f, 60f);
        titleRt.anchoredPosition = new Vector2(40f, 0f);
    }

    /// <summary>下方黑邊：返回章節選擇按鈕（靠右，細框 + 文字）。</summary>
    private static void BuildBottomBar(Transform parent, Sprite frame)
    {
        var go = new GameObject("BottomBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, BottomBarHeight);
        rt.anchoredPosition = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = BarColor;
        img.raycastTarget = true;

        var btnGo = new GameObject("ReturnButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        btnGo.transform.SetParent(go.transform, false);
        var btnRt = (RectTransform)btnGo.transform;
        btnRt.anchorMin = btnRt.anchorMax = new Vector2(1f, 0.5f);
        btnRt.pivot = new Vector2(1f, 0.5f);
        btnRt.sizeDelta = new Vector2(300f, 58f);
        btnRt.anchoredPosition = new Vector2(-32f, 0f);

        var frameImg = btnGo.GetComponent<Image>();
        frameImg.sprite = frame;
        frameImg.type = Image.Type.Sliced;
        frameImg.color = new Color(1f, 1f, 1f, 0.9f);
        frameImg.raycastTarget = true;

        var label = CreateTMP(btnGo.transform, "Label", "返回章節選擇", 32,
            TextAlignmentOptions.Center, Color.white);
        StretchFull((RectTransform)label.transform);

        var btn = btnGo.GetComponent<Button>();
        btn.targetGraphic = frameImg;
        var colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(0.85f, 0.85f, 0.85f, 1f);
        colors.pressedColor = new Color(0.65f, 0.65f, 0.65f, 1f);
        colors.fadeDuration = 0.08f;
        btn.colors = colors;
    }

    private static GameObject SaveAsPrefab(GameObject temp, string path)
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return asset;
    }

    #endregion

    #region Canvas 組裝與對話系統接線

    private static GameObject BuildCanvas(GameObject panelPrefab, GameObject nodePrefab,
        StoryNodeGraphData graphData, Sprite line)
    {
        var canvasGo = new GameObject("StoryNodeMapCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster), typeof(StoryNodeMapController));
        var canvas = canvasGo.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 5; // 對話 UI 疊在節點圖之上

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // 最底層墊一張全螢幕純黑背景圖（取代舊的共用 SceneBlackout），確保切換空檔不透出後方畫面
        AddCanvasBackdrop(canvasGo);

        var panelGo = (GameObject)PrefabUtility.InstantiatePrefab(panelPrefab);
        panelGo.transform.SetParent(canvasGo.transform, false);
        StretchFull((RectTransform)panelGo.transform);

        Transform scroll = panelGo.transform.Find("NodeScroll");
        Transform content = scroll.Find("Viewport/Content");
        Transform edges = content.Find("Edges");
        Transform detail = panelGo.transform.Find("DetailPanel");
        Transform topTitle = panelGo.transform.Find("TopBar/TitleText");
        Transform returnBtn = panelGo.transform.Find("BottomBar/ReturnButton");

        var controller = canvasGo.GetComponent<StoryNodeMapController>();
        var detailView = detail.GetComponent<StoryNodeDetailView>();

        var so = new SerializedObject(controller);
        so.FindProperty("graph").objectReferenceValue = graphData;
        so.FindProperty("panelRoot").objectReferenceValue = panelGo;
        so.FindProperty("titleText").objectReferenceValue = topTitle.GetComponent<TMP_Text>();
        so.FindProperty("scrollRect").objectReferenceValue = scroll.GetComponent<ScrollRect>();
        so.FindProperty("content").objectReferenceValue = content.GetComponent<RectTransform>();
        so.FindProperty("edgesRoot").objectReferenceValue = edges.GetComponent<RectTransform>();
        so.FindProperty("detailView").objectReferenceValue = detailView;
        so.FindProperty("defaultNodePrefab").objectReferenceValue = nodePrefab;
        so.FindProperty("edgeSprite").objectReferenceValue = line;
        so.FindProperty("edgeColor").colorValue = EdgeColor;
        so.FindProperty("edgeThickness").floatValue = EdgeThickness;
        so.FindProperty("contentRightPadding").floatValue = ContentRightPadding;
        so.FindProperty("focusOnSelect").boolValue = true;
        so.FindProperty("viewFocusRightInset").floatValue = DetailWidth * 0.5f;
        so.FindProperty("centerVertically").boolValue = true;
        so.FindProperty("focusDuration").floatValue = 0.35f;
        so.FindProperty("selectFirstOnOpen").boolValue = false;
        so.FindProperty("rebuildOnEnable").boolValue = true;
        so.FindProperty("hidePanelOnPlay").boolValue = true;
        so.FindProperty("reopenAfterDialogue").boolValue = true;
        so.FindProperty("hidePanelOnReturn").boolValue = true;
        so.ApplyModifiedPropertiesWithoutUndo();

        // 閱讀鈕 → 控制器（持久化監聽，Inspector 看得到）
        var readBtn = detailView.ReadButton;
        if (readBtn != null) UnityEventTools.AddPersistentListener(readBtn.onClick, controller.Btn_ReadSelected);

        // 返回章節選擇 → 控制器
        var returnButton = returnBtn.GetComponent<Button>();
        if (returnButton != null) UnityEventTools.AddPersistentListener(returnButton.onClick, controller.Btn_Return);

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

    private static bool BindDialogueSystem(GameObject canvasGo, GameObject dialogueCanvasGo)
    {
        if (dialogueCanvasGo == null)
        {
            Debug.LogWarning("[StoryNodeMap] 找不到 " + DialogueCanvasPrefabPath +
                             "，節點圖畫面已產生但尚未接上對話系統。請先執行 Tools → Dialogue → Generate Dialogue UI。");
            return false;
        }

        var trigger = dialogueCanvasGo.GetComponent<TriggerDialogue>();
        var typer = dialogueCanvasGo.GetComponent<DialogueTypingEffect>();
        if (trigger == null && typer == null)
        {
            Debug.LogWarning("[StoryNodeMap] DialogueCanvas 上找不到 TriggerDialogue / DialogueTypingEffect，無法接上對話系統。");
            return false;
        }

        var so = new SerializedObject(canvasGo.GetComponent<StoryNodeMapController>());
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

    /// <summary>把文字橫向撐滿父容器（左右各留 pad），並固定在指定的 y 位移與高度。</summary>
    private static void PlaceTop(TMP_Text tmp, float y, float height, float pad)
    {
        var rt = (RectTransform)tmp.transform;
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.offsetMin = new Vector2(pad, 0f);
        rt.offsetMax = new Vector2(-pad, 0f);
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
    /// 在 Canvas 最底層墊一張全螢幕純黑背景圖：沒有背景 / 畫面切換的空檔，
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
