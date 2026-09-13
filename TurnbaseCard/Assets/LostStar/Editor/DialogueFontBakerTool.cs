using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Dialogue 字型烘焙工具：
/// 掃描「選取的」DialogueData（可加入多個）實際用到的字元
/// （對白文本 + 顯示名稱；行有指定人物風格資訊表時含風格表的顯示名稱），
/// 再以指定的 TTF / OTF 字體檔烘焙出「靜態」TMP SDF 字型資產（xxx SDF.asset）。
///
/// 只烘焙有用到的字元 → 圖集小、載入快，執行期不需動態產生字形（不會卡頓）。
/// 使用方式：Unity 選單 → Tools → Dialogue → 字型烘焙 (Bake SDF Font from DialogueData)
/// 步驟：1. 指定字體檔 → 2. 加入掃描目標 → 3. 掃描字元 → 4. 產生 SDF 字型資產。
/// </summary>
public class DialogueFontBakerTool : EditorWindow
{
    // ---- 設定 ----
    private Font sourceFont;                       // 來源字體檔（TTF / OTF）
    private string outputDir = "Assets/Font";      // 輸出資料夾
    private string outputName = "";                // 輸出資產名稱（留空 = 用預設命名）
    private int samplingSize = 90;                 // 取樣點大小（SDF 品質）
    private int padding = 9;                       // 字形間距（SDF 邊緣範圍）
    private int atlasSizeIndex = 1;                // 圖集尺寸索引
    private bool includeAscii = true;              // 額外納入基本 ASCII（英數與常用符號）
    private string extraChars = "";                // 額外手動指定的字元

    // ---- 掃描目標 ----
    private readonly List<DialogueData> targetDataList = new List<DialogueData>(); // 要掃描的 DialogueData（可多個）
    private Vector2 targetScroll;

    // ---- 掃描結果 ----
    private string collectedChars = "";            // 收集到的不重複字元
    private int scannedDataCount;                  // 實際掃描的 DialogueData 數量
    private Vector2 charScroll;

    private static readonly int[] AtlasSizes = { 512, 1024, 2048, 4096 };
    private static readonly string[] AtlasSizeLabels = { "512", "1024", "2048", "4096" };

    // 與 DialogueTypingEffect 相同的停頓標記語法；富文本標籤（<...>）不會被顯示，也一併剔除
    private static readonly Regex PauseRegex =
        new Regex(@"\|pause(=\d+(\.\d+)?)?\|", RegexOptions.Compiled);
    private static readonly Regex RichTagRegex =
        new Regex(@"<[^<>]*>", RegexOptions.Compiled);

    [MenuItem("Tools/Dialogue/字型烘焙 (Bake SDF Font from DialogueData)")]
    private static void Open()
    {
        var window = GetWindow<DialogueFontBakerTool>("Dialogue 字型烘焙");
        window.minSize = new Vector2(420f, 520f);
    }

