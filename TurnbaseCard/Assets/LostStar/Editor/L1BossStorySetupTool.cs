// 此工具由 A_Good_Ink 使用 AI 生成。
using System.Collections.Generic;
using System.Text;
using Assets.Scripts.Dialogue;
using Assets.Scripts.GlobalEnums.BattleEnum;
using Spine.Unity;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 「L1 Boss／Joker 劇情」資料與場景接線工具（由 A_Good_Ink 使用 AI 生成）。
///
/// 做什麼：
///   1. 建立奧蘭娜的人物風格表 <c>Resources/Dialogue/Style_Orlana.asset</c>（Spine 立繪＝Story_Goddness）；已存在則不動（尊重手改）。
///   1b. DialogueEmpty 立繪接線：在立繪容器掛 CharacterPortraitController（單一 SkeletonGraphic＝每句換骨架；多個＝角色槽），
///       接到打字引擎（讓立繪依每句的角色/表情切換；沒立繪的句子隱藏）。
///   2. 把三份文本轉成對話資訊表（DialogueData，放 <c>Resources/Dialogue/</c>）：
///        Text_014_L1-BOSS_Main_1 → L1-Boss_Intro（Boss 登場）
///        Text_015_L1-BOSS_Main_2 → L1-Boss_Defeated（打倒 Boss 後）
///        Text_016_L1-Joker_Main_1 → L1-Joker（Joker 對話；「？？？」用 Style_Joker＝Sephil 剪影 Shadow）
///      文本是新兩欄格式（人物[part]對話），立繪表情取自 LS2 舊三欄原稿（內建於本工具）。
///      Orlana/Jephthah/Seraphis 讀風格表＋Spine 表情；Unknown 顯示「？？？」、Narration 不顯示名稱（自定義模式）。
///      已存在且有對白的資產不覆寫（尊重手改）；要重建請用「強制重建對白」選單。
///   3. 在目前開啟的 LevelMap 場景建立/更新 <c>JokerSecret</c> 物件並接好：對話 UI、三段對話、Joker BGM、音效；
///      Boss Stage／Boss 敵人型別自動取「含 BossCombat 節點的 Stage」，找不到就留空待手動指定。
///
/// 使用方式：開啟 LevelMapSample → Tools/Tutorial/接上 L1 Boss 與 Joker 劇情 (Setup L1 Boss Story)。
/// 可重複執行：資產就地更新（GUID 不變、引用不會斷）、場景物件依名稱找到後更新。
/// </summary>
public static class L1BossStorySetupTool
{
    const string DialogueFolder = "Assets/LostStar/Resources/Dialogue";
    const string OrlanaStylePath = DialogueFolder + "/Style_Orlana.asset";
    const string JephthahStyleGuid = "6c4b3ab1de92fba4b9c909ea4cf6f8fc";
    const string SeraphisStyleGuid = "f9f6ac0c1381e904589d4f22a45269e3";
    const string GoddessSpineGuid = "f21b7d33d3dbfb84d970389e2ed55a13"; // Story_Goddess_SkeletonData
    const string JokerStylePath = DialogueFolder + "/Style_Joker.asset";
    const string SephilSpineGuid = "c23c7dcdaeead544281a8a9cde7fdab9";  // Sephil_SkeletonData（Joker 剪影，動畫 Shadow）
    const string JokerExpression = "Shadow";

    const string Txt014Guid = "da47a6fc53ead7a448075f2265550550";
    const string Txt015Guid = "c7655e92ff95fbf4a917f8aa3ab52d7e";
    const string Txt016Guid = "a5ec11a117faf704da27a99003707540";

    const string JokerBgmGuid = "74957b99f6e97144187c42efa4418134";     // Audio/BGM/L1/BGM_JokerSecret.mp3
    const string ShockSfxPath = "Assets/LostStar/Audio/SFX/SFX_EarthQuake.mp3";
    const string ExplosionSfxPath = "Assets/LostStar/Audio/SFX/SFX_Space explosion.mp3";

    const string DialogueEmptyGuid = "81fd412e08540e54faa1585aa932e9f9"; // Prefabs/Dialogue/DialogueEmpty.prefab
    const string PortraitHolderPath = "DialogueUI/CharacterAnim_BehindPanel_Pos/CharacterAnim_BehindPanel_East";

