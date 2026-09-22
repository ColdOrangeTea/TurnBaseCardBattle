// 此工具由 A_Good_Ink 使用 AI 生成。
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TMPro;
using TurnBaseBattleV2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 「地圖探索範例場景」生成器（由 A_Good_Ink 使用 AI 生成）。
///
/// 做什麼：沿用既有 prefab（不使用雜亂的 LevelMap.prefab）程式化組出一個可測的地圖探索場景，
/// 驗證深度重構後的 LevelMapManager / MapTurnBaseManager / S001_PlayerController：
///   - 正面視角相機（正交，看 +Z）＋ CameraController(followOffset)、EventSystem、Canvas
///   - 直接沿用 LevelMap_Stage 內「手排好的 Grid（Start/…/End）、Wire、Enemy_Boy、CameraPoint」
///   - 每個 Stage 是一顆星球小地圖；走到該 Stage 終點 Grid(End) 會切換到下一顆星球(Stage)
///   - 用既有 <see cref="BattleV2SceneGenerator.BuildBattleV2"/> 接一場常駐 V2 戰鬥（初始隱藏、不自動開戰）
///
/// 前置：Grid 的相鄰關係(connectedGrids)由 <see cref="GridWireLinker"/> 依 Wire 事先烘進 prefab；
///      本工具只讀取、不重排 Grid/Wire。
///
/// 使用方式：Unity 上方選單 Tools/TurnBaseBattle/生成 地圖探索範例場景 (LevelMap Sample)。
/// 可重複執行：覆蓋更新同路徑場景；GUID 不變、真正的 prefab 不被更動（事件類型等只改場景實例）。
/// </summary>
public static class LevelMapSampleGenerator
{
    const string LevelMapManagerGuid = "d7ca4c0624ca9c44f84a9ab885c15dc3";
    const string HeroGuid = "db08402c8b0bfd842b64cf2e8cf01415";
    const string StageGuid = "e47404ff569b93949857eccad10ebab8";
    const string EnemyGuid = "55621860537cf414590ae37f0725a300"; // 敵人已抽成獨立 prefab（不再內嵌於 LevelMap_Stage）
    const string BattleV2RootGuid = "461e61ce6af15cb45b3ce9d734d20f55"; // 自包含的 V2 戰鬥 prefab（含 Canvas+BattleEmpty+brain）
    const string PauseMenuGuid = "ef9449f2633ce354b8c95375bd9b34cf";    // 暫停選單 UI（UI_SetUpBackground，含 UI_PauseMenuController，ESC 叫出）
    const string ShopEmptyGuid = "392f9a331e0f848449e55dc7f50f752d";    // 商店 UI（ShopEmpty，接 ShopSystem）
    const string ShopItemGuid = "212d6131bc9cb9448937217e78e01137";     // 商店商品欄樣板（動態生成用）
    const string ScenePath = "Assets/LostStar/Scenes/LevelMapSample.unity";

    const float StageSpacingX = 40f;   // 兩顆星球(Stage)在世界座標的水平間距
    const float CameraOrthoSize = 10f; // 正交相機大小（框住整顆星球）

    // BGM 一律用 GUID 載入（使用者常搬動 Audio 資料夾；GUID 不隨路徑變、較穩）
    const string MapBgmGuid = "c72d1e49937b0564a9bda7f73da7c513";  // 地圖背景音樂 L1_BackgroundMusic_Fairy 7
    const string MoveSfxPath = "Assets/LostStar/Audio/SFX/SFX_PlayerMove.wav";           // 玩家移動音效
    const string TmpFontPath = "Assets/LostStar/Font/TaipeiSansTCBeta-Regular SDF.asset"; // 中文 TMP 字型

    // 預設音量（偏低；玩家可用暫停選單拉條再調）
    const float DefaultBgmVolume = 0.3f;
    const float DefaultSfxVolume = 0.5f;
    const string BuySfxPath = "Assets/LostStar/Audio/SFX/SFX_Buy.mp3";
    const string BuyFailSfxPath = "Assets/LostStar/Audio/SFX/SFX_BuyFailed.wav";
    const string StoreBgmGuid = "ba87664fac7db694799f302946719668";  // 商店 BGM（走 AudioDirector）
    const string ToStoreSfxPath = "Assets/LostStar/Audio/SFX/SFX_ToStore.mp3";        // 進店音效
    const string BattleBgmGuid = "515acfa99c4d3004aaec410f757669f9";  // 戰鬥 BGM（走 AudioDirector）
    const string VictoryBgmGuid = "607399303041343438e15fa16989edb0"; // 結算：勝利音樂 BGM_Battle_Succ
    const string LoseBgmGuid = "30507b7bdf3cd71489d4ecbdc36e9671";    // 結算：失敗音樂 BGM_Battle_Lose
    const string AudioDirectorGuid = "c7b739dcf9e33b1478cde8e7ac90ac97";              // 可重用的 AudioDirector prefab