    private void OnEnable()
    {
        // 預設帶入專案現有的中文字型檔（存在的話）
        if (sourceFont == null)
        {
            sourceFont = AssetDatabase.LoadAssetAtPath<Font>("Assets/Font/TaipeiSansTCBeta-Regular.ttf");
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("字體與輸出", EditorStyles.boldLabel);
        sourceFont = (Font)EditorGUILayout.ObjectField(
            new GUIContent("字體檔 (TTF / OTF)", "要用來產生 SDF 字形的來源字體檔。"), sourceFont, typeof(Font), false);
        outputDir = EditorGUILayout.TextField(
            new GUIContent("輸出資料夾", "產生的 SDF.asset 存放位置。"), outputDir);
        outputName = EditorGUILayout.TextField(
            new GUIContent("輸出資產名稱", "產生的字型資產檔名（不含副檔名）。留空 = 使用預設命名。"), outputName);
        EditorGUILayout.LabelField(" ", $"輸出：{GetOutputPath()}", EditorStyles.miniLabel);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("烘焙設定", EditorStyles.boldLabel);
        samplingSize = EditorGUILayout.IntSlider(
            new GUIContent("取樣點大小", "越大字形越精細，但佔用圖集空間越多。一般 70~90。"), samplingSize, 40, 120);
        padding = EditorGUILayout.IntSlider(
            new GUIContent("Padding", "SDF 邊緣範圍，影響外框 / 陰影等效果的最大寬度。一般為取樣的 1/10。"), padding, 4, 16);
        atlasSizeIndex = EditorGUILayout.Popup(
            new GUIContent("圖集尺寸", "字元多（中文對白）建議 1024 以上；放不下會自動增建多張圖集。"),
            atlasSizeIndex, AtlasSizeLabels);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("掃描目標（可加入多個 DialogueData）", EditorStyles.boldLabel);
        DrawTargetList();

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("字元來源", EditorStyles.boldLabel);
        includeAscii = EditorGUILayout.ToggleLeft(
            new GUIContent(" 額外納入基本 ASCII（英數與常用符號）", "系統 UI 或數字顯示常會用到，建議保持勾選。"), includeAscii);
        EditorGUILayout.LabelField("額外字元（手動補充）");
        extraChars = EditorGUILayout.TextArea(extraChars, GUILayout.MinHeight(36f));

        EditorGUILayout.Space(8f);
        bool hasTarget = targetDataList.Exists(d => d != null);
        using (new EditorGUI.DisabledScope(!hasTarget))
        {
            if (GUILayout.Button("1. 掃描選取的 DialogueData 用到的字元", GUILayout.Height(28f)))
            {
                CollectCharacters();
            }
        }
        if (!hasTarget)
        {
            EditorGUILayout.HelpBox("請先在「掃描目標」加入至少一個 DialogueData。", MessageType.Warning);
        }

        if (collectedChars.Length > 0)
        {
            EditorGUILayout.HelpBox(
                $"掃描結果：{scannedDataCount} 個 DialogueData，共 {collectedChars.Length} 個不重複字元。",
                MessageType.Info);
            charScroll = EditorGUILayout.BeginScrollView(charScroll, GUILayout.MinHeight(100f), GUILayout.MaxHeight(160f));
            EditorGUILayout.TextArea(collectedChars, EditorStyles.wordWrappedLabel);
            EditorGUILayout.EndScrollView();
        }

        EditorGUILayout.Space(4f);
        using (new EditorGUI.DisabledScope(sourceFont == null || collectedChars.Length == 0))
        {
            if (GUILayout.Button("2. 產生靜態 SDF 字型資產", GUILayout.Height(32f)))
            {
                BakeFontAsset();
            }
        }
        if (sourceFont == null)
        {
            EditorGUILayout.HelpBox("請先指定字體檔。", MessageType.Warning);
        }
        else if (collectedChars.Length == 0)
        {
            EditorGUILayout.HelpBox("請先執行「掃描字元」。", MessageType.None);
        }
    }

    #region 掃描目標清單

    /// <summary>畫「掃描目標」清單：多個 DialogueData 欄位 + 加入 / 移除 / 全部加入 / 清空。</summary>
    private void DrawTargetList()
    {
        targetScroll = EditorGUILayout.BeginScrollView(targetScroll, GUILayout.MaxHeight(140f));
        int removeIndex = -1;
        for (int i = 0; i < targetDataList.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            targetDataList[i] = (DialogueData)EditorGUILayout.ObjectField(
                targetDataList[i], typeof(DialogueData), false);
            if (GUILayout.Button("−", GUILayout.Width(24f)))
            {
                removeIndex = i;
            }
            EditorGUILayout.EndHorizontal();
        }
        if (removeIndex >= 0) targetDataList.RemoveAt(removeIndex);
        EditorGUILayout.EndScrollView();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("＋ 加入欄位"))
        {
            targetDataList.Add(null);
        }
        if (GUILayout.Button("加入 Project 選取中的"))
        {
            AddSelectedDialogueData();
        }
        if (GUILayout.Button("加入專案內全部"))
        {
            foreach (string guid in AssetDatabase.FindAssets("t:DialogueData"))
            {
                var data = AssetDatabase.LoadAssetAtPath<DialogueData>(AssetDatabase.GUIDToAssetPath(guid));
                if (data != null && !targetDataList.Contains(data)) targetDataList.Add(data);
            }
        }
        if (GUILayout.Button("清空", GUILayout.Width(50f)))
        {
            targetDataList.Clear();
        }
        EditorGUILayout.EndHorizontal();
    }

    /// <summary>把 Project 視窗目前選取的 DialogueData 加入清單（略過重複）。</summary>
    private void AddSelectedDialogueData()
    {
        foreach (var obj in Selection.objects)
        {
            if (obj is DialogueData data && !targetDataList.Contains(data))
            {
                targetDataList.Add(data);
            }
        }
    }

    #endregion

    #region 字元收集

