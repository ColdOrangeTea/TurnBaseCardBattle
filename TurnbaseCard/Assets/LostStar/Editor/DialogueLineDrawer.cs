using UnityEditor;
using UnityEngine;

/// <summary>
/// DialogueData.DialogueLine 的自訂 Inspector 繪製。
/// 依「讀取人物風格資訊表 / 自定義」切換顯示欄位：
///   - 讀取模式：說話者與顯示名稱直接顯示風格表資料；
///     名稱顏色以主題色色塊點選、立繪以縮圖點選（含目前選取的放大預覽）。
///   - 自定義模式：手動填寫說話者、顯示名稱、名稱顏色與立繪（含預覽）。
/// </summary>
[CustomPropertyDrawer(typeof(DialogueData.DialogueLine))]
public class DialogueLineDrawer : PropertyDrawer
{
    private const float VPad = 2f;
    private const float ThumbSize = 52f;   // 立繪縮圖大小
    private const float ThumbPad = 4f;
    private const float PreviewSize = 84f; // 選取立繪的放大預覽大小
    private const float SwatchSize = 20f;  // 主題色色塊大小
    private const float SwatchPad = 4f;

    // 立繪位置預覽：以 Canvas 參考解析度為基準的畫面示意圖
    // （座標基準與立繪 Prefab 相同：錨點 / pivot = 畫面底部中央）

    private static readonly Color SelectColor = new Color(0.25f, 0.6f, 1f); // 選取框顏色
    private static readonly Color CellBgColor = new Color(0f, 0f, 0f, 0.15f); // 縮圖底色

    private static float LineH => EditorGUIUtility.singleLineHeight;

    private static GUIStyle centeredLabel;
    private static GUIStyle CenteredLabel
    {
        get
        {
            if (centeredLabel == null)
            {
                centeredLabel = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    wordWrap = true,
                };
            }
            return centeredLabel;
        }
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var textProp = property.FindPropertyRelative("text");
        var pauseProp = property.FindPropertyRelative("pauseDuration");
        var useStyleProp = property.FindPropertyRelative("useCharacterStyle");
        var styleProp = property.FindPropertyRelative("characterStyle");
        var colorIndexProp = property.FindPropertyRelative("themeColorIndex");
        var speakerProp = property.FindPropertyRelative("speaker");
        var displayNameProp = property.FindPropertyRelative("displayName");
        var customColorProp = property.FindPropertyRelative("customNameColor");
        var portraitProp = property.FindPropertyRelative("portrait");

        EditorGUI.BeginProperty(position, label, property);

        var foldRect = new Rect(position.x, position.y, position.width, LineH);
        property.isExpanded = EditorGUI.Foldout(foldRect, property.isExpanded,
            BuildHeader(useStyleProp, styleProp, speakerProp, displayNameProp, textProp), true);

