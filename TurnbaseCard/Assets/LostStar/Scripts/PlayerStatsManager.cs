using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家屬性與持有物的場景橋接（由 A_Good_Ink 使用 AI 生成/重構）。
///
/// 真正的資料（金幣 / HP / 道具背包）由跨場景常駐的 <see cref="LevelMapInitializer"/> 持有，
/// 本元件只是場景內方便掛接（按鈕、事件獎勵）用的門面：讀取轉呼叫中樞，修改轉呼叫中樞的
/// ChangeMoney / ChangeHp（會觸發事件讓各 UI 自動更新）。保留類別/方法名以相容既有綁定。
/// </summary>
public class PlayerStatsManager : MonoBehaviour
{
    private static LevelMapInitializer Hub => LevelMapInitializer.Instance;

    /// <summary>目前 HP（來自中樞；無中樞時回 0）。</summary>
    public int Health => Hub != null ? Hub.Hp : 0;
    /// <summary>目前金幣（來自中樞）。</summary>
    public int Gold => Hub != null ? Hub.Money : 0;
    /// <summary>目前持有道具（來自中樞）。</summary>
    public IReadOnlyList<Item> Items => Hub != null ? Hub.HeldItems : new List<Item>();

    /// <summary>修改金幣（+/-）。轉呼叫中樞，會觸發 MoneyChanged 讓 UI 同步。</summary>
    public void ModifyGold(int amount)
    {
        if (Hub == null) { Debug.LogWarning("[PlayerStatsManager] 找不到 LevelMapInitializer，無法修改金幣"); return; }
        Hub.ChangeMoney(amount);
        Debug.Log($"玩家金幣變化 {amount}，目前 {Hub.Money}");
    }

    /// <summary>修改血量（+/-）。轉呼叫中樞，會觸發 HpChanged 讓狀態 UI 同步。</summary>
    public void ModifyHealth(int amount)
    {
        if (Hub == null) { Debug.LogWarning("[PlayerStatsManager] 找不到 LevelMapInitializer，無法修改血量"); return; }
        Hub.ChangeHp(amount);
        Debug.Log($"玩家血量變化 {amount}，目前 {Hub.Hp}/{Hub.MaxHp}");
    }
}
