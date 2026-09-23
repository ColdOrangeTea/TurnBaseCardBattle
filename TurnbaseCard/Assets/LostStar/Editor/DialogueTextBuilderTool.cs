using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 對話文本編輯器（由 A_Good_Ink 使用 AI 生成）。
///
/// 做什麼：方便製作 DialogueData 可讀取的對話 txt。提供
///   - 編輯區：可直接打字/貼上，並用按鈕在「目前游標處」插入自定義語法（[part]、|pause|、
///     |pause=秒數|、TMP 顏色標籤、換行）。
///   - 載入既有 txt 到編輯區、覆寫來源或另存新 txt。
///   - 舊格式轉換：把舊的三欄（人物[part]表情[part]對話）去掉「表情」欄，轉成新的兩欄
///     （人物[part]對話）；可單檔（對編輯區內容）或批次（對整個資料夾）。
///
/// 產出：純文字 .txt（放在使用者指定路徑；批次預設另存 *_2col.txt，勾「覆寫原檔」才就地覆蓋）。
///
/// 使用方式：Unity 上方選單 → Tools → Dialogue → 對話文本編輯器 (Dialogue Text Builder)。
///
/// 可重複執行：批次轉換就地覆蓋更新（GUID 不變，引用不會斷）；不刪除重建。
/// </summary>
public class DialogueTextBuilderTool : EditorWindow
{
    // ---- 語法常數（與 DialogueData / DialogueTypingEffect 對齊）----
    private const string PartDelimiter = "[part]";   // 人物與對話的分隔
    private const string PauseTag = "|pause|";       // 停頓（用該行預設秒數）
    private const string PauseValueTag = "|pause=1|"; // 停頓指定秒數（範本）
    private const string ColorOpen = "<color=#84C1FF>";
    private const string ColorClose = "</color>";

    private const string EditorControlName = "DialogueTextBuilderArea";
    private const string ConvertSuffix = "_2col";

    // ---- 狀態 ----
    private string _text = "";
    private TextAsset _loadedAsset;      // 載入來源（可覆寫）
    private DefaultAsset _batchFolder;   // 批次轉換的來源資料夾
    private bool _batchOverwrite = false; // 批次是否覆寫原檔
    private bool _batchRecursive = true;  // 批次是否含子資料夾
    private Vector2 _scroll;
    private string _status = "";

    // 游標（在編輯區有焦點時持續更新；插入按鈕依此決定插入位置）
    private int _caret, _select;
    private int _pendingCaret = -1; // 插入後要還原到的游標位置（下一幀套用）

    [MenuItem("Tools/Dialogue/對話文本編輯器 (Dialogue Text Builder)")]
    private static void Open()
    {
        var window = GetWindow<DialogueTextBuilderTool>("對話文本編輯器");
        window.minSize = new Vector2(480f, 560f);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(4f);

        DrawFileSection();
        EditorGUILayout.Space(6f);
        DrawInsertButtons();
        EditorGUILayout.Space(4f);
        DrawEditorArea();
        EditorGUILayout.Space(6f);
        DrawConvertSection();

        if (!string.IsNullOrEmpty(_status))
        {
            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(_status, MessageType.Info);
        }
    }

    // ────────────────────────────────────────────────────────────
    // 檔案：載入 / 儲存
    // ────────────────────────────────────────────────────────────
    private void DrawFileSection()
    {
        EditorGUILayout.LabelField("檔案", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            var newAsset = (TextAsset)EditorGUILayout.ObjectField(
                new GUIContent("來源 txt", "把既有 txt 拖進來，按「載入」讀進編輯區。"),
                _loadedAsset, typeof(TextAsset), false);
            if (newAsset != _loadedAsset) _loadedAsset = newAsset;

            using (new EditorGUI.DisabledScope(_loadedAsset == null))
                if (GUILayout.Button("載入到編輯區", GUILayout.Width(110f)))
                    LoadFromAsset();
        }

        using (new EditorGUILayout.HorizontalScope())
        {
            using (new EditorGUI.DisabledScope(_loadedAsset == null))
                if (GUILayout.Button(new GUIContent("覆寫來源 txt", "把編輯區內容寫回上面載入的來源 txt。")))
                    SaveToLoadedAsset();

            if (GUILayout.Button(new GUIContent("另存新 txt…", "把編輯區內容存成新的 txt 檔。")))
                SaveAsNew();
        }
    }

    private void LoadFromAsset()
    {
        if (_loadedAsset == null) return;
        _text = _loadedAsset.text;
        _pendingCaret = 0;
        _status = $"已載入「{_loadedAsset.name}」（{CountLines(_text)} 行）到編輯區。";
        GUI.FocusControl(null);
    }

