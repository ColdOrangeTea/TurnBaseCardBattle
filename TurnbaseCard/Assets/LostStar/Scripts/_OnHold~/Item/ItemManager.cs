using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemManager : MonoBehaviour
{
    public ItemDatabase itemDatabase; // 物品數據庫
    public PlayerInventory playerInventory; // 玩家背包（存放購買物品）


    // 獲取隨機物品
    public Item GetRandomItem()
    {
        return itemDatabase.GetRandomItem();
    }

    // 獲取特定名稱的物品
    public Item GetItemByName(string itemName)
    {
        return itemDatabase.GetItemByName(itemName);
    }

    // 添加物品到玩家背包（需實現玩家背包系統）
    public void AddItemToInventory(Item item)
    {
        Debug.Log("添加物品到玩家背包: " + item.itemName);
        // TODO: 將物品實際添加到玩家的背包
    }

    // 從商店購買物品
    public void BuyItem(Item item)
    {
        if (playerInventory.gold >= item.value) // 檢查玩家金幣是否足夠
        {
            playerInventory.AddItem(item);  // 將物品添加到背包
            playerInventory.gold -= item.value; // 扣除玩家金幣
            Debug.Log("購買了物品: " + item.itemName);
  
        }
        else
        {
            // TODO: 保留空間來觸發“金幣不足”對話
            Debug.Log("金幣不足，無法購買 " + item.itemName);
        }
    }

    // 打開寶箱獲得物品
    public void OpenTreasureChest()
    {
        Item reward = GetRandomItem();
        if (reward != null)
        {
            Debug.Log("從寶箱中獲得物品: " + reward.itemName);
            AddItemToInventory(reward);
        }
    }

    // 從隨機事件獲得物品
    public void TriggerEventReward()
    {
        Item reward = GetRandomItem();
        if (reward != null)
        {
            Debug.Log("事件獲得獎勵: " + reward.itemName);
            AddItemToInventory(reward);
        }
    }
}