    const string ObjectName = "JokerSecret";
    const int ShakeStartLine = 5; // Text_015 第 5 句：（地面開始震動…）

    // LS2 舊三欄原稿的表情（依行序；與兩欄文本行數一致時才套用）
    static readonly string[] Expr014 = { "Noexpression", "Smile", "Watch", "Watch", "Smile" };
    static readonly string[] Expr015 = { "Closeeyes", "Watch", "Closeeyes", "Closeeyes", "Noexpression", "Watch", "Closeeyes",
                                         "Noexpression", "Noexpression", "Watch", "Surprise", "Watch", "Watch" };

    struct Spec { public string name, txtGuid; public string[] expr; public bool unknownIsJoker; }
    static readonly Spec[] Specs =
    {
        new Spec { name = "L1-Boss_Intro",    txtGuid = Txt014Guid, expr = Expr014 },
        new Spec { name = "L1-Boss_Defeated", txtGuid = Txt015Guid, expr = Expr015 },
        new Spec { name = "L1-Joker",         txtGuid = Txt016Guid, expr = null, unknownIsJoker = true },
    };

    [MenuItem("Tools/Tutorial/接上 L1 Boss 與 Joker 劇情 (Setup L1 Boss Story)")]
    public static void Setup()
    {
        bool ok = Build(false, out string report);
        EditorUtility.DisplayDialog(ok ? "L1 Boss 劇情接線完成" : "L1 Boss 劇情接線失敗", report, "OK");
    }

    [MenuItem("Tools/Tutorial/接上 L1 Boss 與 Joker 劇情（強制重建對白）")]
    public static void SetupForceRebuild()
    {
        if (!EditorUtility.DisplayDialog("強制重建對白", "會用文本重建三份對話資訊表的對白（覆蓋手改的立繪/表情等設定），確定？", "重建", "取消")) return;
        bool ok = Build(true, out string report);
        EditorUtility.DisplayDialog(ok ? "L1 Boss 劇情接線完成" : "L1 Boss 劇情接線失敗", report, "OK");
    }

