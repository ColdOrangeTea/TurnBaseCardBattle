using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/SO_ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<Item> items; // 儲存所有可用的物品

    // 獲取隨機物品
    public Item GetRandomItem()
    {
        if (items.Count == 0) return null;
        int randomIndex = Random.Range(0, items.Count);
        return items[randomIndex];
    }

    // 根據名稱查找物品
    public Item GetItemByName(string itemName)
    {
        return items.Find(item => item.itemName == itemName);
    }
}