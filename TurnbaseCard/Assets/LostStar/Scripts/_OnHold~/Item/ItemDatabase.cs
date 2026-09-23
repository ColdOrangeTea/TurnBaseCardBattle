using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "SO/Inventory/SO_ItemDatabase")]
public class ItemDatabase : ScriptableObject
{
    public List<Item> items; // �x�s�Ҧ��i�Ϊ����~

    // ����H�����~
    public Item GetRandomItem()
    {
        if (items.Count == 0) return null;
        int randomIndex = Random.Range(0, items.Count);
        return items[randomIndex];
    }

    // �ھڦW�٬d�䪫�~
    public Item GetItemByName(string itemName)
    {
        return items.Find(item => item.itemName == itemName);
    }
}