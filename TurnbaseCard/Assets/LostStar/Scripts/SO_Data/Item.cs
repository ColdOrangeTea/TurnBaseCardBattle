using UnityEngine;

public enum ItemEffectType
{
    Heal,        // 恢復生命值
    Buff,        // 增加屬性（實際行為看 buffType）
    Debuff,      // 減少屬性
    Damage,      // 對敵人造成傷害
    Special      // 特殊效果（例如傳送）
}

/// <summary>Buff 子類型（僅當 <see cref="ItemEffectType.Buff"/> 時有效），決定 effectValue 加到哪個屬性。
/// 註：回復現有血量請用 <see cref="ItemEffectType.Heal"/>，不在 Buff 範圍內。</summary>
public enum BuffType
{
    AddMaxDice,  // 增加最大骰子數（+effectValue）
    AddMaxHp     // 增加最大生命值（+effectValue，並同步回復該量現有血量）
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
    [Tooltip("僅當 effectType = Buff 時有效：決定 effectValue 加到哪個屬性")]
    public BuffType buffType;         // Buff 子類型（回血 / 加最大骰 / 加最大血）
    public int effectValue;           // 效果數值（例如恢復多少生命值）
}