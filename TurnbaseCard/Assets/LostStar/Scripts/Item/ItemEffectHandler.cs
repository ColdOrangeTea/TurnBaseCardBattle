using UnityEngine;

/// <summary>
/// 道具效果處理器：依 <see cref="Item.effectType"/> 觸發對應效果（回血／增益／減益／傷害／特殊）。
/// 回血會作用到地圖角色狀態 <see cref="PlayerMapStatus_UI"/>（一般 LevelMap 節點探索）。
/// </summary>
public class ItemEffectHandler : MonoBehaviour
{
    public PlayerMapStatus_UI playerMapStatus;

    // 依道具類型觸發對應的效果
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
                Debug.LogWarning($"未知的道具效果類型：{item.effectType}");
                break;
        }
    }


    // 恢復生命值的效果
    private void ApplyHealEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 恢復了 {item.effectValue} 點生命值！");

        if (playerMapStatus != null)
        {
            playerMapStatus.playerDataInMap.currentHp += item.effectValue;
            playerMapStatus.UpdateUI(playerMapStatus.playerDataInMap);
        }
    }

    // 增加屬性的效果
    private void ApplyBuffEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 增加了 {item.effectValue} 點屬性！");
        // 這裡可呼叫角色的屬性增益方法
        // 例如：PlayerStats.Instance.IncreaseStats(item.effectValue);
    }

    // 降低屬性的效果
    private void ApplyDebuffEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 減少了 {item.effectValue} 點屬性！");
        // 這裡可呼叫敵人的屬性減益方法
        // 例如：EnemyStats.Instance.DecreaseStats(item.effectValue);
    }

    // 造成傷害的效果
    private void ApplyDamageEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 對敵人造成了 {item.effectValue} 點傷害！");
        // 這裡可呼叫敵人的受傷方法
        // 例如：EnemyHealth.Instance.TakeDamage(item.effectValue);
    }

    // 特殊效果
    private void ApplySpecialEffect(Item item)
    {
        Debug.Log($"使用了特殊道具：{item.itemName}！");
        // 在這裡定義特殊效果
        // 例如：PlayerMovement.Instance.TeleportToSpecialLocation();
    }
}