    /// <summary>執行全部步驟（供 coplay/腳本呼叫，不跳對話框）。forceRebuildLines＝即使已有對白也重建。</summary>
    public static bool Build(bool forceRebuildLines, out string report)
    {
        var sb = new StringBuilder();

        // ── 1. 奧蘭娜風格表 ──
        var orlana = AssetDatabase.LoadAssetAtPath<CharacterStyleData>(OrlanaStylePath);
        if (orlana == null)
        {
            orlana = ScriptableObject.CreateInstance<CharacterStyleData>();
            orlana.characterName = DialogueUnitType.Orlana;
            orlana.displayName = "Orlana";
            orlana.themeColors = new List<Color> { new Color(0.61f, 0.47f, 0.82f, 1f) }; // #9C79D1（文本中伊婓的紫）
            orlana.spinePortrait = LoadByGuid<SkeletonDataAsset>(GoddessSpineGuid);
            if (orlana.spinePortrait == null) sb.AppendLine("⚠ 找不到 Story_Goddess 的 Spine 資料，奧蘭娜將沒有立繪");
            AssetDatabase.CreateAsset(orlana, OrlanaStylePath);
            sb.AppendLine("＋ 建立 Style_Orlana");
        }
        // Joker 風格表：名稱維持「？？？」、立繪＝Sephil 剪影（Shadow），配合 DialogueEmpty 的 Black_Up/Black_Down 斜黑幕
        var jokerStyle = AssetDatabase.LoadAssetAtPath<CharacterStyleData>(JokerStylePath);
        if (jokerStyle == null)
        {
            jokerStyle = ScriptableObject.CreateInstance<CharacterStyleData>();
            jokerStyle.characterName = DialogueUnitType.Unknown;
            jokerStyle.displayName = "？？？";
            jokerStyle.themeColors = new List<Color> { Color.white };
            jokerStyle.spinePortrait = LoadByGuid<SkeletonDataAsset>(SephilSpineGuid);
            if (jokerStyle.spinePortrait == null) sb.AppendLine("⚠ 找不到 Sephil 的 Spine 資料，Joker 將沒有剪影立繪");
            AssetDatabase.CreateAsset(jokerStyle, JokerStylePath);
            sb.AppendLine("＋ 建立 Style_Joker");
        }

        var styles = new Dictionary<DialogueUnitType, CharacterStyleData>
        {
            { DialogueUnitType.Orlana, orlana },
            { DialogueUnitType.Jephthah, LoadByGuid<CharacterStyleData>(JephthahStyleGuid) },
            { DialogueUnitType.Seraphis, LoadByGuid<CharacterStyleData>(SeraphisStyleGuid) },
        };

        // ── 1b. DialogueEmpty 立繪接線（讓立繪依每句的角色/表情切換）──
        WireDialoguePortraits(sb);

        // ── 2. 三份對話資訊表 ──
        var built = new List<DialogueData>();
        foreach (var spec in Specs)
        {
            var specStyles = new Dictionary<DialogueUnitType, CharacterStyleData>(styles);
            if (spec.unknownIsJoker) specStyles[DialogueUnitType.Unknown] = jokerStyle;
            var d = BuildDialogue(spec, specStyles, forceRebuildLines, sb);
            if (d == null) { report = sb.ToString(); return false; }
            built.Add(d);
        }
        AssetDatabase.SaveAssets();

        // ── 3. 場景 JokerSecret ──
        Scene scene = SceneManager.GetActiveScene();
        var levelMap = Object.FindAnyObjectByType<LevelMapManager>(FindObjectsInactive.Include);
        if (levelMap == null)
        {
            sb.AppendLine("⚠ 目前場景沒有 LevelMapManager，只建立了對話資料；請開啟 LevelMapSample 再執行一次以接上場景。");
            report = sb.ToString();
            return true;
        }

        GameObject go = null;
        foreach (var r in scene.GetRootGameObjects()) if (r.name == ObjectName) { go = r; break; }
        if (go == null)
        {
            go = new GameObject(ObjectName);
            SceneManager.MoveGameObjectToScene(go, scene);
            Undo.RegisterCreatedObjectUndo(go, "Create " + ObjectName);
            sb.AppendLine("＋ 建立場景物件 JokerSecret");
        }
        var js = go.GetComponent<JokerSecret>();
        if (js == null) js = Undo.AddComponent<JokerSecret>(go);

        var so = new SerializedObject(js);
        SetRef(so, "dialogue", Object.FindAnyObjectByType<TriggerDialogue>(FindObjectsInactive.Include), sb);
        SetRef(so, "levelMap", levelMap, sb);
        SetRef(so, "player", Object.FindAnyObjectByType<S001_PlayerController>(FindObjectsInactive.Include), sb);
        SetSequence(so, "bossIntroDialogue", built[0]);
        SetSequence(so, "bossDefeatedDialogue", built[1]);
        SetSequence(so, "jokerDialogue", built[2]);
        so.FindProperty("shakeStartLine").intValue = ShakeStartLine;

        // Joker 斜黑幕裝飾：DialogueEmpty 內的 Black_Up / Black_Down（Joker 對話期間顯示）
        var bgList = so.FindProperty("jokerBlackBG");
        bgList.ClearArray();
        var trigger = Object.FindAnyObjectByType<TriggerDialogue>(FindObjectsInactive.Include);
        if (trigger != null)
        {
            foreach (var t in trigger.transform.root.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != "Black_Up" && t.name != "Black_Down") continue;
                bgList.InsertArrayElementAtIndex(bgList.arraySize);
                bgList.GetArrayElementAtIndex(bgList.arraySize - 1).objectReferenceValue = t.gameObject;
            }
        }
        sb.AppendLine(bgList.arraySize > 0 ? $"・Joker 斜黑幕裝飾：{bgList.arraySize} 個（Black_Up/Black_Down）" : "⚠ 對話 UI 內找不到 Black_Up / Black_Down");
        SetRef(so, "jokerBgm", LoadByGuid<AudioClip>(JokerBgmGuid), sb);
        SetRef(so, "shockSfx", AssetDatabase.LoadAssetAtPath<AudioClip>(ShockSfxPath), sb);
        SetRef(so, "spaceExplosionSfx", AssetDatabase.LoadAssetAtPath<AudioClip>(ExplosionSfxPath), sb);

