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
using Assets.Scripts.Dialogue;

/// <summary>
/// 一鍵產生對話系統的 UI Prefabs、底部按鈕開關動畫與範例場景。
/// 使用方式：Unity 選單 → Tools → Dialogue → Generate Dialogue UI (Prefabs + Sample Scene)
/// 產出：
///   Assets/Prefabs/Dialogue/   DialogueBackground / CharacterPortrait / DialogueBoxPanel / BottomButton / DialogueCanvas
///   Assets/Animations/Dialogue/ Bottom_Btn_On_Start / Loop / End（Legacy 動畫）
///   Assets/Scenes/DialogueSample.unity
/// 可重複執行：會覆蓋更新既有產出（GUID 不變，引用不會斷）。
/// </summary>
public static class DialogueUISetupTool
{
    private const string PrefabDir = "Assets/Prefabs/Dialogue";
    private const string AnimDir = "Assets/Animations/Dialogue";
    private const string SceneDir = "Assets/Scenes";
    private const string ScenePath = SceneDir + "/DialogueSample.unity";

    // 中文字型（避免中文顯示為 □□□）
    private const string FontSourcePath = "Assets/Font/TaipeiSansTCBeta-Regular.ttf";
    private const string FontAssetPath = "Assets/Font/TaipeiSansTCBeta-Regular SDF.asset";
    private static TMP_FontAsset s_dialogueFont;

    // 對話資訊表
    private const string DataDir = "Assets/Data/Dialogue";
    private const string SampleDataPath = DataDir + "/SampleDialogue.asset";

    // 按鈕關閉 / 開啟狀態的顏色
    private static readonly Color BtnOffColor = new Color(0.13f, 0.13f, 0.16f, 0.90f);
    private static readonly Color BtnOnColor = new Color(0.85f, 0.65f, 0.20f, 0.95f);
    private static readonly Color PanelColor = new Color(0.05f, 0.05f, 0.08f, 0.85f);
    private static readonly Color NameColor = new Color(0.95f, 0.80f, 0.40f, 1.00f);

    [MenuItem("Tools/Dialogue/Generate Dialogue UI (Prefabs + Sample Scene)")]
    public static void Generate()
    {
        if (!EnsureTMPEssentials()) return;
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        EnsureFolder(PrefabDir);
        EnsureFolder(AnimDir);
        EnsureFolder(SceneDir);
        EnsureFolder(DataDir);

        s_dialogueFont = CreateOrLoadChineseFontAsset();
        DialogueData sampleData = CreateSampleDialogueData();
        AnimationClip[] clips = CreateButtonAnimations();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

        GameObject canvasGo = BuildDialogueCanvas(clips, sampleData);

        // EventSystem：使用專案自訂的 InteractionOfUI（繼承 EventSystem）
        new GameObject("EventSystem", typeof(InteractionOfUI), typeof(StandaloneInputModule));

        // 整個 Canvas 存成 Prefab（內含巢狀子 Prefab）
        PrefabUtility.SaveAsPrefabAssetAndConnect(
            canvasGo, PrefabDir + "/DialogueCanvas.prefab", InteractionMode.AutomatedAction);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog("Dialogue UI",
            "產生完成！\n\n" +
            "Prefabs：" + PrefabDir + "\n" +
            "動畫：" + AnimDir + "\n" +
            "對話資訊表：" + SampleDataPath + "\n" +
            "中文字型：" + FontAssetPath + "\n" +
            "範例場景：" + ScenePath + "\n\n" +
            "按 Play 後點擊右上角「開始對話」按鈕測試。", "OK");
    }

    #region TMP / 資料夾

