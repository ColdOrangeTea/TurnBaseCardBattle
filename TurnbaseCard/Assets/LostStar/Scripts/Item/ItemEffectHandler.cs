using UnityEngine;

/// <summary>
/// 道具效果處理器：依 <see cref="Item.effectType"/> 觸發對應效果（回血／增益／減益／傷害／特殊）。
/// 效果一律作用到跨場景中樞 <see cref="LevelMapInitializer"/>（HP 等），由它的事件驅動各 UI 更新。
/// 背包只允許使用 Heal / Buff 類型（見 PlayerInventory）。
/// </summary>
public class ItemEffectHandler : MonoBehaviour
{
    private static LevelMapInitializer Hub => LevelMapInitializer.Instance;

    // 這個方法會依道具類型觸發對應的效果
    public void TriggerEffect(Item item)
    {
        if (item == null) return;
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

    // 恢復生命值：走中樞 ChangeHp（觸發 HpChanged → 狀態 UI 自動更新）
    private void ApplyHealEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 恢復了 {item.effectValue} 點生命值！");
        if (Hub != null) Hub.ChangeHp(item.effectValue);
        else Debug.LogWarning("[ItemEffectHandler] 找不到 LevelMapInitializer，無法回血");
    }

    // 增加屬性（暫置占位；仍可被使用/消耗）
    private void ApplyBuffEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 增加了 {item.effectValue} 點屬性！（Buff 效果待實作）");
        // TODO: 接玩家屬性系統後套用增益
    }

    // 降低屬性（占位；一般不由背包使用）
    private void ApplyDebuffEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 減少了 {item.effectValue} 點屬性！（Debuff 效果待實作）");
    }

    // 造成傷害（占位；一般用於戰鬥而非背包）
    private void ApplyDamageEffect(Item item)
    {
        Debug.Log($"使用道具：{item.itemName} 對敵人造成了 {item.effectValue} 點傷害！（Damage 效果待實作）");
    }

    // 特殊效果（占位；通關 Boss 取得，用途另訂）
    private void ApplySpecialEffect(Item item)
    {
        Debug.Log($"使用了特殊道具：{item.itemName}！（Special 效果待實作）");
    }
}
