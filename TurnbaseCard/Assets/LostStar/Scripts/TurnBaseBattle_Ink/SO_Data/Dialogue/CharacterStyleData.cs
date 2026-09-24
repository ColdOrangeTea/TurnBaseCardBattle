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
///   3. 此人物的立繪：二選一（<see cref="useSpine2D"/>）
///      - 取消：用 <see cref="portraits"/> Sprite 清單列出各表情立繪（現行作法）
///      - 勾選：用 Spine2D 立繪資源（<see cref="spinePortrait"/>），表情＝其 Animation 名稱
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

    [Header("立繪")]
    [Tooltip("勾選 = 使用 Spine2D 立繪資源（表情＝Spine 的 Animation 名稱）；取消 = 使用下方的立繪 Sprite 清單。")]
    public bool useSpine2D = false;

    [Tooltip("此人物擁有的全部表情立繪（Sprite 模式；取消 useSpine2D 時使用）。")]
    public List<Sprite> portraits = new List<Sprite>();

    [Tooltip("Spine2D 立繪資源（useSpine2D 勾選時使用）。表情由其 Animation 名稱提供。")]
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

    /// <summary>目前是否採用 Spine2D 立繪（勾選 useSpine2D 且已指定資源）。</summary>
    public bool UsesSpine => useSpine2D && spinePortrait != null;

    /// <summary>
    /// 取得 Spine2D 立繪資源的所有 Animation 名稱（＝立繪表情）。
    /// 未使用 Spine2D、未指定資源或載入失敗時回傳空清單。
    /// </summary>
    public List<string> GetSpineAnimationNames()
    {
        var names = new List<string>();
        if (!UsesSpine) return names;

        var data = spinePortrait.GetSkeletonData(true); // quiet：取不到不洗 Console
        if (data == null || data.Animations == null) return names;

        foreach (var anim in data.Animations)
            if (anim != null && !string.IsNullOrEmpty(anim.Name))
                names.Add(anim.Name);
        return names;
    }
}
