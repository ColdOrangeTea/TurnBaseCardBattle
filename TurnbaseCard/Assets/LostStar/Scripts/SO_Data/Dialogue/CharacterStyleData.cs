using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Dialogue;
using Spine.Unity;

/// <summary>
/// 人物風格資訊表：記錄單一人物的對話文字樣式與立繪資訊（ScriptableObject）。
/// 由 Assets 右鍵 → Create → SO/Dialogue/人物風格資訊表 建立。
///
/// 記錄內容：
///   1. 人物名稱（DialogueUnitType 列舉）與顯示用名稱
///   2. 人物主題色（可多個，第一個為預設；Inspector 點色塊即可開啟選色器）
///   3. 此人物的立繪：<see cref="portraits"/>（Sprite 清單）與 <see cref="spinePortrait"/>
///      （Spine2D 立繪，表情＝其 Animation 名稱）可並存，由每行對白各自選要用哪個。
/// DialogueData 的對白行勾選「讀取人物風格資訊表」時，會從這裡取得資料。
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterStyle", menuName = "SO/Dialogue/人物風格資訊表 (CharacterStyleData)")]
public class CharacterStyleData : ScriptableObject
{
    [Tooltip("人物名稱（列舉）。")]
    public DialogueUnitType characterName = DialogueUnitType.Unknown;

    [Tooltip("顯示用名稱。留空則直接顯示 characterName 的列舉名稱。")]
    public string displayName;

    [Tooltip("人物主題色（可多個，第一個為預設）。點色塊可開啟選色器。")]
    public List<Color> themeColors = new List<Color> { Color.white };

    // [Header("立繪（Sprite 與 Spine2D 可並存，由每行對白各自選用）")]
    [Tooltip("此人物的表情立繪（Sprite）。對白行可從這裡挑一張。")]
    public List<Sprite> portraits = new List<Sprite>();

    [Tooltip("此人物的 Spine2D 立繪資源（可空）。對白行可改選其 Animation 名稱當表情。")]
    public SkeletonDataAsset spinePortrait;

    /// <summary>取得顯示用名稱（displayName 為空時退回 characterName 名稱）。</summary>
    public string DisplayName =>
        string.IsNullOrEmpty(displayName) ? characterName.ToString() : displayName;

    /// <summary>
    /// 取得指定索引的主題色。
    /// 索引越界時退回第一個主題色；完全沒設定時退回白色。
    /// </summary>
    public Color GetThemeColor(int index)
    {
        if (themeColors == null || themeColors.Count == 0) return Color.white;
        if (index < 0 || index >= themeColors.Count) return themeColors[0];
        return themeColors[index];
    }

    /// <summary>取得指定索引的立繪；越界回傳 null。（Sprite 模式）</summary>
    public Sprite GetPortrait(int index)
    {
        if (portraits == null || index < 0 || index >= portraits.Count) return null;
        return portraits[index];
    }

    /// <summary>是否有可用的 Spine2D 立繪資源（已指定 spinePortrait）。</summary>
    public bool HasSpine => spinePortrait != null;

    /// <summary>
    /// 取得 Spine2D 立繪資源的所有 Animation 名稱（＝立繪表情）。
    /// 未指定資源或載入失敗時回傳空清單。
    /// </summary>
    public List<string> GetSpineAnimationNames()
    {
        var names = new List<string>();
        if (!HasSpine) return names;

        var data = spinePortrait.GetSkeletonData(true); // quiet：取不到不洗 Console
        if (data == null || data.Animations == null) return names;

        foreach (var anim in data.Animations)
            if (anim != null && !string.IsNullOrEmpty(anim.Name))
                names.Add(anim.Name);
        return names;
    }
}
