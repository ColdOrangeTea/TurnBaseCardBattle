using UnityEngine;

public class ItemEffectHandler : MonoBehaviour
{
    public TurnBaseBattleManager battleManager;
    public PlayerMapStatus_UI_EndLess PlayerMapStatus_UI_EndLess;
    public PlayerMapStatus_UI playerMapStatus;

    // 這個方法接收物品並觸發對應的效果
    public void TriggerEffect(Item item)
    {
        switch (item.effectType)
        {
            case ItemEffectType.Heal:
                ApplyHealEffect(item);
                break;

            case ItemEffectType.Buff:
                ApplyBuffEffect(item);
                break;

            case ItemEffectType.Debuff:
                ApplyDebuffEffect(item);
                break;

            case ItemEffectType.Damage:
                ApplyDamageEffect(item);
                break;

            case ItemEffectType.Special:
                ApplySpecialEffect(item);
                break;

            default:
                Debug.LogWarning($"未知的物品效果類型：{item.effectType}");
                break;
        }
    }


    // 恢復生命值的效果
    private void ApplyHealEffect(Item item)
    {
        Debug.Log($"使用物品：{item.itemName} 恢復了 {item.effectValue} 點生命值！");

        // playerData.currentHp += item.effectValue; // 恢復生命值playerData.GetCurrentHp() + (item.effectValue);

        if (PlayerMapStatus_UI_EndLess != null)
        {
            PlayerMapStatus_UI_EndLess.playerDataInMap.currentHp += item.effectValue;
            PlayerMapStatus_UI_EndLess.UpdateUI(PlayerMapStatus_UI_EndLess.playerDataInMap);
        }
        if (playerMapStatus != null)
        {
            playerMapStatus.playerDataInMap.currentHp += item.effectValue;
            playerMapStatus.UpdateUI(playerMapStatus.playerDataInMap);
        }


        // 這裡調用角色的恢復生命值方法
        // 例如：PlayerHealth.Instance.Heal(item.effectValue);
    }

    // 增加屬性的效果
    private void ApplyBuffEffect(Item item)
    {
        Debug.Log($"使用物品：{item.itemName} 增加了 {item.effectValue} 點屬性！");
        // 這裡調用角色的屬性增強方法
        // 例如：PlayerStats.Instance.IncreaseStats(item.effectValue);
    }

    // 減少屬性的效果
    private void ApplyDebuffEffect(Item item)
    {
        Debug.Log($"使用物品：{item.itemName} 減少了 {item.effectValue} 點屬性！");
        // 這裡調用敵人的屬性減弱方法
        // 例如：EnemyStats.Instance.DecreaseStats(item.effectValue);
    }

    // 造成傷害的效果
    private void ApplyDamageEffect(Item item)
    {
        Debug.Log($"使用物品：{item.itemName} 對敵人造成了 {item.effectValue} 點傷害！");
        // 這裡調用敵人受傷的方法
        // 例如：EnemyHealth.Instance.TakeDamage(item.effectValue);
    }

    // 特殊效果
    private void ApplySpecialEffect(Item item)
    {
        Debug.Log($"使用了特殊物品：{item.itemName}！");
        // 在這裡定義特殊效果
        // 例如：PlayerMovement.Instance.TeleportToSpecialLocation();
    }
}
