using UnityEngine;

public class PlayerStatsManager : MonoBehaviour
{

    public int health;
    private PlayerInventory inventory;

    private void Start()
    {
        // 使用 FindWithTag 找到標記為 "Bag" 的物件，並將其轉型為 PlayerInventory 類型
        GameObject inventoryObject = GameObject.FindWithTag("Bag");
        if (inventoryObject != null)
        {
            inventory = inventoryObject.GetComponent<PlayerInventory>();
        }
        else
        {
            Debug.LogWarning("未找到標記為 'Bag' 的 PlayerInventory 物件");
        }
    }
    public void ModifyGold(int amount)
    {
        if (inventory != null)
        {
            inventory.gold += amount;       // 修改背包中的金幣
            inventory.UpdateGoldText();     // 更新金幣顯示
            Debug.Log("玩家的金幣變化: " + amount + "，當前金幣總數: " + inventory.gold);
        }
        else
        {
            Debug.LogWarning("PlayerInventory 未初始化，無法修改金幣");
        }
    }

    public void ModifyHealth(int amount)
    {
        health += amount;
        Debug.Log("玩家的血量變化: " + amount + "，當前血量: " + health);
    }
}
