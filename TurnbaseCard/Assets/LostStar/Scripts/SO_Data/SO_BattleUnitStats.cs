// 此 ScriptableObject 由 A_Good_Ink 使用 AI 生成。
// 用途：集中管理回合制戰鬥中「玩家角色」與「敵人」的原始數值（最大 HP、每回合骰子數），
//       取代原本寫死在 TurnBaseBattlePlayerData.GetInfo() / TurnBaseBattleEnemyData.GetInfo() 的 if-else。
// 資產由 Editor 工具「Tools/TurnBaseBattle/生成戰鬥資料 SO」自動生成，並置於 Resources/Battle 供執行期讀取。
using System;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;

/// <summary>回合制戰鬥單位（玩家角色 / 敵人）的原始數值資料表。</summary>
[CreateAssetMenu(fileName = "BattleUnitStats", menuName = "SO/Battle/Create SO_BattleUnitStats", order = 2)]
public class SO_BattleUnitStats : ScriptableObject
{
    /// <summary>玩家可操作角色的一筆數值。</summary>
    [Serializable]
    public class PlayerStatEntry
    {
        [Tooltip("角色類型")] public CharacterType characterType = CharacterType.Seraphis;
        [Tooltip("最大生命值")] public int maxHp = 16;
        [Tooltip("每回合骰子數量")] public int diceCount = 2;
    }

    /// <summary>敵人的一筆數值。</summary>
    [Serializable]
    public class EnemyStatEntry
    {
        [Tooltip("敵人類型（僅填基礎/薯型敵人；無盡模式的人形薯會先映射回基礎型再查表）")]
        public EnemyType enemyType = EnemyType.Yarn;
        [Tooltip("最大生命值")] public int maxHp = 20;
        [Tooltip("每回合骰子數量")] public int diceCount = 1;
    }

    [Header("玩家角色數值")]
    [Tooltip("依角色類型設定各角色的原始數值")]
    public List<PlayerStatEntry> players = new List<PlayerStatEntry>();

    [Header("敵人數值")]
    [Tooltip("依敵人類型設定各敵人的原始數值")]
    public List<EnemyStatEntry> enemies = new List<EnemyStatEntry>();

    /// <summary>依角色類型取得玩家數值；找不到回傳 false。</summary>
    public bool TryGetPlayer(CharacterType type, out PlayerStatEntry entry)
    {
        if (players != null)
        {
            for (int i = 0; i < players.Count; i++)
            {
                if (players[i] != null && players[i].characterType == type)
                {
                    entry = players[i];
                    return true;
                }
            }
        }
        entry = null;
        return false;
    }

    /// <summary>依敵人類型取得敵人數值；找不到回傳 false。</summary>
    public bool TryGetEnemy(EnemyType type, out EnemyStatEntry entry)
    {
        if (enemies != null)
        {
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i] != null && enemies[i].enemyType == type)
                {
                    entry = enemies[i];
                    return true;
                }
            }
        }
        entry = null;
        return false;
    }
}