    /// <summary>
    /// 掃描「掃描目標」清單中的 DialogueData，收集實際會顯示的字元
    /// （對白文本 + 顯示名稱；行有指定人物風格資訊表時，DisplayName 已含風格表名稱）。
    /// 文本會剔除 |pause| 停頓標記與富文本標籤（標籤本身不會被顯示）。
    /// </summary>
    private void CollectCharacters()
    {
        var set = new SortedSet<char>();

        void AddString(string s)
        {
            if (string.IsNullOrEmpty(s)) return;
            foreach (char c in s)
            {
                if (!char.IsControl(c)) set.Add(c);
            }
        }

        scannedDataCount = 0;
        var scanned = new HashSet<DialogueData>(); // 避免同一資料被加入多次而重複掃描
        foreach (var data in targetDataList)
        {
            if (data == null || data.lines == null || !scanned.Add(data)) continue;
            scannedDataCount++;

            foreach (var line in data.lines)
            {
                if (line == null) continue;
                AddString(CleanDisplayedText(line.text));
                AddString(line.DisplayName); // 含風格表顯示名稱或 speaker 列舉名稱
            }
        }

        if (includeAscii)
        {
            for (char c = ' '; c <= '~'; c++) set.Add(c);
        }
        AddString(extraChars);

        collectedChars = new string(set.ToArray());
        charScroll = Vector2.zero;
    }

    /// <summary>剔除文本中不會被顯示的部分：|pause| 停頓標記與富文本標籤。</summary>
    private static string CleanDisplayedText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        return RichTagRegex.Replace(PauseRegex.Replace(text, ""), "");
    }

    #endregion

    #region SDF 烘焙

    /// <summary>以收集到的字元，由來源字體檔烘焙靜態 TMP SDF 字型資產並存檔。</summary>
    private void BakeFontAsset()
    {
        int atlasSize = AtlasSizes[atlasSizeIndex];
        string path = GetOutputPath();

        try
        {
            EditorUtility.DisplayProgressBar("Dialogue 字型烘焙", "產生字形與 SDF 圖集中……", 0.3f);

            // 先以 Dynamic 模式建立，逐字加入字形後再轉為 Static（= 烘焙完成的靜態圖集）
            var fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, samplingSize, padding,
                GlyphRenderMode.SDFAA, atlasSize, atlasSize, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                EditorUtility.ClearProgressBar();
                EditorUtility.DisplayDialog("Dialogue 字型烘焙", "字型資產建立失敗，請確認字體檔格式是否正確。", "OK");
                return;
            }

            fontAsset.TryAddCharacters(collectedChars, out string missing);

            EditorUtility.DisplayProgressBar("Dialogue 字型烘焙", "轉為靜態圖集並存檔中……", 0.7f);
            fontAsset.atlasPopulationMode = AtlasPopulationMode.Static;
            fontAsset.name = Path.GetFileNameWithoutExtension(path);

            // 覆蓋舊資產：先刪除再建立（GUID 會變，已引用處需重新指定字型）
            EnsureFolder(outputDir);
            if (AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            AssetDatabase.CreateAsset(fontAsset, path);

            // 圖集（可能多張）與材質存為子資產
            var atlases = fontAsset.atlasTextures;
            for (int i = 0; i < (atlases != null ? atlases.Length : 0); i++)
            {
                if (atlases[i] == null) continue;
                atlases[i].name = $"{fontAsset.name} Atlas {i}";
                AssetDatabase.AddObjectToAsset(atlases[i], fontAsset);
            }
            fontAsset.material.name = fontAsset.name + " Material";
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);

            // 靜態字型不再需要來源字體檔的引用（避免 TTF 一併被打包進版本）
            var so = new SerializedObject(fontAsset);
            var sourceProp = so.FindProperty("m_SourceFontFile");
            if (sourceProp != null)
            {
                sourceProp.objectReferenceValue = null;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorUtility.ClearProgressBar();

            EditorGUIUtility.PingObject(fontAsset);

            string message = $"烘焙完成！\n\n輸出：{path}\n字元數：{collectedChars.Length}";
            if (!string.IsNullOrEmpty(missing))
            {
                message += $"\n\n注意：來源字體檔缺少 {missing.Length} 個字元的字形：\n{Truncate(missing, 100)}";
            }
            EditorUtility.DisplayDialog("Dialogue 字型烘焙", message, "OK");
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }
    }

    /// <summary>
    /// 組出輸出路徑：使用「輸出資產名稱」；留空時退回預設命名（字體檔名 + " SDF (Baked)"）。
    /// 會剔除檔名不允許的字元，並忽略使用者多打的 .asset 副檔名。
    /// </summary>
    private string GetOutputPath()
    {
        string name = string.IsNullOrWhiteSpace(outputName)
            ? (sourceFont != null ? sourceFont.name + " SDF (Baked)" : "Dialogue SDF (Baked)")
            : outputName.Trim();

        if (name.EndsWith(".asset", System.StringComparison.OrdinalIgnoreCase))
        {
            name = name.Substring(0, name.Length - ".asset".Length);
        }
        foreach (char c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c.ToString(), "");
        }
        if (string.IsNullOrWhiteSpace(name)) name = "Dialogue SDF (Baked)";

        return $"{outputDir}/{name}.asset";
    }

    private static string Truncate(string s, int max) =>
        s.Length <= max ? s : s.Substring(0, max) + "…";

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    #endregion
}
