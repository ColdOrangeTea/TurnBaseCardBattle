using System.Collections.Generic;
using UnityEngine;

public class DicePoolManager : MonoBehaviour
{
    [SerializeField] private TurnBaseBattleUI battleUI;
    public S001_DiceSystem diceSystem; // 手動掛

    [Header("色子物件池設定")]
    public GameObject dicePrefab;
    public int initialPoolSize = 10; // 初始池大小
    [SerializeField]
    private List<GameObject> dicePool = new List<GameObject>();
    [SerializeField] Vector2 initPos = new Vector2(-15, -420);

    [Header("物件")]
    public GameObject Group_Cards;
    public GameObject Player1_Group_Dices;
    public GameObject Player2_Group_Dices;


    void SetCardPanelsPosToDice(GameObject dice) => dice.GetComponent<S004_DiceMove>().SetPanels(Group_Cards);
    public void SetDiceValueToDice(GameObject dice, int initValue = 0) => dice.GetComponent<DiceData>().SetDiceValue(initValue);
    public void SetTurnBaseBattleUI(TurnBaseBattleUI battleUI) => this.battleUI = battleUI;


    public void InitFromTurnBaseBattleUI()
    {
        Group_Cards = battleUI.GetGroup_Cards();
        Player1_Group_Dices = battleUI.GetPlayer1Dices();
        Player2_Group_Dices = battleUI.GetPlayer2Dices();
        InitDice();
    }

    void InitDice()
    {

        // 初始化物件池，生成預定數量的骰子
        for (int i = 0; i < initialPoolSize; i++)
        {
            GameObject dice = CreateDiceObject("Dice_UnUsed_" + i);
            dice.SetActive(false); // 不使用時隱藏
            dicePool.Add(dice);
        }
    }

    // 從池中取得可用骰子
    public GameObject GetDice(int count)
    {
        foreach (var dice in dicePool)
        {
            if (!dice.activeInHierarchy)
            {
                Debug.Log($"GetDice: {dice.name} {dice.activeInHierarchy}");
                dice.name = "Dice" + (count + 1).ToString();
                dice.SetActive(true); // 啟用骰子
                return dice;
            }
        }
        return AddNewDiceToPool(count);
    }

    // 將骰子返回到池中
    public void ReturnDice(GameObject dice)
    {
        dice.name = "Dice";
        dice.SetActive(false); // 停用骰子，回收進池
    }

    GameObject AddNewDiceToPool(int count)
    {
        // 若池中沒有可用物件，創建新骰子並添加到池中
        GameObject newDice = CreateDiceObject("Dice" + (count + 1).ToString());
        newDice.SetActive(true);
        dicePool.Add(newDice);
        return newDice;
    }

    GameObject CreateDiceObject(string name)
    {
        GameObject dice = Instantiate(dicePrefab, Player1_Group_Dices.transform.transform);
        (dice.name, dice.transform.localPosition) = (name, initPos);
        SetCardPanelsPosToDice(dice);
        SetDiceValueToDice(dice, 0); // 初始化數值
        return dice;
    }
}
