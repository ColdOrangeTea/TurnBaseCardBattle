using UnityEngine;

public enum ItemEffectType
{
    Heal,        // 恢復生命值
    Buff,        // 增加屬性
    Debuff,      // 減少屬性
    Damage,      // 對敵人造成傷害
    Special      // 特殊效果（例如傳送）
}

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/SO_Item")]
public class Item : ScriptableObject
{
    public string itemName;     // 物品名稱
    public string description;  // 物品描述
    public Sprite icon;         // 物品圖示
    public int value;           // 物品價值（可用於商店價格）
    public int Sellvalue;           // 販賣物品價值
    public ItemEffectType effectType; // 效果類型
    public int effectValue;           // 效果數值（例如恢復多少生命值）
}