        if (property.isExpanded)
        {
            EditorGUI.indentLevel++;
            float x = position.x;
            float w = position.width;
            float y = foldRect.yMax + VPad;

            float textH = EditorGUI.GetPropertyHeight(textProp);
            EditorGUI.PropertyField(new Rect(x, y, w, textH), textProp, new GUIContent("對白文本", textProp.tooltip));
            y += textH + VPad;

            EditorGUI.PropertyField(new Rect(x, y, w, LineH), pauseProp, new GUIContent("停頓秒數", pauseProp.tooltip));
            y += LineH + VPad;

            EditorGUI.PropertyField(new Rect(x, y, w, LineH), useStyleProp, new GUIContent("讀取人物風格資訊表", useStyleProp.tooltip));
            y += LineH + VPad;

            if (useStyleProp.boolValue)
            {
                EditorGUI.PropertyField(new Rect(x, y, w, LineH), styleProp, new GUIContent("人物風格資訊表", styleProp.tooltip));
                y += LineH + VPad;

                var style = styleProp.objectReferenceValue as CharacterStyleData;
                if (style == null)
                {
                    EditorGUI.HelpBox(EditorGUI.IndentedRect(new Rect(x, y, w, LineH * 2)),
                        "尚未指定人物風格資訊表。", MessageType.Info);
                    y += LineH * 2 + VPad;
                }
                else
                {
                    // 說話者 / 顯示名稱：唯讀顯示風格表資料
                    EditorGUI.LabelField(new Rect(x, y, w, LineH), "說話者",
                        $"{style.characterName}（顯示名稱：{style.DisplayName}）");
                    y += LineH + VPad;

                    y = DrawColorSwatchRow(new Rect(x, y, w, 0f), style, colorIndexProp);
                    // Sprite 立繪挑選（一律顯示）
                    y = DrawPortraitPicker(new Rect(x, y, w, 0f), style, portraitProp);
                    y = DrawSelectedPreview(new Rect(x, y, w, 0f), portraitProp.objectReferenceValue as Sprite);
                    // Spine 表情下拉（有 Spine 資源時額外顯示；選「不用 Spine」則用上方 Sprite）
                    if (style.HasSpine)
                        y = DrawSpineExpressionDropdown(new Rect(x, y, w, 0f), style,
                            property.FindPropertyRelative("spineExpression"));
                }
            }
            else
            {
                EditorGUI.PropertyField(new Rect(x, y, w, LineH), speakerProp, new GUIContent("說話者", speakerProp.tooltip));
                y += LineH + VPad;
                EditorGUI.PropertyField(new Rect(x, y, w, LineH), displayNameProp, new GUIContent("顯示名稱", displayNameProp.tooltip));
                y += LineH + VPad;
                EditorGUI.PropertyField(new Rect(x, y, w, LineH), customColorProp, new GUIContent("名稱顏色", customColorProp.tooltip));
                y += LineH + VPad;
                EditorGUI.PropertyField(new Rect(x, y, w, LineH), portraitProp, new GUIContent("人物立繪", portraitProp.tooltip));
                y += LineH + VPad;

                y = DrawSelectedPreview(new Rect(x, y, w, 0f), portraitProp.objectReferenceValue as Sprite);
            }

            EditorGUI.indentLevel--;
        }

        // 縮圖尚在非同步載入時持續重繪，避免預覽停在空白
        if (AssetPreview.IsLoadingAssetPreviews()) HandleUtility.Repaint();

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float h = LineH; // 折疊列
        if (!property.isExpanded) return h;

        h += VPad + EditorGUI.GetPropertyHeight(property.FindPropertyRelative("text")) + VPad;
        h += LineH + VPad; // 停頓秒數
        h += LineH + VPad; // 讀取風格表開關

        var portrait = property.FindPropertyRelative("portrait").objectReferenceValue as Sprite;

        if (property.FindPropertyRelative("useCharacterStyle").boolValue)
        {
            h += LineH + VPad; // 風格表欄位
            var style = property.FindPropertyRelative("characterStyle").objectReferenceValue as CharacterStyleData;
            if (style == null)
            {
                h += LineH * 2 + VPad; // HelpBox
            }
            else
            {
                h += LineH + VPad; // 說話者資訊

                int swCount = style.themeColors != null ? style.themeColors.Count : 0;
                int swRows = swCount > 0
                    ? Mathf.CeilToInt((float)swCount / SwatchCols(EstimatedFieldWidth()))
                    : 1;
                h += Mathf.Max(LineH, swRows * (SwatchSize + SwatchPad)) + VPad;

                // Sprite 立繪挑選（一律）
                h += LineH + VPad; // 立繪選擇標籤
                int cellCount = (style.portraits != null ? style.portraits.Count : 0) + 1; // +1 = 「無」
                int rows = Mathf.CeilToInt((float)cellCount / ThumbCols(EstimatedContentWidth()));
                h += rows * (ThumbSize + ThumbPad) + VPad;
                if (portrait != null) h += PreviewSize + VPad;

                // Spine 表情下拉（有 Spine 資源時額外一列；無 Animation 時為提示框）
                if (style.HasSpine)
                    h += (style.GetSpineAnimationNames().Count == 0 ? LineH * 2 : LineH) + VPad;
            }
        }
        else
        {
            h += (LineH + VPad) * 4; // 說話者、顯示名稱、名稱顏色、立繪
            if (portrait != null) h += PreviewSize + VPad;
        }

