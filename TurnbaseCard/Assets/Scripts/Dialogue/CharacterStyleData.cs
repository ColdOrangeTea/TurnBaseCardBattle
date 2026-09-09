using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.Dialogue;

/// <summary>
/// 人物風格資訊表：記錄單一人物的對話文字樣式與立繪資訊（ScriptableObject）。
/// 由 Assets 右鍵 → Create → Dialogue → 人物風格資訊表 建立。
///
/// 記錄內容：
///   1. 人物名稱（DialogueUnitType 列舉）與顯示用名稱
///   2. 人物主題色（可多個，第一個為預設；Inspector 點色塊即可開啟選色器）
///   3. 此人物擁有的全部表情立繪
/// DialogueData 的對白行勾選「讀取人物風格資訊表」時，會從這裡取得資料。
/// </summary>
[CreateAssetMenu(fileName = "NewCharacterStyle", menuName = "Dialogue/人物風格資訊表 (CharacterStyleData)")]
public class CharacterStyleData : ScriptableObject
{
    [Tooltip("人物名稱（列舉）。")]
    public DialogueUnitType characterName = DialogueUnitType.Unknown;

    [Tooltip("顯示用名稱。留空則直接顯示 characterName 的列舉名稱。")]
    public string displayName;

    [Tooltip("人物主題色（可多個，第一個為預設）。點色塊可開啟選色器。")]
    public List<Color> themeColors = new List<Color> { Color.white };

    [Tooltip("此人物擁有的全部表情立繪。")]
    public List<Sprite> portraits = new List<Sprite>();

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

    /// <summary>取得指定索引的立繪；越界回傳 null。</summary>
    public Sprite GetPortrait(int index)
    {
        if (portraits == null || index < 0 || index >= portraits.Count) return null;
        return portraits[index];
    }
}
