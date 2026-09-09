using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;
using System.Collections.Generic;
public class TurnBaseEnemyBehavior
{
    // 定義每個敵人類型及其行為
    private static readonly Dictionary<EnemyType, Dictionary<int, CardType>> enemyActions = new Dictionary<EnemyType, Dictionary<int, CardType>>()
    {
        { EnemyType.Yarn, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},
           { EnemyType.Boy, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.StarThreaten },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},
          { EnemyType.Swordsman, new Dictionary<int, CardType>() {
            { 1, CardType.Poisoned },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }

        }},
        { EnemyType.Nun, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.HolyProtect },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.Preacher, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Burnt },
            { 6, CardType.Attack }
        }},

        { EnemyType.Godness, new Dictionary<int, CardType>() {
            { 1, CardType.Poisoned },
            { 2, CardType.HolyProtect },
            { 3, CardType.StarThreaten },
            { 4, CardType.Oath },
            { 5, CardType.Burnt },
            { 6, CardType.Purify }
        }},


        // 無盡模式的沒共用故事模式的機器薯
           { EnemyType.PotatoAlpha, new Dictionary<int, CardType>() {
           { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoBeta, new Dictionary<int, CardType>() {
             { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoGamma, new Dictionary<int, CardType>() {
           { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoDelta, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoDigamma, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoOmega, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoKappa, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoVex, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        { EnemyType.PotatoPowder, new Dictionary<int, CardType>() {
            { 1, CardType.Attack },
            { 2, CardType.Attack },
            { 3, CardType.Attack },
            { 4, CardType.Attack },
            { 5, CardType.Attack },
            { 6, CardType.Attack }
        }},

        // 可以為其他敵人類型添加更多行為（含無盡模式人形薯 Potato*，需要時再補上對應行為表）
    };

    // 獲取敵人行為的方法
    public static CardType GetEnemyAction(EnemyType enemyType, int diceValue)
    {
        if (enemyActions.TryGetValue(enemyType, out var actions))
        {
            if (actions.TryGetValue(diceValue, out var action))
            {
                return action;
            }
        }
        Debug.LogWarning($"未定義的敵人類型或錯誤的骰子點數: {enemyType}, {diceValue}");
        return CardType.Undefined;
    }
}