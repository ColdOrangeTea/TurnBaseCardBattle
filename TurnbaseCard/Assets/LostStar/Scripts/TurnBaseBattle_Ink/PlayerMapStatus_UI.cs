using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;
using UnityEngine.UI; // 引入 UI 命名空間
using TMPro;

public class PlayerMapStatus_UI : MonoBehaviour
{
    // UI 元素引用
    public TMP_Text unitNameText; // 名稱 UI
    public TMP_Text diceCountText; // 骰子數量 UI
    public Image hpBarImage; // 血量條 UI
    public TMP_Text hpText; // 顯示血量數字的 Text
    public TurnBaseBattlePlayerData playerDataInMap; // 這是玩家在大地圖的資料，可以用來保存從戰鬥中取得的殘存HP、用物品+HP

    private void Start()
    {
        Status_UI();
    }

    public void GetPlayerDataFromTBBM(TurnBaseBattlePlayerData data)
    {
        playerDataInMap.SetTurnOrder(TurnBaseBattleOrderType.FirstMember);
        playerDataInMap.SetUnitName(CharacterType.Seraphis.ToString());
        playerDataInMap.SetTW_UnitName("賽拉菲斯");
        playerDataInMap.SetOriginMaxHp(16);
        playerDataInMap.SetCurHp(16);
        playerDataInMap.SetOriginMaxCountOfDice(2);
        playerDataInMap.SetCountOfDice(2);

        // 更新 UI 顯示
        UpdateUI(playerDataInMap);
    }

    // 初始化並更新 UI
    public void Status_UI()
    {
        // 初始化玩家數據
        TurnBaseBattlePlayerData playerData = new TurnBaseBattlePlayerData();
        playerData.SetTurnOrder(TurnBaseBattleOrderType.FirstMember);
        playerData.SetUnitName(CharacterType.Seraphis.ToString());
        playerData.SetTW_UnitName("賽拉菲斯");
        playerData.SetOriginMaxHp(16);
        playerData.SetCurHp(16);
        playerData.SetOriginMaxCountOfDice(2);
        playerData.SetCountOfDice(2);

        // 更新 UI 顯示
        UpdateUI(playerData);
    }

    // 更新 UI 方法
    public void UpdateUI(TurnBaseBattlePlayerData playerData)
    {
        // 更新名稱
        if (unitNameText != null)
        {
            playerData.SetTurnOrder(TurnBaseBattleOrderType.FirstMember); // 使用中文名稱
            playerData.SetUnitName(CharacterType.Seraphis.ToString());
            playerData.SetTW_UnitName("賽拉菲斯");

            unitNameText.text = playerData.GetTW_UnitName();
        }

        // 更新骰子數量
        if (diceCountText != null)
        {
            diceCountText.text = $" {playerData.GetCountOfDice()}";
        }

        // 更新血量條
        if (hpBarImage != null)
        {
            // 計算血量比例
            float hpRatio = (float)playerData.GetCurHp() / playerData.GetOriginMaxHp();
            hpBarImage.fillAmount = Mathf.Clamp01(hpRatio); // 填充比例
        }
        // 更新血量數字
        if (hpText != null)
        {
            hpText.text = $"{playerData.GetCurHp()} / {playerData.GetOriginMaxHp()}"; // 顯示具體血量
        }
    }
    //// UI 元素引用
    //public TMP_Text unitNameText; // 名稱 UI
    //public TMP_Text diceCountText; // 骰子數量 UI
    //public Image hpBarImage; // 血量條 UI
    //public TMP_Text hpText; // 顯示血量數字的 Text

    //// 玩家資料
    //private TurnBaseBattlePlayerData playerData;

    //private void Start()
    //{
    //    // 初始化玩家資料
    //    TurnBaseBattlePlayerData battleUnitPlayerData = new TurnBaseBattlePlayerData();
    //    playerData = battleUnitPlayerData.InitPlayerInfo(CharacterType.Seraphis);

    //    // 初始化並更新 UI
    //    Status_UI();
    //}

    ///// <summary>
    ///// 更新玩家狀態 UI
    ///// </summary>
    //public void Status_UI()
    //{
    //    if (playerData != null)
    //    {
    //        UpdateUIWithPlayerData(playerData);
    //    }
    //    else
    //    {
    //        Debug.LogWarning("Player data 未初始化，無法更新 UI");
    //    }
    //}

    ///// <summary>
    ///// 使用玩家資料更新 UI
    ///// </summary>
    ///// <param name="playerData">玩家資料</param>
    //public void UpdateUIWithPlayerData(TurnBaseBattlePlayerData playerData)
    //{
    //    // 更新名稱
    //    if (unitNameText != null)
    //    {
    //        unitNameText.text = playerData.GetTW_UnitName();
    //    }

    //    // 更新骰子數量
    //    if (diceCountText != null)
    //    {
    //        diceCountText.text = $"骰子數量: {playerData.GetCurrentCountOfDice()}";
    //    }

    //    // 更新血量條和數值
    //    if (hpBarImage != null && hpText != null)
    //    {
    //        float hpRatio = (float)playerData.GetCurrentHp() / playerData.GetCurrentMaxHp();
    //        hpBarImage.fillAmount = Mathf.Clamp01(hpRatio);
    //        hpText.text = $"血量: {playerData.GetCurrentHp()} / {playerData.GetCurrentMaxHp()}";
    //    }
    //}
}
