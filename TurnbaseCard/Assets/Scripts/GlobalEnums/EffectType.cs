namespace Assets.Scripts.GlobalEnums
{
    /// <summary>
    /// 遊戲效果的類型（金幣、血量、卡片的增減）。
    /// </summary>
    public enum EffectType
    {
        AddGold,
        ReduceHealth,
        AddHealth,
        ReduceGold,
        AddCard,
        ReduceCard
    }

    /// <summary>
    /// 單一效果的資料：類型 + 數值。
    /// </summary>
    [System.Serializable]
    public class Effect
    {
        public EffectType effectType; // 效果類型，例如加金幣、扣血量等
        public int amount;            // 效果數值
    }
}