        return h + 4f;
    }

    #region 區塊繪製

    /// <summary>Spine2D 模式：以下拉選單選立繪表情（Spine Animation 名稱）。回傳下一列的 y。</summary>
    private static float DrawSpineExpressionDropdown(Rect area, CharacterStyleData style, SerializedProperty spineExprProp)
    {
        var names = style.GetSpineAnimationNames();
        float y = area.y;
        if (names.Count == 0)
        {
            EditorGUI.HelpBox(EditorGUI.IndentedRect(new Rect(area.x, y, area.width, LineH * 2)),
                "此 Spine2D 資源沒有可用的 Animation（立繪表情）。", MessageType.Warning);
            return y + LineH * 2 + VPad;
        }

        // 選項：[0]＝「不用 Spine（用上方立繪）」，其後為各 Animation
        var labels = new GUIContent[names.Count + 1];
        labels[0] = new GUIContent("（不用 Spine，用上方立繪）");
        for (int i = 0; i < names.Count; i++) labels[i + 1] = new GUIContent(names[i]);

        string cur = spineExprProp.stringValue;
        int idx = string.IsNullOrEmpty(cur) ? 0 : names.IndexOf(cur) + 1; // 找不到→0（不用 Spine）
        if (idx < 0) idx = 0;

        int sel = EditorGUI.Popup(new Rect(area.x, y, area.width, LineH),
            new GUIContent("立繪表情 (Spine)", "選 Animation＝此行改用 Spine 立繪並播該表情；選「不用 Spine」則用上方立繪。"),
            idx, labels);
        spineExprProp.stringValue = sel == 0 ? "" : names[sel - 1];
        return y + LineH + VPad;
    }

    /// <summary>畫「名稱顏色」列：左為標籤，右為可點選的主題色色塊。回傳下一列的 y。</summary>
    private static float DrawColorSwatchRow(Rect area, CharacterStyleData style, SerializedProperty colorIndexProp)
    {
        EditorGUI.LabelField(new Rect(area.x, area.y, EditorGUIUtility.labelWidth, LineH), "名稱顏色");

        float sx = area.x + EditorGUIUtility.labelWidth + 2f;
        int count = style.themeColors != null ? style.themeColors.Count : 0;
        int cols = SwatchCols(EstimatedFieldWidth());
        int rows = 1;

        if (count == 0)
        {
            EditorGUI.LabelField(new Rect(sx, area.y, area.width - EditorGUIUtility.labelWidth, LineH),
                "（風格表未設定主題色，將以白色顯示）");
        }
        else
        {
            rows = Mathf.CeilToInt((float)count / cols);
            for (int i = 0; i < count; i++)
            {
                var cell = new Rect(
                    sx + (i % cols) * (SwatchSize + SwatchPad),
                    area.y + (i / cols) * (SwatchSize + SwatchPad),
                    SwatchSize, SwatchSize);

                if (GUI.Button(cell, new GUIContent("", $"主題色 {i}"), GUIStyle.none))
                {
                    colorIndexProp.intValue = i;
                }
                if (colorIndexProp.intValue == i)
                {
                    EditorGUI.DrawRect(Expand(cell, 2f), SelectColor);
                }
                var c = style.themeColors[i];
                EditorGUI.DrawRect(cell, new Color(c.r, c.g, c.b, 1f));
            }
        }
        return area.y + Mathf.Max(LineH, rows * (SwatchSize + SwatchPad)) + VPad;
    }

    /// <summary>畫「立繪選擇」標籤與縮圖格；點縮圖即選取（第一格「無」＝清空）。回傳下一列的 y。</summary>
    private static float DrawPortraitPicker(Rect area, CharacterStyleData style, SerializedProperty portraitProp)
    {
        EditorGUI.LabelField(new Rect(area.x, area.y, area.width, LineH), "立繪選擇（點選縮圖）");
        float y = area.y + LineH + VPad;

        var grid = EditorGUI.IndentedRect(new Rect(area.x, y, area.width, 0f));
        int spriteCount = style.portraits != null ? style.portraits.Count : 0;
        int cellCount = spriteCount + 1; // 第一格 = 無
        int cols = ThumbCols(EstimatedContentWidth());
        var current = portraitProp.objectReferenceValue as Sprite;

        for (int i = 0; i < cellCount; i++)
        {
            var cell = new Rect(
                grid.x + (i % cols) * (ThumbSize + ThumbPad),
                y + (i / cols) * (ThumbSize + ThumbPad),
                ThumbSize, ThumbSize);
            Sprite sprite = i == 0 ? null : style.portraits[i - 1];

            if (GUI.Button(cell, GUIContent.none, GUIStyle.none))
            {
                portraitProp.objectReferenceValue = sprite;
            }
            if (current == sprite) // 含「無」（null）的情況
            {
                EditorGUI.DrawRect(Expand(cell, 2f), SelectColor);
            }
            EditorGUI.DrawRect(cell, CellBgColor);

            if (sprite == null)
            {
                GUI.Label(cell, "無", CenteredLabel);
            }
            else
            {
                var tex = GetPreviewTexture(sprite);
                if (tex != null) GUI.DrawTexture(cell, tex, ScaleMode.ScaleToFit);
                else GUI.Label(cell, sprite.name, CenteredLabel);
            }
        }

        int rows = Mathf.CeilToInt((float)cellCount / cols);
        return y + rows * (ThumbSize + ThumbPad) + VPad;
    }


    /// <summary>畫目前選取立繪的放大預覽（未選取立繪時不畫）。回傳下一列的 y。</summary>
    private static float DrawSelectedPreview(Rect area, Sprite sprite)
    {
        if (sprite == null) return area.y;

        var r = EditorGUI.IndentedRect(new Rect(area.x, area.y, area.width, PreviewSize));
        var box = new Rect(r.x, r.y, PreviewSize, PreviewSize);
        EditorGUI.DrawRect(box, CellBgColor);
        var tex = GetPreviewTexture(sprite);
        if (tex != null) GUI.DrawTexture(box, tex, ScaleMode.ScaleToFit);
        GUI.Label(new Rect(box.xMax + 6f, r.y, r.width - PreviewSize - 6f, LineH),
            $"目前立繪：{sprite.name}", EditorStyles.miniLabel);
        return area.y + PreviewSize + VPad;
    }

    #endregion

    #region 輔助

    /// <summary>折疊列標題：顯示名稱＋文本開頭摘要。</summary>
    private static GUIContent BuildHeader(SerializedProperty useStyleProp, SerializedProperty styleProp,
        SerializedProperty speakerProp, SerializedProperty displayNameProp, SerializedProperty textProp)
    {
        string name;
        var style = styleProp.objectReferenceValue as CharacterStyleData;
        if (useStyleProp.boolValue && style != null)
        {
            name = style.DisplayName;
        }
        else if (!string.IsNullOrEmpty(displayNameProp.stringValue))
        {
            name = displayNameProp.stringValue;
        }
        else
        {
            var names = speakerProp.enumDisplayNames;
            int idx = speakerProp.enumValueIndex;
            name = (idx >= 0 && idx < names.Length) ? names[idx] : "?";
        }

        string text = textProp.stringValue ?? string.Empty;
        text = text.Replace("\r", "").Replace("\n", " ");
        if (text.Length > 18) text = text.Substring(0, 18) + "…";
        return new GUIContent($"{name}｜{text}");
    }

    private static Texture GetPreviewTexture(Sprite sprite)
    {
        Texture tex = AssetPreview.GetAssetPreview(sprite);
        if (tex == null) tex = AssetPreview.GetMiniThumbnail(sprite);
        return tex;
    }

    private static Rect Expand(Rect r, float amount) =>
        new Rect(r.x - amount, r.y - amount, r.width + amount * 2f, r.height + amount * 2f);

    // 估算可用寬度：OnGUI 與 GetPropertyHeight 都用同一套估算值計算欄數，確保高度與繪製一致
    private static float EstimatedContentWidth() =>
        Mathf.Max(120f, EditorGUIUtility.currentViewWidth - 120f);

    private static float EstimatedFieldWidth() =>
        Mathf.Max(SwatchSize + SwatchPad, EstimatedContentWidth() - EditorGUIUtility.labelWidth);

    private static int SwatchCols(float width) => Mathf.Max(1, (int)(width / (SwatchSize + SwatchPad)));

    private static int ThumbCols(float width) => Mathf.Max(1, (int)(width / (ThumbSize + ThumbPad)));

    #endregion
}
