using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using Assets.Scripts.Dialogue;

/// <summary>
/// 對話資訊表：一段對話的完整資料（ScriptableObject）。
/// 由 Assets 右鍵 → Create → Dialogue → 對話資訊表 建立。
///
/// 每行對白可設定：文本、|pause| 停頓，以及人物資訊（說話者、名稱顏色、立繪）。
/// 人物資訊可「讀取人物風格資訊表 (CharacterStyleData)」或「自定義」二選一（useCharacterStyle）。
/// 文本內停頓語法：
///   |pause|      → 停頓（秒數用該行的 pauseDuration，未設定則用打字機的 specialCharDelay）
///   |pause=1.5|  → 停頓指定秒數（此例為 1.5 秒）
/// 文本支援 TMP 富文本標籤，如 &lt;color=red&gt;紅字&lt;/color&gt;。
/// </summary>
[CreateAssetMenu(fileName = "NewDialogueData", menuName = "SO/Dialogue/對話資訊表 (DialogueData)")]
public class DialogueData : ScriptableObject
{
    /// <summary>單行對白的資料。</summary>
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("對白文本。可用 |pause| 或 |pause=秒數| 標記停頓，並支援 TMP 富文本標籤。")]
        [TextArea(2, 5)]
        public string text;

        [Tooltip("此行 |pause| 的預設停頓秒數。0 = 使用打字機全域的 specialCharDelay。")]
        [Min(0)]
        public float pauseDuration = 0f;

        [Tooltip("勾選 = 讀取人物風格資訊表 (CharacterStyleData)；取消 = 自定義（手動填寫人物資料）。")]
        public bool useCharacterStyle = true;

        [Tooltip("人物風格資訊表：提供說話者、顯示名稱、主題色與立繪清單。")]
        public CharacterStyleData characterStyle;

        [Tooltip("採用風格表中第幾個主題色作為人物名稱的顯示顏色。")]
        [Min(0)]
        public int themeColorIndex = 0;

        [Tooltip("說話的人物（Narration = 旁白，會自動隱藏立繪）。（自定義模式）")]
        public DialogueUnitType speaker = DialogueUnitType.Unknown;

        [Tooltip("顯示用名稱。留空則直接顯示 speaker 的列舉名稱。（自定義模式）")]
        public string displayName;

        [Tooltip("人物名稱的顯示顏色。（自定義模式）")]
        public Color customNameColor = Color.white;

        [Tooltip("此行要顯示的人物立繪。讀取風格表時從其立繪清單中點選；自定義時手動指定。")]
        public Sprite portrait;

        [Tooltip("勾選 = 此行使用自訂的立繪位置座標；取消 = 使用立繪 Prefab 原本的位置。")]
        public bool useCustomPortraitPosition = false;

        [Tooltip("立繪的自訂位置（RectTransform 的 anchoredPosition）。")]
        public Vector2 portraitPosition;

        /// <summary>是否實際採用風格表資料（勾選讀取且已指定風格表）。</summary>
        public bool UsesStyle => useCharacterStyle && characterStyle != null;

        /// <summary>取得說話者（風格表模式讀取風格表的人物名稱）。</summary>
        public DialogueUnitType Speaker => UsesStyle ? characterStyle.characterName : speaker;

        /// <summary>取得顯示用名稱（風格表模式讀取風格表；自定義時 displayName 為空退回 speaker 名稱）。</summary>
        public string DisplayName => UsesStyle
            ? characterStyle.DisplayName
            : (string.IsNullOrEmpty(displayName) ? speaker.ToString() : displayName);

        /// <summary>取得人物名稱的顯示顏色（風格表模式依 themeColorIndex 取主題色）。</summary>
        public Color NameColor => UsesStyle ? characterStyle.GetThemeColor(themeColorIndex) : customNameColor;

        /// <summary>取得此行要顯示的人物立繪（可為 null）。</summary>
        public Sprite Portrait => portrait;
    }

    [Tooltip("這段對話的劇情類型。")]
    public DialogueType dialogueType = DialogueType.Other;

    [Tooltip("此段對話的背景圖。留空 = 不顯示背景（背景 Prefab 會自動隱藏）。顯示時會等比放大填滿畫面，超出部分裁切，不會變形或非等比拉伸。")]
    public Sprite backgroundImage;

    [Tooltip("顯示這段對話時使用的 TMP 字型資產（可由 Tools → Dialogue → 字型烘焙 產生）。留空 = 使用對話框原本的字型。")]
    public TMP_FontAsset dialogueFont;

    [Tooltip("對白列表，依序播放。")]
    public List<DialogueLine> lines = new List<DialogueLine>();

    /// <summary>對白總行數。</summary>
    public int LineCount => lines != null ? lines.Count : 0;

    /// <summary>取得指定索引的對白行；越界回傳 null。</summary>
    public DialogueLine GetLine(int index)
    {
        if (lines == null || index < 0 || index >= lines.Count) return null;
        return lines[index];
    }
}
