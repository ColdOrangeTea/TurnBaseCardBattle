// 此 ScriptableObject 由 A_Good_Ink 使用 AI 生成。
// 用途：一張卡片的完整資料（種類 + 呈現 + 行為）。取代原本寫死在 BattleCard.SendCardInfo() 的大 switch，
//       以及每張卡各做一個 prefab 的做法——戰鬥抽卡時把本 SO 傳給 CardData 即可生成不同卡片。
// 資產由 Editor 工具「Tools/TurnBaseBattle/生成卡片資料 SO」生成，置於 Resources/SO_Battle/Cards 供執行期讀取。
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;

/// <summary>單張卡片的資料表：種類、呈現（名稱/敘述/卡面圖/使用音效）與行為（骰數條件、狀態效果、分類）。</summary>
[CreateAssetMenu(fileName = "Card_", menuName = "SO/Battle/Create SO_CardData", order = 3)]
public class SO_CardData : ScriptableObject
{
    [Header("種類")]
    [Tooltip("卡片型別（對應 CardType；戰鬥效果計算以此為鍵）")]
    public CardType cardType = CardType.Undefined;

    [Header("呈現")]
    [Tooltip("英文/內部名稱")] public string cardName;
    [Tooltip("中文卡名")] public string tw_CardName;
    [Tooltip("中文敘述")] [TextArea] public string tw_Description;
    [Tooltip("卡面圖（可留空＝沿用卡片 prefab 的預設卡框）")] public Sprite cardArt;
    [Tooltip("使用此卡時播放的音效（一律經 AudioDirector 播）")] public AudioClip useSfx;

    [Header("骰數條件 - 累積")]
    [Tooltip("須累積骰子點數才可觸發")] public bool requiredAccumulatedDiceValue;
    [Tooltip("須累積的數值")] public int accu_DiceValue;

    [Header("骰數條件 - 指定數值")]
    [Tooltip("需要指定的骰子數值")] public bool requireDesignatedDiceValue;
    public int desi_DiceValue;
    public bool isEqualTo;
    public bool isGreaterThan;
    public bool isLessThan;

    [Header("骰數條件 - 偶奇")]
    [Tooltip("需要奇數")] public bool requiredOddDiceValue;
    [Tooltip("需要偶數")] public bool requiredEvenDiceValue;

    [Header("狀態效果")]
    [Tooltip("是否附加狀態")] public bool isAddEffectStatus;
    public BattleStatusEffectType effectType = BattleStatusEffectType.None;

    [Header("分類")]
    [Tooltip("攻擊類")] public bool isUsedToAttack;
    [Tooltip("效果類（需搭配 functionalType）")] public bool isFunctional;
    public BattleFunctionalCardType functionalType = BattleFunctionalCardType.None;

    [Header("直接數值")]
    [Tooltip("不經傷害計算、直接造成的數值")]
    public List<int> trueValues = new List<int>() { 0 };

    /// <summary>轉成戰鬥流程使用的 BattleCardInfo（與 BattleCard.SendCardInfo 相容）。</summary>
    public BattleCardInfo ToBattleCardInfo()
    {
        return new BattleCardInfo(
            cardType,
            trueValues != null && trueValues.Count > 0 ? new List<int>(trueValues) : new List<int>() { 0 },
            requiredAccumulatedDiceValue, accu_DiceValue,
            requireDesignatedDiceValue, desi_DiceValue, isEqualTo, isGreaterThan, isLessThan,
            requiredOddDiceValue, requiredEvenDiceValue,
            isAddEffectStatus, effectType,
            isUsedToAttack, isFunctional, functionalType);
    }
}