    private static bool EnsureTMPEssentials()
    {
        TMP_FontAsset font = null;
        try { font = TMP_Settings.defaultFontAsset; } catch { /* TMP Settings 尚未匯入 */ }
        if (font != null) return true;

        string[] candidates =
        {
            "Packages/com.unity.ugui/Package Resources/TMP Essential Resources.unitypackage",
            "Packages/com.unity.textmeshpro/Package Resources/TMP Essential Resources.unitypackage",
        };
        foreach (var p in candidates)
        {
            string physical = FileUtil.GetPhysicalPath(p);
            if (File.Exists(physical))
            {
                AssetDatabase.ImportPackage(physical, false);
                EditorUtility.DisplayDialog("TMP Essential Resources",
                    "已自動匯入 TMP Essential Resources。\n匯入完成後請再執行一次：\nTools → Dialogue → Generate Dialogue UI", "OK");
                return false;
            }
        }
        EditorUtility.DisplayDialog("TMP Essential Resources",
            "找不到 TMP Essential Resources，請先由\nWindow → TextMeshPro → Import TMP Essential Resources 匯入後再執行。", "OK");
        return false;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    #endregion

    #region 中文字型與對話資訊表

    /// <summary>
    /// 由 TaipeiSansTCBeta-Regular.ttf 產生 TMP 字型資產（Dynamic 動態填充圖集），
    /// 讓中文對白與系統文字（跳過 / 自動 / 紀錄等）不會顯示為 □□□。
    /// </summary>
    private static TMP_FontAsset CreateOrLoadChineseFontAsset()
    {
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontAssetPath);
        if (existing != null) return existing;

        var sourceFont = AssetDatabase.LoadAssetAtPath<Font>(FontSourcePath);
        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("中文字型",
                "找不到 " + FontSourcePath + "\nTMP 將使用預設字型（中文會顯示為方框）。", "OK");
            return null;
        }

        // Dynamic 模式：字元用到時才動態填入圖集，不需預先烘焙數千個中文字
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
    /// 建立範例對話資訊表。已存在且有內容時不覆蓋（保留使用者的編輯）。
    /// </summary>
    private static DialogueData CreateSampleDialogueData()
    {
        var data = AssetDatabase.LoadAssetAtPath<DialogueData>(SampleDataPath);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(data, SampleDataPath);
        }

