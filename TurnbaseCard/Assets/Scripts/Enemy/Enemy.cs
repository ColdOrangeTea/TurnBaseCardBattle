using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;

public class Enemy : MonoBehaviour
{
    public EnemyType enemyType; // 敵人類型

    // 這個方法可以用來初始化敵人的屬性
    public void InitializeEnemy(EnemyType type)
    {
        enemyType = type; // 設定敵人的類型

        // 根據敵人的類型調整屬性
        switch (enemyType)
        {
            case EnemyType.Yarn:

                break;

            case EnemyType.Boy:

                break;

            case EnemyType.Swordsman:

                break;

            case EnemyType.Nun:

                break;

            case EnemyType.Preacher:

                break;

            case EnemyType._Boss_:

                break;

            case EnemyType.Godness:

                break;

            default:
                Debug.LogWarning("未定義的敵人類型，使用預設值。");

                break;
        }
    }


}
