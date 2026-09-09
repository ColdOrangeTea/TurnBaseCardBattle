using System;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;
using Spine.Unity;

[CreateAssetMenu(fileName = "SO_BattleEnemyProfile", menuName = "SO/Battle/Create SO_BattleEnemyProfile", order = 1)]
public class SO_BattleEnemyProfile : ScriptableObject
{
    [Header("敵人戰鬥圖像")]
    public List<GameObject> enemyProfile = new List<GameObject>();// 手動掛 List順序對應 EnemyType
    [Header("檢查用數值 目前敵人種類的數量")]
    public int enemyTypeCount;
    public EnemyType type;
    public int typeNum;

    void OnValidate()
    {
        enemyTypeCount = Enum.GetValues(typeof(EnemyType)).Length;
        int bossType = (int)EnemyType._Boss_;
        typeNum = (int)type;
        if (enemyProfile.Count > bossType)
        {
            enemyProfile[bossType] = null;
        }
    }
}