        if (data.lines == null || data.lines.Count == 0) // 不覆蓋使用者已編輯的內容
        {
            data.dialogueType = DialogueType.Other;
            data.lines = new List<DialogueData.DialogueLine>
            {
                new DialogueData.DialogueLine
                {
                    text = "你好，旅人。|pause|歡迎來到伊菲爾。",
                    useCharacterStyle = false, // 範例資料使用自定義模式（未依賴人物風格資訊表）
                    speaker = DialogueUnitType.Narration,
                    displayName = "瑟拉菲斯",
                },
                new DialogueData.DialogueLine
                {
                    text = "接下來的路……|pause=1.5|會有些顛簸，準備好了嗎？",
                    useCharacterStyle = false,
                    speaker = DialogueUnitType.Narration,
                    displayName = "瑟拉菲斯",
                },
                new DialogueData.DialogueLine
                {
                    text = "（<color=#F2D388>旅人</color>點了點頭。）",
                    useCharacterStyle = false,
                    speaker = DialogueUnitType.Narration,
                    displayName = "旁白",
                },
                new DialogueData.DialogueLine
                {
                    text = "很好！|pause=0.8|那我們出發吧！",
                    useCharacterStyle = false,
                    speaker = DialogueUnitType.Narration,
                    displayName = "瑟拉菲斯",
                    pauseDuration = 0.6f, // 此行 |pause| 的預設停頓改為 0.6 秒
                },
            };
            EditorUtility.SetDirty(data);
        }
        return data;
    }

    #endregion

    #region 按鈕開關動畫（Legacy AnimationClip）

    private static AnimationClip[] CreateButtonAnimations()
    {
        // 開啟：放大 + 變成金色
        var start = LoadOrCreateClip(AnimDir + "/Bottom_Btn_On_Start.anim");
        start.wrapMode = WrapMode.Once;
        SetScaleCurve(start, new[] { 0f, 0.15f }, new[] { 1f, 1.15f });
        SetColorCurve(start, new[] { 0f, 0.15f }, new[] { BtnOffColor, BtnOnColor });

        // 開啟中：微幅呼吸縮放循環
        var loop = LoadOrCreateClip(AnimDir + "/Bottom_Btn_On_Loop.anim");
        loop.wrapMode = WrapMode.Loop;
        SetScaleCurve(loop, new[] { 0f, 0.6f, 1.2f }, new[] { 1.15f, 1.08f, 1.15f });
        SetColorCurve(loop, new[] { 0f, 1.2f }, new[] { BtnOnColor, BtnOnColor });

        // 關閉：縮回 + 變回深色
        var end = LoadOrCreateClip(AnimDir + "/Bottom_Btn_On_End.anim");
        end.wrapMode = WrapMode.Once;
        SetScaleCurve(end, new[] { 0f, 0.15f }, new[] { 1.15f, 1f });
        SetColorCurve(end, new[] { 0f, 0.15f }, new[] { BtnOnColor, BtnOffColor });

        var clips = new[] { start, loop, end };
        foreach (var c in clips) EditorUtility.SetDirty(c);
        return clips;
    }

    private static AnimationClip LoadOrCreateClip(string path)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null)
        {
            clip = new AnimationClip { name = Path.GetFileNameWithoutExtension(path) };
            AssetDatabase.CreateAsset(clip, path);
        }
        clip.ClearCurves();
        clip.legacy = true; // Animation（Legacy）組件需要 legacy clip 才能用 anim.Play(name)
        return clip;
    }

    private static void SetScaleCurve(AnimationClip clip, float[] times, float[] values)
    {
        var cx = new AnimationCurve();
        var cy = new AnimationCurve();
        var cz = new AnimationCurve();
        for (int i = 0; i < times.Length; i++)
        {
            cx.AddKey(times[i], values[i]);
            cy.AddKey(times[i], values[i]);
            cz.AddKey(times[i], 1f);
        }
        clip.SetCurve("", typeof(Transform), "localScale.x", cx);
        clip.SetCurve("", typeof(Transform), "localScale.y", cy);
        clip.SetCurve("", typeof(Transform), "localScale.z", cz);
    }

    private static void SetColorCurve(AnimationClip clip, float[] times, Color[] colors)
    {
        var cr = new AnimationCurve();
        var cg = new AnimationCurve();
        var cb = new AnimationCurve();
        var ca = new AnimationCurve();
        for (int i = 0; i < times.Length; i++)
        {
            cr.AddKey(times[i], colors[i].r);
            cg.AddKey(times[i], colors[i].g);
            cb.AddKey(times[i], colors[i].b);
            ca.AddKey(times[i], colors[i].a);
        }
        clip.SetCurve("", typeof(Image), "m_Color.r", cr);
        clip.SetCurve("", typeof(Image), "m_Color.g", cg);
        clip.SetCurve("", typeof(Image), "m_Color.b", cb);
        clip.SetCurve("", typeof(Image), "m_Color.a", ca);
    }

    #endregion

    #region Prefab 生成

    /// <summary>
    /// 對話背景 Prefab：放置 / 替換背景圖（由 DialogueData.backgroundImage 提供）。
    /// 結構：根物件（滿版 + RectMask2D 裁切）→ 子 Image（AspectRatioFitter EnvelopeParent）。
    /// 等比放大填滿畫面：Resize 拉伸不露餡，也不會非等比拉伸造成圖像變形模糊。
    /// </summary>
    private static GameObject CreateBackgroundPrefab()
    {
        // 根物件：滿版容器 + 裁切超出畫面的部分
        var go = new GameObject("DialogueBackground",
            typeof(RectTransform), typeof(RectMask2D), typeof(DialogueBackgroundController));
        StretchFull((RectTransform)go.transform);

        // 子物件：實際顯示背景的 Image，由 AspectRatioFitter 控制等比填滿
        var imgGo = new GameObject("BackgroundImage",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(AspectRatioFitter));
        imgGo.transform.SetParent(go.transform, false);

        var img = imgGo.GetComponent<Image>();
        img.raycastTarget = false; // 不擋任何點擊
        img.preserveAspect = false; // 長寬比交由 AspectRatioFitter 控制

        var arf = imgGo.GetComponent<AspectRatioFitter>();
        arf.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; // 等比放大到包住父區域（Cover）
        arf.aspectRatio = 16f / 9f; // 預設比例；設定 Sprite 後由腳本依原圖尺寸更新

        var so = new SerializedObject(go.GetComponent<DialogueBackgroundController>());
        so.FindProperty("backgroundImage").objectReferenceValue = img;
        so.FindProperty("aspectFitter").objectReferenceValue = arf;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SaveAsPrefab(go, PrefabDir + "/DialogueBackground.prefab");
    }

    /// <summary>人物立繪 Prefab（ADV 立繪置換用）。</summary>
    private static GameObject CreatePortraitPrefab()
    {
        var go = new GameObject("CharacterPortrait",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CharacterPortraitController));
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(-800f, 360f);
        rt.sizeDelta = new Vector2(200f, 200f);

        var img = go.GetComponent<Image>();
        img.color = new Color(1f, 1f, 1f, 0.12f); // 佔位半透明；設定 Sprite 後由腳本改回白色
        img.raycastTarget = false;
        img.preserveAspect = true;

        // 佔位提示文字（登錄 Sprite 後自動隱藏）
        var label = CreateTMP(go.transform, "PlaceholderLabel",
            "人物立繪\n（在 CharacterPortraitController\n登錄 Sprite 後自動置換）", 24,
            TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.5f));
        StretchFull((RectTransform)label.transform);

        var so = new SerializedObject(go.GetComponent<CharacterPortraitController>());
        so.FindProperty("portraitImage").objectReferenceValue = img;
        so.FindProperty("placeholder").objectReferenceValue = label.gameObject;
        so.ApplyModifiedPropertiesWithoutUndo();

        return SaveAsPrefab(go, PrefabDir + "/CharacterPortrait.prefab");
    }

    /// <summary>對話框 Prefab：面板 + 角色名稱框 + 對話內容框。</summary>
    private static GameObject CreateDialogueBoxPrefab()
    {
        var go = new GameObject("DialogueBoxPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(-80f, 260f);
        rt.anchoredPosition = new Vector2(0f, 86f);

        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = PanelColor;
        img.raycastTarget = true; // TriggerDialogue.TextChange 需 Raycast 到此面板來推進對話

        // 角色名稱框
        var nameTmp = CreateTMP(go.transform, "NameBox", "名稱", 32,
            TextAlignmentOptions.Left, NameColor);
        nameTmp.fontStyle = FontStyles.Bold;
        var nameRt = (RectTransform)nameTmp.transform;
        nameRt.anchorMin = nameRt.anchorMax = new Vector2(0f, 1f);
        nameRt.pivot = new Vector2(0f, 1f);
        nameRt.anchoredPosition = new Vector2(40f, -16f);
        nameRt.sizeDelta = new Vector2(500f, 44f);

        // 對話內容框
        var contentTmp = CreateTMP(go.transform, "ContentBox",
            "點擊「開始對話」後在此顯示對話內容。", 30,
            TextAlignmentOptions.TopLeft, Color.white);
        var contentRt = (RectTransform)contentTmp.transform;
        contentRt.anchorMin = new Vector2(0f, 0f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.offsetMin = new Vector2(40f, 24f);
        contentRt.offsetMax = new Vector2(-40f, -72f);

        return SaveAsPrefab(go, PrefabDir + "/DialogueBoxPanel.prefab");
    }

    /// <summary>底部功能按鈕 Prefab（跳過 / 自動 / 紀錄 通用，附開關動畫）。</summary>
    private static GameObject CreateBottomButtonPrefab(AnimationClip[] clips)
    {
        var go = new GameObject("BottomButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(Animation), typeof(BottomButtonController));
        var rt = (RectTransform)go.transform;
        rt.sizeDelta = new Vector2(160f, 56f);

        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = BtnOffColor;

        // 顏色交由開關動畫控制，關閉 Button 自身的 ColorTint 避免互相覆蓋
        var btn = go.GetComponent<Button>();
        btn.targetGraphic = img;
        btn.transition = Selectable.Transition.None;

        var anim = go.GetComponent<Animation>();
        anim.playAutomatically = false;
        AnimationUtility.SetAnimationClips(anim, clips);

        var label = CreateTMP(go.transform, "Label", "按鈕", 26,
            TextAlignmentOptions.Center, Color.white);
        StretchFull((RectTransform)label.transform);

        return SaveAsPrefab(go, PrefabDir + "/BottomButton.prefab");
    }

    private static GameObject SaveAsPrefab(GameObject temp, string path)
    {
        var asset = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return asset;
    }

    #endregion

    #region Canvas 組裝

    private static GameObject BuildDialogueCanvas(AnimationClip[] clips, DialogueData sampleData)
    {
        GameObject backgroundPrefab = CreateBackgroundPrefab();
        GameObject portraitPrefab = CreatePortraitPrefab();
        GameObject dialogueBoxPrefab = CreateDialogueBoxPrefab();
        GameObject buttonPrefab = CreateBottomButtonPrefab(clips);
        GameObject logEntryPrefab = CreateLogEntryPrefab();

        var canvasGo = new GameObject("DialogueCanvas",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // ---- 對話 UI 群組（跳過時整組淡出的容器）----
        var groupGo = new GameObject("DialogueGroup", typeof(RectTransform), typeof(CanvasGroup));
        groupGo.transform.SetParent(canvasGo.transform, false);
        StretchFull((RectTransform)groupGo.transform);

        // ---- 巢狀 Prefab 實例（順序 = 渲染順序）----
        var backgroundGo = InstantiateUnder(backgroundPrefab, groupGo.transform); // 最底層：對話背景
        var portraitGo = InstantiateUnder(portraitPrefab, groupGo.transform);
        var dialogueBoxGo = InstantiateUnder(dialogueBoxPrefab, groupGo.transform);
        var logPanel = BuildLogPanel(groupGo.transform, logEntryPrefab);
        var bar = BuildBottomBar(groupGo.transform);
        var ffBtn = SetupButtonInstance(buttonPrefab, bar.transform, "FastForwardButton", "快轉");
        var autoBtn = SetupButtonInstance(buttonPrefab, bar.transform, "AutoButton", "自動");
        var logBtn = SetupButtonInstance(buttonPrefab, bar.transform, "LogButton", "紀錄");
        var skipBtn = BuildSkipButton(bar.transform); // 跳過：單發按鈕（淡出結束），非開關型
        var openBtn = BuildOpenButton(canvasGo.transform);

        groupGo.SetActive(false); // 預設隱藏，由「開始對話」開啟

        // ---- 邏輯組件 + 引用綁定 ----
        var typer = canvasGo.AddComponent<DialogueTypingEffect>();
        typer.charaNameBox = dialogueBoxGo.transform.Find("NameBox").GetComponent<TMP_Text>();
        typer.dialogueContentBox = dialogueBoxGo.transform.Find("ContentBox").GetComponent<TMP_Text>();
        var typerSO = new SerializedObject(typer);
        typerSO.FindProperty("portraitController").objectReferenceValue =
            portraitGo.GetComponent<CharacterPortraitController>();
        typerSO.FindProperty("backgroundController").objectReferenceValue =
            backgroundGo.GetComponent<DialogueBackgroundController>();
        typerSO.FindProperty("logController").objectReferenceValue =
            logPanel.GetComponent<DialogueLogController>();
        typerSO.FindProperty("dialogueData").objectReferenceValue = sampleData;
        typerSO.ApplyModifiedPropertiesWithoutUndo();

        var trigger = canvasGo.AddComponent<TriggerDialogue>();
        var so = new SerializedObject(trigger);
        so.FindProperty("ContentTyper").objectReferenceValue = typer;
        so.FindProperty("DialogueGroup").objectReferenceValue = groupGo.GetComponent<CanvasGroup>();
        so.FindProperty("DialogueBoxPanel").objectReferenceValue = dialogueBoxGo;
        so.FindProperty("LogUI").objectReferenceValue = logPanel;
        so.FindProperty("FastForwardButton").objectReferenceValue = ffBtn;
        so.FindProperty("AutoButton").objectReferenceValue = autoBtn;
        so.FindProperty("LogButton").objectReferenceValue = logBtn;
        so.FindProperty("SkipButton").objectReferenceValue = skipBtn;
        so.FindProperty("OpenButton").objectReferenceValue = openBtn;
        so.ApplyModifiedPropertiesWithoutUndo();

        // Button OnClick → TriggerDialogue（持久化監聽，Inspector 可見）
        UnityEventTools.AddPersistentListener(ffBtn.GetComponent<Button>().onClick, trigger.Btn_FastForward);
        UnityEventTools.AddPersistentListener(autoBtn.GetComponent<Button>().onClick, trigger.Btn_Auto);
        UnityEventTools.AddPersistentListener(logBtn.GetComponent<Button>().onClick, trigger.Btn_Log);
        UnityEventTools.AddPersistentListener(skipBtn.GetComponent<Button>().onClick, trigger.Btn_Skip);
        UnityEventTools.AddPersistentListener(openBtn.GetComponent<Button>().onClick, trigger.Btn_Open);

        return canvasGo;
    }

    private static GameObject InstantiateUnder(GameObject prefab, Transform parent)
    {
        var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        go.transform.SetParent(parent, false);
        return go;
    }

    private static GameObject SetupButtonInstance(GameObject prefab, Transform parent, string name, string label)
    {
        var go = InstantiateUnder(prefab, parent);
        go.name = name;
        go.transform.Find("Label").GetComponent<TextMeshProUGUI>().text = label;
        return go;
    }

    /// <summary>單筆對話紀錄的 Prefab（名稱 + 對白，一個 TMP 文字）。</summary>
    private static GameObject CreateLogEntryPrefab()
    {
        var go = new GameObject("LogEntry", typeof(RectTransform));
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (s_dialogueFont != null) tmp.font = s_dialogueFont;
        tmp.fontSize = 26;
        tmp.alignment = TextAlignmentOptions.TopLeft;
        tmp.color = new Color(1f, 1f, 1f, 0.92f);
        tmp.raycastTarget = false;
        tmp.text = "<color=#F2D388>名稱</color>\n對白內容";
        return SaveAsPrefab(go, PrefabDir + "/LogEntry.prefab");
    }

    /// <summary>對話紀錄面板：由上到下記錄「名稱 + 對白」，可捲動觀看（預設隱藏）。</summary>
    private static GameObject BuildLogPanel(Transform parent, GameObject entryPrefab)
    {
        var go = new GameObject("LogPanel",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(DialogueLogController));
        go.transform.SetParent(parent, false);
        StretchFull((RectTransform)go.transform);

        var img = go.GetComponent<Image>();
        img.color = new Color(0f, 0f, 0f, 0.88f);
        img.raycastTarget = true;

        var title = CreateTMP(go.transform, "Title", "對話紀錄", 48,
            TextAlignmentOptions.Center, Color.white);
        var titleRt = (RectTransform)title.transform;
        titleRt.anchorMin = titleRt.anchorMax = new Vector2(0.5f, 1f);
        titleRt.pivot = new Vector2(0.5f, 1f);
        titleRt.anchoredPosition = new Vector2(0f, -40f);
        titleRt.sizeDelta = new Vector2(500f, 64f);

        var hint = CreateTMP(go.transform, "Hint",
            "（滾輪或拖曳捲動．點擊下方「紀錄」按鈕關閉）", 24,
            TextAlignmentOptions.Center, new Color(1f, 1f, 1f, 0.6f));
        var hintRt = (RectTransform)hint.transform;
        hintRt.anchorMin = hintRt.anchorMax = new Vector2(0.5f, 0f);
        hintRt.pivot = new Vector2(0.5f, 0f);
        hintRt.anchoredPosition = new Vector2(0f, 90f);
        hintRt.sizeDelta = new Vector2(800f, 40f);

        // ---- ScrollView（捲動區）----
        var svGo = new GameObject("ScrollView", typeof(RectTransform), typeof(ScrollRect));
        svGo.transform.SetParent(go.transform, false);
        var svRt = (RectTransform)svGo.transform;
        svRt.anchorMin = new Vector2(0f, 0f);
        svRt.anchorMax = new Vector2(1f, 1f);
        svRt.offsetMin = new Vector2(160f, 140f);  // 底部讓出提示文字與按鈕列
        svRt.offsetMax = new Vector2(-160f, -120f); // 頂部讓出標題

        // Viewport（遮罩）
        var viewportGo = new GameObject("Viewport",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(RectMask2D));
        viewportGo.transform.SetParent(svGo.transform, false);
        StretchFull((RectTransform)viewportGo.transform);
        var vpImg = viewportGo.GetComponent<Image>();
        vpImg.color = new Color(1f, 1f, 1f, 0.02f); // 幾乎透明，但可接收拖曳 Raycast

        // Content（紀錄條目容器：由上往下排列、高度自適應）
        var contentGo = new GameObject("Content",
            typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
        contentGo.transform.SetParent(viewportGo.transform, false);
        var contentRt = (RectTransform)contentGo.transform;
        contentRt.anchorMin = new Vector2(0f, 1f);
        contentRt.anchorMax = new Vector2(1f, 1f);
        contentRt.pivot = new Vector2(0.5f, 1f);
        contentRt.sizeDelta = Vector2.zero;

        var vlg = contentGo.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(8, 8, 8, 8);
        vlg.spacing = 28f;
        vlg.childAlignment = TextAnchor.UpperLeft;
        vlg.childControlWidth = true;
        vlg.childControlHeight = true;   // 高度交由 TMP 的 preferredHeight
        vlg.childForceExpandWidth = true;
        vlg.childForceExpandHeight = false;
        contentGo.GetComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        // Scrollbar（右側細捲軸）
        var sbGo = new GameObject("Scrollbar",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Scrollbar));
        sbGo.transform.SetParent(svGo.transform, false);
        var sbRt = (RectTransform)sbGo.transform;
        sbRt.anchorMin = new Vector2(1f, 0f);
        sbRt.anchorMax = new Vector2(1f, 1f);
        sbRt.pivot = new Vector2(1f, 0.5f);
        sbRt.sizeDelta = new Vector2(12f, 0f);
        sbRt.anchoredPosition = Vector2.zero;
        sbGo.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.06f);

        var slidingGo = new GameObject("SlidingArea", typeof(RectTransform));
        slidingGo.transform.SetParent(sbGo.transform, false);
        StretchFull((RectTransform)slidingGo.transform);

        var handleGo = new GameObject("Handle",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        handleGo.transform.SetParent(slidingGo.transform, false);
        StretchFull((RectTransform)handleGo.transform);
        var handleImg = handleGo.GetComponent<Image>();
        handleImg.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        handleImg.type = Image.Type.Sliced;
        handleImg.color = new Color(1f, 1f, 1f, 0.35f);

        var sb = sbGo.GetComponent<Scrollbar>();
        sb.direction = Scrollbar.Direction.BottomToTop;
        sb.handleRect = (RectTransform)handleGo.transform;
        sb.targetGraphic = handleImg;

        // ScrollRect 組裝
        var scroll = svGo.GetComponent<ScrollRect>();
        scroll.content = contentRt;
        scroll.viewport = (RectTransform)viewportGo.transform;
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        scroll.verticalScrollbar = sb;
        scroll.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport;

        // DialogueLogController 綁定
        var logSO = new SerializedObject(go.GetComponent<DialogueLogController>());
        logSO.FindProperty("scrollRect").objectReferenceValue = scroll;
        logSO.FindProperty("contentRoot").objectReferenceValue = contentRt;
        logSO.FindProperty("entryPrefab").objectReferenceValue = entryPrefab;
        logSO.ApplyModifiedPropertiesWithoutUndo();

        go.SetActive(false);
        return go;
    }

    /// <summary>螢幕底部的功能按鈕列容器。</summary>
    private static GameObject BuildBottomBar(Transform parent)
    {
        var go = new GameObject("BottomButtonBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = new Vector2(0f, 0f);
        rt.anchorMax = new Vector2(1f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.sizeDelta = new Vector2(0f, 70f);
        rt.anchoredPosition = new Vector2(0f, 8f);

        var hlg = go.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(24, 24, 7, 7);
        hlg.spacing = 16f;
        hlg.childAlignment = TextAnchor.MiddleRight;
        hlg.childControlWidth = false;
        hlg.childControlHeight = false;
        hlg.childForceExpandWidth = false;
        hlg.childForceExpandHeight = false;
        return go;
    }

    /// <summary>跳過按鈕：單發型（按下淡出並結束對話），不掛 BottomButtonController。</summary>
    private static GameObject BuildSkipButton(Transform parent)
    {
        var go = new GameObject("SkipButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        ((RectTransform)go.transform).sizeDelta = new Vector2(160f, 56f);

        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = new Color(0.38f, 0.14f, 0.14f, 0.92f); // 紅色調：提示為結束對話的操作
        go.GetComponent<Button>().targetGraphic = img;

        var label = CreateTMP(go.transform, "Label", "跳過", 26,
            TextAlignmentOptions.Center, Color.white);
        StretchFull((RectTransform)label.transform);
        return go;
    }

    /// <summary>右上角「開始對話」按鈕（範例場景用的入口）。</summary>
    private static GameObject BuildOpenButton(Transform parent)
    {
        var go = new GameObject("OpenButton",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = rt.anchorMax = new Vector2(1f, 1f);
        rt.pivot = new Vector2(1f, 1f);
        rt.anchoredPosition = new Vector2(-24f, -24f);
        rt.sizeDelta = new Vector2(220f, 60f);

        var img = go.GetComponent<Image>();
        img.sprite = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
        img.type = Image.Type.Sliced;
        img.color = BtnOffColor;
        go.GetComponent<Button>().targetGraphic = img;

        var label = CreateTMP(go.transform, "Label", "開始對話", 28,
            TextAlignmentOptions.Center, Color.white);
        StretchFull((RectTransform)label.transform);
        return go;
    }

    #endregion

    #region 共用小工具

    private static TextMeshProUGUI CreateTMP(Transform parent, string name, string text,
        float fontSize, TextAlignmentOptions alignment, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        var tmp = go.AddComponent<TextMeshProUGUI>();
        if (s_dialogueFont != null) tmp.font = s_dialogueFont; // 中文字型，避免顯示 □□□
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.alignment = alignment;
        tmp.color = color;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void StretchFull(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
    }

    #endregion
}
