using UnityEngine;

/// <summary>
/// 道具管理：查詢道具（隨機/依名）、把獎勵道具加入背包。由 A_Good_Ink 使用 AI 生成/重構。
/// 道具增減一律走跨場景中樞 <see cref="LevelMapInitializer"/>（單一真相源）；
/// 買賣統一由 <see cref="ShopSystem"/> 處理，本類別專責「取得道具/發放獎勵」。
/// </summary>
public class ItemManager : MonoBehaviour
{
    private static LevelMapInitializer Hub => LevelMapInitializer.Instance;

    [Tooltip("道具資料庫；留空會嘗試 Resources.Load(\"Item/ShopItemDatabase\")。")]
    public ItemDatabase itemDatabase;

    private void Awake()
    {
        if (itemDatabase == null) itemDatabase = Resources.Load<ItemDatabase>("Item/ShopItemDatabase");
    }

    // 取得隨機道具
    public Item GetRandomItem() => itemDatabase != null ? itemDatabase.GetRandomItem() : null;

    // 取得特定名稱的道具
    public Item GetItemByName(string itemName) => itemDatabase != null ? itemDatabase.GetItemByName(itemName) : null;

    /// <summary>把道具加入背包（走中樞；滿則中樞觸發 ItemDiscarded → 提示放棄）。回傳是否成功入袋。</summary>
    public bool AddItemToInventory(Item item)
    {
        if (item == null) return false;
        if (Hub == null) { Debug.LogWarning("[ItemManager] 找不到 LevelMapInitializer，無法加入道具"); return false; }
        bool ok = Hub.AddItem(item);
        Debug.Log(ok ? $"加入道具到背包：{item.itemName}" : $"背包已滿，放棄道具：{item.itemName}");
        return ok;
    }

    // 開寶箱獲得隨機道具
    public void OpenTreasureChest()
    {
        Item reward = GetRandomItem();
        if (reward != null) AddItemToInventory(reward);
    }

    // 隨機事件獲得道具
    public void TriggerEventReward()
    {
        Item reward = GetRandomItem();
        if (reward != null) AddItemToInventory(reward);
    }
}