        // Boss Stage：找含 BossCombat 節點的 Stage；已手動指定則保留
        var bossStageProp = so.FindProperty("bossStage");
        if (bossStageProp.objectReferenceValue == null)
        {
            StageInfo bossStage = null;
            NodeEvent bossNode = null;
            foreach (var s in levelMap.stages)
            {
                if (s == null) continue;
                foreach (var ne in s.GetComponentsInChildren<NodeEvent>(true))
                    if (ne.eventType == NodeEventType.BossCombat) { bossStage = s; bossNode = ne; break; }
                if (bossStage != null) break;
            }
            if (bossStage != null)
            {
                bossStageProp.objectReferenceValue = bossStage;
                so.FindProperty("bossEnemyType").enumValueIndex = (int)bossNode.enemyType;
                sb.AppendLine($"・Boss Stage＝{bossStage.name}（BossCombat 節點 {bossNode.name}，敵人 {bossNode.enemyType}）");
            }
            else sb.AppendLine("⚠ 場景的 Stage 中沒有 BossCombat 節點：請在 JokerSecret 手動指定 bossStage 與 bossEnemyType");
        }
        else sb.AppendLine($"・Boss Stage 保留手動指定：{bossStageProp.objectReferenceValue.name}");

        so.ApplyModifiedProperties();
        EditorSceneManager.MarkSceneDirty(scene);
        sb.AppendLine("✔ 完成。請存檔場景（Ctrl+S）。");
        report = sb.ToString();
        return true;
    }

    /// <summary>
    /// 在 DialogueEmpty prefab 的立繪容器（CharacterAnim_BehindPanel_East）掛 CharacterPortraitController，
    /// 把底下預先擺好的各角色 SkeletonGraphic 登錄為立繪槽，並接到打字引擎（無立繪的句子隱藏立繪）。
    /// </summary>
    static void WireDialoguePortraits(StringBuilder sb)
    {
        string path = AssetDatabase.GUIDToAssetPath(DialogueEmptyGuid);
        if (string.IsNullOrEmpty(path)) { sb.AppendLine("⚠ 找不到 DialogueEmpty prefab，略過立繪接線"); return; }

        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var holder = root.transform.Find(PortraitHolderPath);
            var typer = root.GetComponentInChildren<DialogueTypingEffect>(true);
            if (holder == null || typer == null) { sb.AppendLine($"⚠ DialogueEmpty 內找不到 {PortraitHolderPath} 或 DialogueTypingEffect，略過立繪接線"); return; }

            var ctrl = holder.GetComponent<CharacterPortraitController>();
            if (ctrl == null) ctrl = holder.gameObject.AddComponent<CharacterPortraitController>();

            // 只有一個 SkeletonGraphic → 當 portraitSpine（每句換骨架）；多個 → 登錄為角色槽（各自保留擺位）
            var found = new List<SkeletonGraphic>();
            foreach (Transform child in holder)
            {
                var sg = child.GetComponent<SkeletonGraphic>();
                if (sg != null) found.Add(sg);
            }
            if (found.Count == 0) { sb.AppendLine("⚠ 立繪容器下沒有 SkeletonGraphic，略過立繪接線"); return; }

            var cso = new SerializedObject(ctrl);
            cso.FindProperty("portraitSpine").objectReferenceValue = found[0];
            var slots = cso.FindProperty("spineSlots");
            slots.ClearArray();
            if (found.Count > 1)
                for (int i = 0; i < found.Count; i++)
                {
                    slots.InsertArrayElementAtIndex(i);
                    slots.GetArrayElementAtIndex(i).objectReferenceValue = found[i];
                }
            cso.ApplyModifiedPropertiesWithoutUndo();
            int n = found.Count;

            var tso = new SerializedObject(typer);
            tso.FindProperty("portraitController").objectReferenceValue = ctrl;
            tso.FindProperty("hidePortraitWhenLineHasNone").boolValue = true;
            tso.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
            sb.AppendLine(n == 1 ? $"・DialogueEmpty 立繪接線：{found[0].name}（每句依風格表換骨架）" : $"・DialogueEmpty 立繪接線：{n} 個角色槽");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    static DialogueData BuildDialogue(Spec spec, Dictionary<DialogueUnitType, CharacterStyleData> styles, bool force, StringBuilder sb)
    {
        var txt = LoadByGuid<TextAsset>(spec.txtGuid);
        if (txt == null) { sb.AppendLine($"✖ 找不到文本（GUID {spec.txtGuid}）"); return null; }

        string path = $"{DialogueFolder}/{spec.name}.asset";
        var data = AssetDatabase.LoadAssetAtPath<DialogueData>(path);
        bool created = false;
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<DialogueData>();
            AssetDatabase.CreateAsset(data, path);
            created = true;
        }

        data.dialogueType = DialogueType.MainStory;
        data.dialogueTextFile = txt;

        if (created || force || data.LineCount == 0)
        {
            data.lines = ParseLines(txt.text, spec.expr, styles, spec.name, sb);
            sb.AppendLine($"{(created ? "＋ 建立" : "↻ 重建")} {spec.name}：{data.LineCount} 句");
        }
        else sb.AppendLine($"・{spec.name} 已有 {data.LineCount} 句，保留（要重建請用強制重建選單）");

        EditorUtility.SetDirty(data);
        return data;
    }

    static List<DialogueData.DialogueLine> ParseLines(string text, string[] expr, Dictionary<DialogueUnitType, CharacterStyleData> styles,
                                                      string name, StringBuilder sb)
    {
        var raw = new List<string>();
        foreach (var l in text.Split('\n'))
        {
            string s = Clean(l);
            if (!string.IsNullOrEmpty(s)) raw.Add(s);
        }
        if (expr != null && expr.Length != raw.Count)
        {
            sb.AppendLine($"⚠ {name}：文本 {raw.Count} 行與內建表情 {expr.Length} 筆不符，略過表情");
            expr = null;
        }

        var result = new List<DialogueData.DialogueLine>();
        for (int i = 0; i < raw.Count; i++)
        {
            int cut = raw[i].IndexOf("[part]");
            if (cut < 0) { sb.AppendLine($"⚠ {name} 第 {i + 1} 行缺少 [part]，略過：{raw[i]}"); continue; }
            string who = Clean(raw[i].Substring(0, cut));
            string content = Clean(raw[i].Substring(cut + "[part]".Length));
            var unit = ParseUnit(who, name, sb);

            var line = new DialogueData.DialogueLine { text = content };
            if (styles.TryGetValue(unit, out var style) && style != null)
            {
                line.useCharacterStyle = true;
                line.characterStyle = style;
                line.themeColorIndex = 0;
                string e = expr != null ? expr[i] : (style.HasSpine && style.characterName == DialogueUnitType.Unknown ? JokerExpression : null);
                line.spineExpression = (style.HasSpine && !string.IsNullOrEmpty(e) && e != "Noexpression") ? e : null;
                line.speaker = unit;
            }
            else
            {
                line.useCharacterStyle = false;
                line.speaker = unit;
                line.displayName = unit == DialogueUnitType.Unknown ? "？？？" : unit == DialogueUnitType.Narration ? " " : unit.ToString();
                line.customNameColor = Color.white;
            }
            result.Add(line);
        }
        return result;
    }

    static DialogueUnitType ParseUnit(string who, string name, StringBuilder sb)
    {
        foreach (DialogueUnitType t in System.Enum.GetValues(typeof(DialogueUnitType)))
            if (t.ToString() == who) return t;
        sb.AppendLine($"⚠ {name}：人物「{who}」不在 DialogueUnitType，記為 Unknown");
        return DialogueUnitType.Unknown;
    }

    static string Clean(string s) => string.IsNullOrEmpty(s) ? "" : s.Trim().Trim((char)0x200B, (char)0xFEFF);

    static T LoadByGuid<T>(string guid) where T : Object
    {
        string p = AssetDatabase.GUIDToAssetPath(guid);
        return string.IsNullOrEmpty(p) ? null : AssetDatabase.LoadAssetAtPath<T>(p);
    }

    static void SetRef(SerializedObject so, string prop, Object value, StringBuilder sb)
    {
        var p = so.FindProperty(prop);
        if (p == null) { sb.AppendLine($"⚠ JokerSecret 缺少欄位 {prop}"); return; }
        p.objectReferenceValue = value;
        if (value == null) sb.AppendLine($"⚠ {prop} 找不到可接的物件/資源");
    }

    // 填入 DialogueSequence（只改段落；keepUIOpenBetween 保留編輯者的選擇）
    static void SetSequence(SerializedObject so, string prop, DialogueData data)
    {
        var list = so.FindProperty(prop).FindPropertyRelative("dialogues");
        list.ClearArray();
        list.InsertArrayElementAtIndex(0);
        list.GetArrayElementAtIndex(0).objectReferenceValue = data;
    }
}
