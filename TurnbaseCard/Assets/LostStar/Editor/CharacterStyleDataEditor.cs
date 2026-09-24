using UnityEditor;
using UnityEngine;

/// <summary>
/// CharacterStyleData 的自訂 Inspector（由 A_Good_Ink 使用 AI 生成）。
///
/// 立繪部分依「使用 Spine2D 資源」核對方塊二選一顯示：
///   - 取消：顯示立繪 Sprite 清單（現行作法）。
///   - 勾選：顯示 Spine2D 立繪資源欄位，並列出該資源偵測到的 Animation 名稱（＝立繪表情）。
/// </summary>
[CustomEditor(typeof(CharacterStyleData))]
public class CharacterStyleDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(serializedObject.FindProperty("characterName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("displayName"));
        EditorGUILayout.PropertyField(serializedObject.FindProperty("themeColors"), true);

        EditorGUILayout.Space(6f);
        EditorGUILayout.LabelField("立繪", EditorStyles.boldLabel);

        var useSpine = serializedObject.FindProperty("useSpine2D");
        EditorGUILayout.PropertyField(useSpine, new GUIContent("使用 Spine2D 資源",
            "勾選 = 用 Spine2D 立繪資源（表情＝Animation 名稱）；取消 = 用下方立繪 Sprite 清單。"));

        using (new EditorGUI.IndentLevelScope())
        {
            if (useSpine.boolValue)
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("spinePortrait"),
                    new GUIContent("Spine2D 立繪資源", "SkeletonDataAsset；表情由其 Animation 名稱提供。"));
                DrawSpineAnimationNames();
            }
            else
            {
                EditorGUILayout.PropertyField(serializedObject.FindProperty("portraits"),
                    new GUIContent("立繪清單 (Sprite)", "此人物的各表情立繪。"), true);
            }
        }

        serializedObject.ApplyModifiedProperties();
    }

    /// <summary>列出 Spine2D 立繪資源偵測到的 Animation 名稱（＝立繪表情）。</summary>
    private void DrawSpineAnimationNames()
    {
        var style = (CharacterStyleData)target;
        var names = style.GetSpineAnimationNames();

        EditorGUILayout.Space(2f);
        if (names.Count == 0)
        {
            EditorGUILayout.HelpBox("未指定 Spine2D 資源，或該資源沒有可用的 Animation（立繪表情）。", MessageType.Info);
            return;
        }

        EditorGUILayout.LabelField($"立繪表情（Animation，共 {names.Count} 個）", EditorStyles.boldLabel);
        using (new EditorGUI.IndentLevelScope())
            foreach (var n in names)
                EditorGUILayout.LabelField("• " + n);
    }
}
