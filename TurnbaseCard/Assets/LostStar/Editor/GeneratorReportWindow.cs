using UnityEditor;
using UnityEngine;

/// <summary>
/// 固定大小、可捲動的產生器報告視窗——取代會因報告太長而超出畫面的 <see cref="EditorUtility.DisplayDialog"/>。
/// 用法：<c>GeneratorReportWindow.Show("標題", reportText);</c>
/// 由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class GeneratorReportWindow : EditorWindow
{
    private string report = "";
    private Vector2 scroll;

    public static void Show(string title, string reportText)
    {
        var w = GetWindow<GeneratorReportWindow>(true, title, true);
        w.report = reportText ?? "";
        w.minSize = new Vector2(460, 300);
        w.maxSize = new Vector2(1000, 800);
        // 固定一個合理起始大小（面板本身可再拉，內容超出就用捲軸）
        w.position = new Rect(w.position.x, w.position.y, 580, 460);
        w.scroll = Vector2.zero;
        w.Show();
        w.Focus();
    }

    private void OnGUI()
    {
        GUILayout.Space(4);
        scroll = EditorGUILayout.BeginScrollView(scroll);
        // 依內容算高度給 SelectableLabel，內容超出面板時由 ScrollView 提供捲軸
        float width = Mathf.Max(200f, EditorGUIUtility.currentViewWidth - 26f);
        float height = EditorStyles.wordWrappedLabel.CalcHeight(new GUIContent(report), width);
        EditorGUILayout.SelectableLabel(report, EditorStyles.wordWrappedLabel,
            GUILayout.Width(width), GUILayout.Height(height));
        EditorGUILayout.EndScrollView();

        GUILayout.Space(4);
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("複製全部", GUILayout.Height(24))) EditorGUIUtility.systemCopyBuffer = report;
            if (GUILayout.Button("關閉", GUILayout.Height(24))) Close();
        }
        GUILayout.Space(4);
    }
}
