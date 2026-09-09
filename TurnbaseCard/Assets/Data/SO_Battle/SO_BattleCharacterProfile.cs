using System;
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;
using Spine.Unity;

[CreateAssetMenu(fileName = "SO_BattleCharacterProfile", menuName = "SO/Battle/Create SO_BattleCharacterProfile", order = 0)]
public class SO_BattleCharacterProfile : ScriptableObject
{
    [Header("人物戰鬥圖像")]
    public List<GameObject> characterProfile = new List<GameObject>();// 手動掛 List順序對應 EnemyType
    [Header("檢查用數值 目前人物種類的數量")]
    public int characterTypeCount;
    public CharacterType type;
    void OnValidate()
    {
        characterTypeCount = Enum.GetValues(typeof(CharacterType)).Length;
        int characterType = (int)CharacterType.Enemy;
        if (characterProfile.Count > characterType)
        {
            characterProfile[characterType] = null;
        }
    }
}