    [MenuItem("Tools/TurnBaseBattle/生成 地圖探索範例場景 (LevelMap Sample)")]
    public static void Generate()
    {
        EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo();
        string report;
        bool ok = Build(out report);
        // 報告可能很長，改用固定大小、可捲動的視窗（DisplayDialog 會超出畫面）
        string header = ok
            ? $"已生成：\n{ScenePath}\n\n開啟後按 Play：點格子沿 Wire 走、遇敵/事件格開戰、走到 End 切下一顆星球。\n\n接線報告：\n"
            : "生成失敗，詳見 Console。\n\n";
        GeneratorReportWindow.Show("地圖探索範例場景", header + report);
    }

    public static bool Build(out string report)
    {
        var log = new StringBuilder();
        try
        {
            var gmPrefab = LoadByGuid(LevelMapManagerGuid, "LevelMapManager", log);
            var heroPrefab = LoadByGuid(HeroGuid, "HeroController_LevelMap", log);
            var stagePrefab = LoadByGuid(StageGuid, "LevelMap_Stage", log);
            var enemyPrefab = LoadByGuid(EnemyGuid, "Enemy", log);
            var battlePrefab = LoadByGuid(BattleV2RootGuid, "BattleV2Root", log);
            var pausePrefab = LoadByGuid(PauseMenuGuid, "PauseMenu(UI_SetUpBackground)", log); // 缺少不致命
            var shopPrefab = LoadByGuid(ShopEmptyGuid, "ShopEmpty", log);                     // 缺少不致命
            if (gmPrefab == null || heroPrefab == null || stagePrefab == null || enemyPrefab == null || battlePrefab == null)
            {
                report = log.ToString();
                return false;
            }

            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── 正面視角相機（看 +Z）＋ CameraController ──
            var camGO = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener), typeof(CameraController));
            camGO.tag = "MainCamera";
            var cam = camGO.GetComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraOrthoSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.05f, 0.06f, 0.09f);
            camGO.transform.rotation = Quaternion.identity;             // 看 +Z（正面）
            var camCtrl = camGO.GetComponent<CameraController>();
            // 尊重 CameraController.followOffset 的預設值（正面地圖 -Z），初始相機位置對齊之
            camGO.transform.position = camCtrl.followOffset;

            // ── EventSystem（UI 互動用；戰鬥 UI 的 Canvas 由 BattleV2Root prefab 自帶）──
            new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));

            // ── 音訊總管 AudioDirector（可重用 prefab：自帶 BGM/SFX 兩個 AudioSource；單一 BGM 頻道，商店/戰鬥 BGM 都走它、永不疊音）──
            var audioPrefab = LoadByGuid(AudioDirectorGuid, "AudioDirector", log);
            AudioSource bgm = null, directorSfx = null;
            if (audioPrefab != null)
            {
                var audioGO = (GameObject)PrefabUtility.InstantiatePrefab(audioPrefab);
                audioGO.name = "AudioDirector";
                var bgmT = FindDeep(audioGO.transform, "BGM");
                var sfxT = FindDeep(audioGO.transform, "SFX");
                bgm = bgmT != null ? bgmT.GetComponent<AudioSource>() : null;
                directorSfx = sfxT != null ? sfxT.GetComponent<AudioSource>() : null;
                var bgmClip = LoadClipByGuid(MapBgmGuid, "地圖 BGM", log);
                if (bgm != null) { bgm.clip = bgmClip; bgm.loop = true; bgm.playOnAwake = true; bgm.volume = DefaultBgmVolume; }
                if (directorSfx != null) directorSfx.volume = DefaultSfxVolume;
                log.AppendLine($"✓ AudioDirector prefab 已放入（BGM 頻道={bgm != null}, SFX={directorSfx != null}）");
            }
            else log.AppendLine("✗ 找不到 AudioDirector prefab，未建立音訊總管");

            // ── 暫停選單（探索地圖按 ESC 叫出；沿用既有 UI_SetUpBackground prefab，含 UI_PauseMenuController）──
            GameObject pauseGO = null;
            if (pausePrefab != null)
            {
                pauseGO = (GameObject)PrefabUtility.InstantiatePrefab(pausePrefab);
                pauseGO.name = "PauseMenu";
                var pauseCanvas = pauseGO.GetComponentInChildren<Canvas>(true);
                if (pauseCanvas != null) pauseCanvas.sortingOrder = 300; // 蓋在地圖與戰鬥(100)之上
                log.AppendLine("✓ 暫停選單已放入場景（ESC 叫出）");
            }
            else log.AppendLine("✗ 未放入暫停選單（prefab 找不到）");

            // ── 常駐 V2 戰鬥：實例化自包含的 BattleV2Root prefab（初始隱藏、關掉自動開戰）──
            var battleGO = (GameObject)PrefabUtility.InstantiatePrefab(battlePrefab);
            battleGO.name = "BattleV2Root";
            var battleUI = FindDeep(battleGO.transform, "BattleEmpty");
            if (battleUI != null) battleUI.gameObject.SetActive(false); // 地圖先顯示，開戰時 BattleView.OpenBattle 再顯示
            else log.AppendLine("✗ BattleV2Root prefab 內找不到 BattleEmpty");
            var bootstrap = battleGO.GetComponentInChildren<BattleV2Bootstrap>(true);
            if (bootstrap != null) BattleV2SceneGenerator.SetBool(bootstrap, "autoStart", false, log);
            else log.AppendLine("✗ BattleV2Root prefab 內找不到 BattleV2Bootstrap");

            // ── 地圖核心物件 ──
            var gmGO = (GameObject)PrefabUtility.InstantiatePrefab(gmPrefab);
            gmGO.name = "LevelMapManager";
            var gm = gmGO.GetComponent<LevelMapManager>();

            var heroGO = (GameObject)PrefabUtility.InstantiatePrefab(heroPrefab);
            heroGO.name = "HeroController_LevelMap";
            // 這顆 prefab 根預設繞 X 轉 90°（給俯視相機用）；本地圖是正面視角(XY 平面)，
            // 需歸零旋轉讓玩家 Spine 與格子同平面、正對相機，否則會側面朝相機而看不見。
            heroGO.transform.rotation = Quaternion.identity;
            var s001 = heroGO.GetComponent<S001_PlayerController>();
            var mapTurn = heroGO.GetComponent<MapTurnBaseManager>();

            // ── 玩家移動音效（移動中循環、停下即止；由 S001 的 DetectPositionChangeAndPlaySFX 控制）──
            var moveClip = AssetDatabase.LoadAssetAtPath<AudioClip>(MoveSfxPath);
            var moveSfx = heroGO.AddComponent<AudioSource>();
            moveSfx.clip = moveClip; moveSfx.loop = true; moveSfx.playOnAwake = false; moveSfx.volume = DefaultSfxVolume;
            s001.playerMoveSFX = moveSfx;
            log.AppendLine(moveClip != null ? "✓ 玩家移動音效已設定" : $"✗ 找不到玩家移動音效：{MoveSfxPath}");

            // ── 暫停選單的音量拉條：接上真正會改音量的 SimpleVolumeControl（BGM→地圖 BGM、SFX→移動音效＋商店等一次性音效）──
            WireVolumeSliders(pauseGO, bgm, new AudioSource[] { moveSfx, directorSfx }, log);

            // MapEventService 已烘進 LevelMapManager prefab，從實例取得（不再另建 GameObject）
            var mes = gmGO.GetComponent<MapEventService>();

            // ── 寶箱事件：建立寶箱 UI ＋ TreasureChest，訂閱 MapEventService.TreasureRequested ──
            var chestFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(TmpFontPath);
            BuildTreasureUI(chestFont, mes, s001, log);

            // ── 商店事件：實例化 ShopEmpty UI ＋ ShopSystem，訂閱 MapEventService.ShopRequested ──
            BuildShopUI(shopPrefab, mes, s001, log);

            // ── Stage 生成已移除（改為 StageInfo 手動擺放流程）──
            // 現在的做法：開發者把 LevelMap_Stage prefab 拖進場景、其上的 StageInfo 自帶資料、
            // 在 Inspector 連好各 Stage 的 exits，再於 LevelMapManager 指定 startStage。
            // 此工具不再自動建 Stage，也不再填 LevelMapManager 的 Stage 資料。
            gm.player = heroGO.transform;
            gm.cameraController = camCtrl;
            EditorUtility.SetDirty(gm);
            log.AppendLine("✓ LevelMapManager：player、cameraController 已接（Stage 請手動擺放 StageInfo 並指定 startStage）");

            // ── 接線：Hero 上兩個腳本 ＋ MapEventService ──
            s001.gridManager = gm;
            mapTurn.gridManager = gm;
            EditorUtility.SetDirty(s001);
            EditorUtility.SetDirty(mapTurn);
            BattleV2SceneGenerator.SetRef(s001, "mapEventService", mes, log);
            BattleV2SceneGenerator.SetRef(mapTurn, "playerController", s001, log);
            BattleV2SceneGenerator.SetRef(mes, "playerController", s001, log);

            // ── 地圖流程總控＋音訊掛件 ──
            // MapFlowController、ShopAudioHook、BattleAudioHook（含各 BGM/SFX clip）已烘進 LevelMapManager.prefab，
            // 隨 gmGO 一起帶進場景，不再另建 GameObject。MapFlowController 執行時自動尋找 player/eventService/shop/treasure
            // 並收集場上的 MapFlowHookBase，故不需在此接線。（SampleTreasureGlintHook 是示範、未烘進 prefab。）
            var flowOnGm = gmGO.GetComponent<MapFlowController>();
            log.AppendLine(flowOnGm != null
                ? "✓ 地圖流程總控＋音訊掛件：已隨 LevelMapManager prefab 帶入（MapFlowController＋Shop/BattleAudioHook）"
                : "✗ LevelMapManager prefab 上找不到 MapFlowController（請確認已烘入）");

            // （Stage 生成已移除；玩家落點由 LevelMapManager.Start 依 startStage 擺放）

            EditorSceneManager.MarkSceneDirty(scene);
            BattleV2SceneGenerator.EnsureFolder(System.IO.Path.GetDirectoryName(ScenePath).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            report = log.ToString();
            Debug.Log($"[LevelMapSampleGenerator] 生成完成：{ScenePath}\n{report}");
            return true;
        }
        catch (System.Exception ex)
        {
            report = log.ToString() + "\n例外：" + ex.Message;
            Debug.LogError($"[LevelMapSampleGenerator] 生成失敗：{ex}");
            return false;
        }
    }

    #region 輔助
    static void SetEvent(Transform grid, GridEventType type, StringBuilder log, int stageIndex)
    {
        var ev = grid.GetComponent<NodeEvent>();
        if (ev != null)
        {
            ev.eventType = type;
            if (type == GridEventType.BossCombat) ev.enemyType = EnemyType.Yarn;
            ev.ApplyIcon();               // 依類型即時套上對應 icon（None 則隱藏）
            EditorUtility.SetDirty(ev);
            // None 的格子太多，不逐格洗 log
            if (type != GridEventType.None)
                log.AppendLine($"  Stage{stageIndex}：{grid.name} → {type}");
        }
    }

    // 建立最小可用的寶箱 UI（Canvas→Panel→獎勵文字/道具圖/確定鈕）並掛上 TreasureChest、接好引用
    static void BuildTreasureUI(TMP_FontAsset font, MapEventService mes, S001_PlayerController player, StringBuilder log)
    {
        var canvasGO = new GameObject("TreasureCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 250; // 蓋在地圖與戰鬥(100)之上、暫停選單(300)之下
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var panel = NewUI("TreasurePanel", canvasGO.transform);
        var panelImg = panel.gameObject.AddComponent<Image>();
        panelImg.color = new Color(0.05f, 0.06f, 0.09f, 0.92f);
        Center(panel, Vector2.zero, new Vector2(700, 460));

        var itemImgRt = NewUI("ItemImage", panel);
        var itemImage = itemImgRt.gameObject.AddComponent<Image>();
        Center(itemImgRt, new Vector2(0, 120), new Vector2(150, 150));

        var textRt = NewUI("RewardText", panel);
        var rewardText = textRt.gameObject.AddComponent<TextMeshProUGUI>();
        rewardText.text = "獲得獎勵";
        rewardText.alignment = TextAlignmentOptions.Center;
        rewardText.fontSize = 46;
        if (font != null) rewardText.font = font;
        Center(textRt, new Vector2(0, -20), new Vector2(640, 140));

        var btnRt = NewUI("OKButton", panel);
        var btnImg = btnRt.gameObject.AddComponent<Image>();
        btnImg.color = new Color(0.25f, 0.5f, 0.85f, 1f);
        var okButton = btnRt.gameObject.AddComponent<Button>();
        Center(btnRt, new Vector2(0, -170), new Vector2(220, 72));
        var btnLabelRt = NewUI("Text", btnRt);
        var btnLabel = btnLabelRt.gameObject.AddComponent<TextMeshProUGUI>();
        btnLabel.text = "確定";
        btnLabel.alignment = TextAlignmentOptions.Center;
        btnLabel.fontSize = 34;
        if (font != null) btnLabel.font = font;
        Stretch(btnLabelRt);

        var chest = canvasGO.AddComponent<TreasureChest>();
        BattleV2SceneGenerator.SetRef(chest, "treasureUI", panel.gameObject, log);
        BattleV2SceneGenerator.SetRef(chest, "rewardText", rewardText, log);
        BattleV2SceneGenerator.SetRef(chest, "itemImage", itemImage, log);
        BattleV2SceneGenerator.SetRef(chest, "okButton", okButton, log);
        BattleV2SceneGenerator.SetRef(chest, "mapEventService", mes, log);
        BattleV2SceneGenerator.SetRef(chest, "playerController", player, log);
        panel.gameObject.SetActive(false); // 初始隱藏
        log.AppendLine(font != null ? "✓ 寶箱 UI + TreasureChest 已建立（含中文字型）" : "✗ 寶箱 UI 已建立但找不到中文字型（文字可能顯示 □）");
    }

    // 實例化 ShopEmpty UI 到自帶 Canvas 下，掛上 ShopSystem 並依名稱接好各部件
    static void BuildShopUI(GameObject shopPrefab, MapEventService mes, S001_PlayerController player, StringBuilder log)
    {
        if (shopPrefab == null) { log.AppendLine("✗ 未建立商店（ShopEmpty prefab 找不到）"); return; }

        // ShopEmpty 根沒有 Canvas，需掛在一個 Canvas 底下才會顯示
        var canvasGO = new GameObject("ShopCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 260;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        var shop = (GameObject)PrefabUtility.InstantiatePrefab(shopPrefab);
        shop.name = "ShopEmpty";
        var shopRt = shop.GetComponent<RectTransform>();
        shop.transform.SetParent(canvasGO.transform, false);
        if (shopRt != null) Stretch(shopRt);

        // ShopSystem 放在獨立管理物件（常駐、不隨 shopUI 開關而停用）；不再掛任何 AudioSource，聲音全走 AudioDirector
        var mgr = new GameObject("ShopManager", typeof(ShopSystem));
        var sys = mgr.GetComponent<ShopSystem>();

        var exit = FindDeep(shop.transform, "Exit");
        var coin = FindDeep(shop.transform, "Coin");
        var dialoguePanel = FindDeep(shop.transform, "Dialogue_Panel");
        var itemInfoOuter = FindDeep(shop.transform, "ItemInfo");
        Transform tooltip = itemInfoOuter != null ? itemInfoOuter.Find("ItemInfo") : null;

        BattleV2SceneGenerator.SetRef(sys, "mapEventService", mes, log);
        BattleV2SceneGenerator.SetRef(sys, "playerController", player, log);
        BattleV2SceneGenerator.SetRef(sys, "shopUI", shop, log);
        BattleV2SceneGenerator.SetRef(sys, "closeButton", exit != null ? exit.GetComponent<Button>() : null, log);
        BattleV2SceneGenerator.SetRef(sys, "goldText", coin != null ? coin.GetComponent<TMP_Text>() : null, log);
        BattleV2SceneGenerator.SetRef(sys, "messageText", dialoguePanel != null ? dialoguePanel.GetComponentInChildren<TMP_Text>(true) : null, log);
        if (tooltip != null)
        {
            BattleV2SceneGenerator.SetRef(sys, "tooltipUI", tooltip.gameObject, log);
            var tn = tooltip.Find("Name"); var td = tooltip.Find("Description");
            BattleV2SceneGenerator.SetRef(sys, "tooltipNameText", tn != null ? tn.GetComponent<TMP_Text>() : null, log);
            BattleV2SceneGenerator.SetRef(sys, "tooltipDescriptionText", td != null ? td.GetComponent<TMP_Text>() : null, log);
        }
        // 動態商品欄：容器 Item_Layout（GridLayoutGroup）＋ 商品欄樣板 Shop_Item（執行時清掉預覽再生成）
        var itemLayout = FindDeep(shop.transform, "Item_Layout");
        var shopItemPrefab = LoadByGuid(ShopItemGuid, "Shop_Item", log);
        BattleV2SceneGenerator.SetRef(sys, "itemSlotPrefab", shopItemPrefab, log);
        BattleV2SceneGenerator.SetRef(sys, "itemSlotContainer", itemLayout, log);
        BattleV2SceneGenerator.SetInt(sys, "itemCount", 3, log);

        shop.SetActive(false); // 初始隱藏（開店時再顯示）
        log.AppendLine($"✓ 商店 UI + ShopSystem 已建立（容器 Item_Layout={itemLayout != null}、樣板 Shop_Item={shopItemPrefab != null}、Exit={exit != null}、Coin={coin != null}）");
    }

    // 把暫停選單的「背景音樂 / 音效」拉條接到 SimpleVolumeControl（原本綁的舊設定腳本已擱置、失效）
    static void WireVolumeSliders(GameObject pauseGO, AudioSource bgm, AudioSource[] sfxSources, StringBuilder log)
    {
        if (pauseGO == null) return;
        var vc = pauseGO.AddComponent<SimpleVolumeControl>();
        BattleV2SceneGenerator.SetRef(vc, "bgmSource", bgm, log);
        SetObjectList(vc, "sfxSources", sfxSources, log);

        float sfxVol = (sfxSources != null && sfxSources.Length > 0 && sfxSources[0] != null) ? sfxSources[0].volume : 1f;
        WireOneVolumeSlider(pauseGO, "Background_Music_Slider", vc.SetBGMVolume, bgm != null ? bgm.volume : 1f, log);
        WireOneVolumeSlider(pauseGO, "Sound_Effects_Slider", vc.SetSFXVolume, sfxVol, log);
    }

    static void WireOneVolumeSlider(GameObject root, string sliderName, UnityEngine.Events.UnityAction<float> method, float initValue, StringBuilder log)
    {
        var t = FindDeep(root.transform, sliderName);
        var slider = t != null ? t.GetComponent<Slider>() : null;
        if (slider == null) { log.AppendLine($"✗ 找不到拉條：{sliderName}"); return; }

        slider.minValue = 0f; slider.maxValue = 1f;   // 統一 0~1（原本 -80~1 是舊 mixer dB 範圍）
        for (int i = slider.onValueChanged.GetPersistentEventCount() - 1; i >= 0; i--)
            UnityEditor.Events.UnityEventTools.RemovePersistentListener(slider.onValueChanged, i); // 清掉指向已擱置腳本的舊綁定
        UnityEditor.Events.UnityEventTools.AddPersistentListener(slider.onValueChanged, method);
        slider.value = Mathf.Clamp01(initValue);       // 觸發一次，讓初值同步到音量
        log.AppendLine($"✓ 音量拉條已接：{sliderName} → SimpleVolumeControl");
    }

    static void SetObjectList(Object target, string field, Object[] values, StringBuilder log)
    {
        var so = new SerializedObject(target);
        var p = so.FindProperty(field);
        if (p == null || !p.isArray) { log.AppendLine($"✗ {target.GetType().Name}.{field} 不是可序列化清單"); return; }
        p.arraySize = values.Length;
        for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        log.AppendLine($"✓ {target.GetType().Name}.{field}：{values.Length} 項");
    }

    static RectTransform NewUI(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return go.GetComponent<RectTransform>();
    }

    static void Center(RectTransform rt, Vector2 anchoredPos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
        rt.sizeDelta = size;
        rt.anchoredPosition = anchoredPos;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
    }

    static GameObject LoadByGuid(string guid, string label, StringBuilder log)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var go = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (go == null) log.AppendLine($"✗ 找不到 prefab：{label}（GUID {guid}）");
        else log.AppendLine($"✓ 載入 prefab：{label} @ {path}");
        return go;
    }

    // 依 GUID 載入 AudioClip（不受資料夾搬動影響）
    static AudioClip LoadClipByGuid(string guid, string label, StringBuilder log)
    {
        string path = AssetDatabase.GUIDToAssetPath(guid);
        var clip = string.IsNullOrEmpty(path) ? null : AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        if (clip == null) log.AppendLine($"✗ 找不到音訊：{label}（GUID {guid}）");
        else log.AppendLine($"✓ 載入音訊：{label} @ {path}");
        return clip;
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name) return root;
        foreach (Transform c in root)
        {
            var r = FindDeep(c, name);
            if (r != null) return r;
        }
        return null;
    }
    #endregion
}
