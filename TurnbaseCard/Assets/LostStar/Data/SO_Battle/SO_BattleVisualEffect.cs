using System;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_BattleVisualEffect", menuName = "SO/Battle/Create SO_BattleVisualEffect", order = 4)]
public class SO_BattleVisualEffect : ScriptableObject
{
    [Header("狀態戰鬥圖像")]
    public List<Sprite> UseCardEffectSprites = new List<Sprite>();// 手動掛 List順序對應 EnemyType
    public List<Sprite> EffectSprites = new List<Sprite>();// 手動掛 List順序對應 EnemyType
    [Header("檢查用數值 目前狀態種類的數量")]
    public int EffectSpritesCount;
    public BattleStatusEffectType type;
    public CardType cardType;
    public string cardname;

    void OnValidate()
    {
        EffectSpritesCount = Enum.GetValues(typeof(BattleStatusEffectType)).Length;

        cardname = cardType.ToString();
    }
}
