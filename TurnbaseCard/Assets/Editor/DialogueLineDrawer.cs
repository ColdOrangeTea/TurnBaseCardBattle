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
    private const float PreviewRefW = 1920f;          // CanvasScaler 參考解析度（寬）
    private const float PreviewRefH = 1080f;          // CanvasScaler 參考解析度（高）
    private const float PosPreviewMaxH = 140f;        // 示意圖最大高度
    private static readonly Vector2 DefaultPortraitSize = new Vector2(500f, 700f); // 無立繪時的示意大小（同 Prefab 預設）

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
                    y = DrawPortraitPicker(new Rect(x, y, w, 0f), style, portraitProp);
                    y = DrawSelectedPreview(new Rect(x, y, w, 0f), portraitProp.objectReferenceValue as Sprite);
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

            // ---- 立繪位置（兩種模式共用）----
            var usePosProp = property.FindPropertyRelative("useCustomPortraitPosition");
            var posProp = property.FindPropertyRelative("portraitPosition");
            EditorGUI.PropertyField(new Rect(x, y, w, LineH), usePosProp,
                new GUIContent("自訂立繪位置", usePosProp.tooltip));
            y += LineH + VPad;
            if (usePosProp.boolValue)
            {
                float posH = EditorGUI.GetPropertyHeight(posProp);
                EditorGUI.PropertyField(new Rect(x, y, w, posH), posProp,
                    new GUIContent("立繪位置", posProp.tooltip));
                y += posH + VPad;

                y = DrawPositionPreview(new Rect(x, y, w, 0f), posProp,
                    portraitProp.objectReferenceValue as Sprite);
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

                h += LineH + VPad; // 立繪選擇標籤
                int cellCount = (style.portraits != null ? style.portraits.Count : 0) + 1; // +1 = 「無」
                int rows = Mathf.CeilToInt((float)cellCount / ThumbCols(EstimatedContentWidth()));
                h += rows * (ThumbSize + ThumbPad) + VPad;

                if (portrait != null) h += PreviewSize + VPad;
            }
        }
        else
        {
            h += (LineH + VPad) * 4; // 說話者、顯示名稱、名稱顏色、立繪
            if (portrait != null) h += PreviewSize + VPad;
        }

        // 立繪位置（兩種模式共用）
        h += LineH + VPad; // 自訂立繪位置開關
        if (property.FindPropertyRelative("useCustomPortraitPosition").boolValue)
        {
            h += EditorGUI.GetPropertyHeight(property.FindPropertyRelative("portraitPosition")) + VPad;

            // 座標位置預覽（示意圖 + 提示文字）
            GetPreviewCanvasSize(EstimatedContentWidth(), out _, out float canvasH);
            h += canvasH + VPad + LineH + VPad;
        }

        return h + 4f;
    }

    #region 區塊繪製

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

    /// <summary>
    /// 畫「立繪位置預覽」：以 Canvas 參考解析度（1920×1080）縮小繪製畫面示意圖，
    /// 把立繪按座標畫在對應位置（座標基準與立繪 Prefab 相同：錨點 / pivot = 畫面底部中央）。
    /// 可直接在示意圖上點擊 / 拖曳設定座標（點擊處 = 立繪底部中央的落點）。
    /// 回傳下一列的 y。
    /// </summary>
    private static float DrawPositionPreview(Rect area, SerializedProperty posProp, Sprite sprite)
    {
        var indented = EditorGUI.IndentedRect(new Rect(area.x, area.y, area.width, 0f));
        GetPreviewCanvasSize(indented.width, out float canvasW, out float canvasH);
        var canvasRect = new Rect(indented.x, area.y, canvasW, canvasH);
        float k = canvasW / PreviewRefW; // 參考解析度 → 示意圖的縮放比

        // ---- 點擊 / 拖曳設定座標（先處理輸入，讓拖曳中即時反映）----
        var e = Event.current;
        if ((e.type == EventType.MouseDown || e.type == EventType.MouseDrag) &&
            canvasRect.Contains(e.mousePosition))
        {
            float ax = (e.mousePosition.x - (canvasRect.x + canvasW / 2f)) / k; // 相對底部中央的 X
            float ay = (canvasRect.yMax - e.mousePosition.y) / k;               // 相對底部的 Y
            posProp.vector2Value = new Vector2(Mathf.Round(ax), Mathf.Round(ay));
            GUI.changed = true;
            e.Use();
        }

        // ---- 畫面示意圖 ----
        EditorGUI.DrawRect(Expand(canvasRect, 1f), new Color(0.5f, 0.5f, 0.5f, 0.8f)); // 邊框
        EditorGUI.DrawRect(canvasRect, new Color(0.12f, 0.12f, 0.15f, 1f));            // 畫面底色

        GUI.BeginGroup(canvasRect); // 之後用示意圖內的區域座標，超出部分自動裁切

        // 對話框示意（同產生的 Prefab：底部、高 260、離底 86）
        var panelRect = new Rect(40f * k, canvasH - (86f + 260f) * k, canvasW - 80f * k, 260f * k);
        EditorGUI.DrawRect(panelRect, new Color(1f, 1f, 1f, 0.10f));

        // 立繪矩形：pivot = 底部中央；大小用 Sprite 原始尺寸（無立繪時用 Prefab 預設大小示意）
        Vector2 pos = posProp.vector2Value;
        Vector2 size = sprite != null ? sprite.rect.size : DefaultPortraitSize;
        var portraitRect = new Rect(
            canvasW / 2f + (pos.x - size.x / 2f) * k,
            canvasH - (pos.y + size.y) * k,
            size.x * k, size.y * k);

        var tex = sprite != null ? GetPreviewTexture(sprite) : null;
        if (tex != null)
        {
            GUI.DrawTexture(portraitRect, tex, ScaleMode.ScaleToFit);
        }
        else
        {
            EditorGUI.DrawRect(portraitRect, new Color(0.35f, 0.6f, 0.9f, 0.35f)); // 無圖時的半透明示意框
        }
        EditorGUI.DrawRect(new Rect(portraitRect.x, portraitRect.y, portraitRect.width, 1f), SelectColor);
        EditorGUI.DrawRect(new Rect(portraitRect.x, portraitRect.yMax - 1f, portraitRect.width, 1f), SelectColor);
        EditorGUI.DrawRect(new Rect(portraitRect.x, portraitRect.y, 1f, portraitRect.height), SelectColor);
        EditorGUI.DrawRect(new Rect(portraitRect.xMax - 1f, portraitRect.y, 1f, portraitRect.height), SelectColor);

        // 落點標記（立繪底部中央 = 座標所在位置）
        var anchorPoint = new Vector2(canvasW / 2f + pos.x * k, canvasH - pos.y * k);
        EditorGUI.DrawRect(new Rect(anchorPoint.x - 4f, anchorPoint.y - 1f, 8f, 2f), Color.yellow);
        EditorGUI.DrawRect(new Rect(anchorPoint.x - 1f, anchorPoint.y - 4f, 2f, 8f), Color.yellow);

        GUI.EndGroup();

        // 提示文字
        float y = area.y + canvasH + VPad;
        GUI.Label(new Rect(indented.x, y, indented.width, LineH),
            $"（基準 {PreviewRefW:0}×{PreviewRefH:0}，原點 = 畫面底部中央；可直接在圖上點擊 / 拖曳指定落點）",
            EditorStyles.miniLabel);
        return y + LineH + VPad;
    }

    /// <summary>計算示意圖的實際大小：維持參考解析度比例，高度上限 PosPreviewMaxH、寬度不超過可用寬。</summary>
    private static void GetPreviewCanvasSize(float availWidth, out float w, out float h)
    {
        h = PosPreviewMaxH;
        w = h * (PreviewRefW / PreviewRefH);
        if (w > availWidth)
        {
            w = Mathf.Max(80f, availWidth);
            h = w * (PreviewRefH / PreviewRefW);
        }
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