    private void SaveToLoadedAsset()
    {
        if (_loadedAsset == null) return;
        string path = AssetDatabase.GetAssetPath(_loadedAsset);
        if (string.IsNullOrEmpty(path)) { _status = "找不到來源 txt 的路徑，無法覆寫。"; return; }
        WriteTextFile(path, _text);
        _status = $"已覆寫「{path}」（{CountLines(_text)} 行）。";
    }

    private void SaveAsNew()
    {
        string path = EditorUtility.SaveFilePanelInProject(
            "另存對話 txt", "NewDialogue", "txt", "選擇要儲存的位置與檔名。");
        if (string.IsNullOrEmpty(path)) return;
        WriteTextFile(path, _text);
        _loadedAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        _status = $"已另存「{path}」（{CountLines(_text)} 行）。";
    }

    // ────────────────────────────────────────────────────────────
    // 插入語法（在目前游標處）
    // ────────────────────────────────────────────────────────────
    private void DrawInsertButtons()
    {
        EditorGUILayout.LabelField("在游標處插入", EditorStyles.boldLabel);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button(new GUIContent("[part]", "人物與對話的分隔符。格式：人物[part]對話"))) InsertAtCaret(PartDelimiter);
            if (GUILayout.Button(new GUIContent("|pause|", "停頓（秒數用該行預設，或打字機全域設定）。"))) InsertAtCaret(PauseTag);
            if (GUILayout.Button(new GUIContent("|pause=秒|", "停頓指定秒數（範本 |pause=1|，可改數字）。"))) InsertAtCaret(PauseValueTag);
            if (GUILayout.Button(new GUIContent("<color>", "TMP 顏色標籤，游標會停在標籤中間讓你打字。"))) InsertAtCaret(ColorOpen + ColorClose, ColorOpen.Length);
            if (GUILayout.Button(new GUIContent("換行 (新對白)", "插入換行，開始新的一行對白。"))) InsertAtCaret("\n");
        }
        EditorGUILayout.LabelField(
            "每行格式：人物[part]對話（未來格式，不含表情）。人物名需對應 DialogueUnitType。",
            EditorStyles.miniLabel);
    }

    private void InsertAtCaret(string snippet, int caretOffset = -1)
    {
        int a = Mathf.Clamp(Mathf.Min(_caret, _select), 0, _text.Length);
        int b = Mathf.Clamp(Mathf.Max(_caret, _select), 0, _text.Length);
        _text = _text.Substring(0, a) + snippet + _text.Substring(b);
        _pendingCaret = a + (caretOffset >= 0 ? caretOffset : snippet.Length);
        EditorGUI.FocusTextInControl(EditorControlName);
        Repaint();
    }

    // ────────────────────────────────────────────────────────────
    // 編輯區（含游標追蹤）
    // ────────────────────────────────────────────────────────────
    private void DrawEditorArea()
    {
        EditorGUILayout.LabelField($"編輯區（{CountLines(_text)} 行）", EditorStyles.boldLabel);
        var style = new GUIStyle(EditorStyles.textArea) { wordWrap = true, richText = false };

        _scroll = EditorGUILayout.BeginScrollView(_scroll, GUILayout.MinHeight(240f), GUILayout.ExpandHeight(true));
        GUI.SetNextControlName(EditorControlName);
        string newText = EditorGUILayout.TextArea(_text, style, GUILayout.ExpandHeight(true));
        if (newText != _text) _text = newText;
        EditorGUILayout.EndScrollView();

        // 有焦點時追蹤游標；若剛插入過，先把游標還原到插入後的位置
        if (GUI.GetNameOfFocusedControl() == EditorControlName)
        {
            var te = (TextEditor)GUIUtility.GetStateObject(typeof(TextEditor), GUIUtility.keyboardControl);
            if (te != null)
            {
                if (_pendingCaret >= 0)
                {
                    te.text = _text;
                    int c = Mathf.Clamp(_pendingCaret, 0, _text.Length);
                    te.cursorIndex = te.selectIndex = c;
                    _caret = _select = c;
                    _pendingCaret = -1;
                }
                else
                {
                    _caret = te.cursorIndex;
                    _select = te.selectIndex;
                }
            }
        }
    }

    // ────────────────────────────────────────────────────────────
    // 舊格式（3欄）→ 新格式（2欄）轉換
    // ────────────────────────────────────────────────────────────
    private void DrawConvertSection()
    {
        EditorGUILayout.LabelField("舊格式轉換：三欄 → 兩欄（去掉表情欄）", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("人物[part]表情[part]對話 → 人物[part]對話", EditorStyles.miniLabel);

        if (GUILayout.Button(new GUIContent("轉換編輯區內容（單檔）", "把上方編輯區的三欄內容就地轉成兩欄。")))
        {
            int changed;
            _text = ConvertOldToNew(_text, out changed);
            _pendingCaret = 0;
            _status = $"編輯區已轉換：{changed} 行由三欄改為兩欄。";
            GUI.FocusControl(null);
        }

        EditorGUILayout.Space(4f);
        EditorGUILayout.LabelField("批次轉換整個資料夾", EditorStyles.boldLabel);
        _batchFolder = (DefaultAsset)EditorGUILayout.ObjectField(
            new GUIContent("來源資料夾", "把 Project 視窗裡的資料夾拖進來。"), _batchFolder, typeof(DefaultAsset), false);
        _batchRecursive = EditorGUILayout.Toggle(new GUIContent("含子資料夾", "是否連子資料夾內的 txt 一起轉換。"), _batchRecursive);
        _batchOverwrite = EditorGUILayout.Toggle(
            new GUIContent("覆寫原檔", $"勾＝就地覆蓋原 txt；不勾＝另存為 檔名{ConvertSuffix}.txt（預設，較安全）。"), _batchOverwrite);

        using (new EditorGUI.DisabledScope(_batchFolder == null))
            if (GUILayout.Button(new GUIContent("批次轉換資料夾內所有 txt", "掃描資料夾內 .txt，三欄轉兩欄後輸出。")))
                BatchConvertFolder();
    }

    /// <summary>把三欄（人物[part]表情[part]對話）每行去掉中間「表情」欄；已是兩欄或格式不符的行原樣保留。</summary>
    private static string ConvertOldToNew(string src, out int changedLines)
    {
        changedLines = 0;
        if (string.IsNullOrEmpty(src)) return src;

        var outLines = new List<string>();
        foreach (var raw in src.Split('\n'))
        {
            string line = raw.TrimEnd('\r');
            string[] cols = line.Split(new[] { PartDelimiter }, System.StringSplitOptions.None);
            if (cols.Length >= 3)
            {
                // 保留人物(cols[0]) + 對話(cols[2..])，丟掉表情(cols[1])
                string content = string.Join(PartDelimiter, cols, 2, cols.Length - 2);
                outLines.Add(cols[0] + PartDelimiter + content);
                changedLines++;
            }
            else
            {
                outLines.Add(line); // 已是兩欄 / 空行 / 格式不符：原樣保留
            }
        }
        return string.Join("\n", outLines);
    }

    private void BatchConvertFolder()
    {
        string folderPath = AssetDatabase.GetAssetPath(_batchFolder);
        if (string.IsNullOrEmpty(folderPath) || !AssetDatabase.IsValidFolder(folderPath))
        {
            _status = "來源資料夾無效，請拖入 Project 視窗裡的資料夾。";
            return;
        }

        string absFolder = ToAbsolute(folderPath);
        var option = _batchRecursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
        var files = Directory.GetFiles(absFolder, "*.txt", option);

        int fileCount = 0, lineCount = 0;
        var sb = new StringBuilder();
        foreach (var abs in files)
        {
            // 避免把先前產生的 *_2col.txt 又轉一次
            if (!_batchOverwrite && Path.GetFileNameWithoutExtension(abs).EndsWith(ConvertSuffix)) continue;

            string content = File.ReadAllText(abs);
            string converted = ConvertOldToNew(content, out int changed);
            if (changed == 0 && _batchOverwrite) continue; // 沒有三欄行、又是覆寫模式：略過不動

            string outAbs = _batchOverwrite
                ? abs
                : Path.Combine(Path.GetDirectoryName(abs), Path.GetFileNameWithoutExtension(abs) + ConvertSuffix + ".txt");

            File.WriteAllText(outAbs, converted, new UTF8Encoding(false));
            fileCount++; lineCount += changed;
            sb.AppendLine($"  {Path.GetFileName(abs)} → {Path.GetFileName(outAbs)}（{changed} 行）");
        }

        AssetDatabase.Refresh();
        _status = fileCount == 0
            ? "沒有找到可轉換的 txt（或都沒有三欄行）。"
            : $"批次完成：{fileCount} 個檔、{lineCount} 行由三欄改為兩欄{(_batchOverwrite ? "（已覆寫原檔）" : $"（另存 *{ConvertSuffix}.txt）")}：\n{sb}";
    }

    // ────────────────────────────────────────────────────────────
    // 輔助
    // ────────────────────────────────────────────────────────────
    private static void WriteTextFile(string projectPath, string text)
    {
        string abs = ToAbsolute(projectPath);
        Directory.CreateDirectory(Path.GetDirectoryName(abs));
        File.WriteAllText(abs, text ?? "", new UTF8Encoding(false)); // 無 BOM，避免第一個人名前多出隱形字元
        AssetDatabase.ImportAsset(projectPath);
    }

    private static string ToAbsolute(string projectPath)
    {
        // projectPath 形如 "Assets/xxx"；Application.dataPath 為 ".../Assets"
        return Path.Combine(Application.dataPath, projectPath.Substring("Assets/".Length))
                   .Replace('\\', '/');
    }

    private static int CountLines(string s)
    {
        if (string.IsNullOrEmpty(s)) return 0;
        int n = 1;
        foreach (char c in s) if (c == '\n') n++;
        return n;
    }
}
