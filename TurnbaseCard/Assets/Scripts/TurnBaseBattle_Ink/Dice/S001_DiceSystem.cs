using UnityEngine;
using UnityEngine.UI; // 引入Unity的UI命名空
using TMPro; // 引入TextMeshPro的命名空間
using System.Collections.Generic;
using System.Linq;
using Assets.Scripts.GlobalEnums.BattleEnum;

public class S001_DiceSystem : MonoBehaviour
{
    #region 變數宣告區
    public DicePoolManager dicePoolManager; // 手動指定

    [Header("V2 直接引用")]
    [Tooltip("骰子拖放定位需要的根 Canvas。")]
    [SerializeField] private Canvas rootCanvasDirect;
    /// <summary>V2：場上一顆骰子被用掉時通知（取代舊版直接改 BattleAction.temp_UserDiceCount）。參數為扣除的顆數。</summary>
    public System.Action<int> OnDiceConsumed;
    DiceEvent diceEvent = new DiceEvent();
    [Header("骰子資訊")]
    [SerializeField] private Sprite[] images; // 存儲圖片的數組
    [SerializeField] private string[] imageTexts; // 存儲與圖片對應的文字
    [SerializeField] private int[] imageValues; // 每張圖片對應的數值
    [SerializeField] private Image[] usableDices; // 骰子物件
    [SerializeField] private TMP_Text[] textSlots; // 存儲TextMeshPro的數組
    [Header("按鈕")]
    [SerializeField] private Button changeImageButton; // 新增一個Button
    public Button ToNextTurnButton;
    [Header("起始數值")]
    [SerializeField] private int[] currentDiceValues; // 用於存儲當前產生的色子的點數
    [SerializeField] private List<Vector2> OriginDicePos;
    [SerializeField] private List<Vector2> Temp_NewDicePos;
    public int CurTurnDiceCount = 0;

    [Header("測試")]
    public Sprite Dot;
    public GameObject Temp_Group_Player1Dice_Dots;
    [SerializeField] List<GameObject> Dots = new List<GameObject>();

    #endregion

    #region 初始化
    /// <summary>V2：初始化。Temp_Group_Player1Dice_Dots 等物件由 Inspector 直接指定，無需額外動作。</summary>
    public void InitDirect()
    {
    }

    #endregion

    #region "訂閱"
    private void OnEnable()
    {
        // 為按鈕添加點擊事件
        if (changeImageButton != null)
            changeImageButton.onClick.AddListener(CheatRollTheDice);
        DiceEvent.OnDiceRemoved += RemoveUsedDice;
    }
    private void OnDisable()
    {
        if (changeImageButton != null)
            changeImageButton.onClick.RemoveListener(CheatRollTheDice); // 替此按鈕加上擲骰事件
        DiceEvent.OnDiceRemoved -= RemoveUsedDice;
    }
    void ReduceDiceCount(int value) // 扣除目前行動者的骰數
    {
        // V2：發 OnDiceConsumed，由橋接扣 BattleUnit 骰數。
        OnDiceConsumed?.Invoke(value);
    }
    void RemoveUsedDice(GameObject dice) // 把放到卡上的骰子丟回去
    {
        RemoveDice(dice);
        ReduceDiceCount(1);
    }

    #endregion

    #region 公共方法      

    #region 功能方法

    public void OnRollDice(bool isAssigned, int value, int count)
    {
        if (isAssigned)
        {
            AssignFixedDiceValue(value, count);
        }

    }
    public void DiceFunction(CardType cardType, ValueForOperation values, int oriCount)
    {
        switch (cardType)
        {
            case CardType.Dismantle:
            case CardType.Clone:
            case CardType.Reverse:
            case CardType.Redice:
                ApplyDiceFunction(values.UserDiceValue, values.UserDiceCount, oriCount);
                break;
        }
    }

    public void StartDiceSystem()
    {
        ResetUsableDices();
        ChangeDiceInfo();
    }
    public void CheatRollTheDice() // 設置擲骰並給予對應圖片、文字、數值 從ToNextTurn() 觸發
    {
        // ResetUsableDices();
        ChangeDiceInfo();
    }
    public void RollTheDice() // 設置擲骰並給予對應圖片、文字、數值 從ToNextTurn() 觸發
    {
        Debug.Log("RollTheDice");
        ResetUsableDices();
        ChangeDiceInfo();
    }
    public void RemoveUsableDices()
    {
        ResetDiceInfo();
    }

    public void ResetUsableDices()
    {
        ResetDiceInfo();
        InitUsableDice();
    }

    #endregion
    #endregion

    #region  "測試功能"
    #region "墨水新增區"

    /// <summary>
    /// 指定特定顆數骰子為特定數值
    /// </summary>
    /// <param name="value"></param>
    /// <param name="count"></param>
    void AssignFixedDiceValue(int value, int count)
    {
        if (count > usableDices.Length)
        {
            count = usableDices.Length;
        }
        for (int i = 0; i < usableDices.Length; i++)
        {
            // value = UnityEngine.Random.Range(0, imageValues.Length); // 隨機取得新點數 
            if (i < count)
            {
                SetDiceInfo(usableDices[i].gameObject, i, value - 1);
            }

        }
    }

    void ChangeDiceInfo() // 擲骰，更改骰子資料
    {
        for (int i = 0; i < usableDices.Length; i++)
        {
            int value = UnityEngine.Random.Range(0, imageValues.Length); // 隨機取得新點數
            SetDiceInfo(usableDices[i].gameObject, i, value);
            Debug.Log($"ChangeDiceInfo: {usableDices[i].name} 骰數: {value}");
        }
    }

    #region "新增骰子"



    /// <summary>
    /// 骰子功能卡（Dismantle／Clone／Reverse／Redice）共用的處理：
    /// 依運算後的骰數與顆數重新補齊並整理場上骰子。
    /// 色子數值 = imageValues[value-1]，目前色子總數 = count，oriCount 為原本顆數。
    /// </summary>
    void ApplyDiceFunction(int value, int count, int oriCount)
    {
        OrganizeUsableDices();
        int curDiceIndex = oriCount;
        AddDice(value - 1, curDiceIndex, count);
        OrganizeUsableDices();
    }
    public void AddDice(int diceValueIndex, int diceListIndex, int newCount)
    {
        int curDiceIndex = usableDices.Length;
        int oriLength = usableDices.Length;
        // Debug.Log("開始: 骰數為: " + imageValues[diceValueIndex] + "從哪開始: " + curDiceIndex + " 色子總顆數: " + newCount + " " + oriLength + " 新產生的色子有 " + (newCount - oriLength) + " 顆");
        SetArrayLength(newCount, curDiceIndex, diceValueIndex);
        // Debug.Log("結果: 骰數為: " + imageValues[diceValueIndex] + " 色子總顆數: " + newCount + " " + usableDices.Length + " 新產生的色子有 " + (newCount - oriLength) + " 顆");
    }

    // 整理 usableDices 的方法
    void OrganizeUsableDices()
    {
        List<Image> organizedDices = new List<Image>();
        int num = 1;
        // 將現有未使用的骰子按順序重新加入列表
        for (int i = 0; i < usableDices.Length; i++)
        {
            // Debug.Log("整理的過程: " + usableDices[i]);
            if (usableDices[i] != null && usableDices[i].gameObject.activeInHierarchy == true)
            {
                // Debug.Log("整理前: " + usableDices[i]);
                // usableDices[i].name = "Dice Organize" + (num);
                organizedDices.Add(usableDices[i]);

                SetDicePos(usableDices[i].gameObject, i);

                // Debug.Log("整理後: " + usableDices[i]);
                num++;
            }
        }

        // 暫存的列表丟回 usableDices 數組
        usableDices = organizedDices.ToArray();

        // Debug.Log("骰子已整理，共有 " + usableDices.Length + " 顆可用骰子");
    }
    #endregion

    /// <summary>設置骰子資料陣列的大小。為了將初始圖片、文字和數值設置到指定的骰子，需指定 diceIndex
    /// </summary><param name="length">指定的陣列長度</param><param name="diceIndex">從哪一顆存於 usableDices 陣列中的骰子開始</param>
    void SetArrayLength(int length, int diceIndex, int diceValue)
    {
        System.Array.Resize(ref usableDices, length);
        System.Array.Resize(ref textSlots, length);
        System.Array.Resize(ref currentDiceValues, length);

        if (length < 1) return;

        // 將初始圖片、文字和數值設置到指定的骰子
        for (int i = diceIndex; i < usableDices.Length; i++)
        {
            usableDices[i] = dicePoolManager.GetDice(i).GetComponent<Image>();
            Debug.Log($"{i} SetArrayLength: {usableDices[i].name} {usableDices[i].gameObject.activeInHierarchy}");
            textSlots[i] = usableDices[i].transform.GetChild(0).GetComponent<TMP_Text>();
            SetDiceInfo(usableDices[i].gameObject, i, diceValue); // 初始化可互動的骰子資料 全起始為1
            SetDicePos(usableDices[i].gameObject, i);
        }

    }

    /// <summary>dice: 指定的骰子  Index: 第幾個骰子  diceValue:骰子的數值
    /// </summary><param name="dice">指定的骰子</param><param name="Index">第幾個骰子</param><param name="diceValue">骰子的數值</param>
    void SetDiceInfo(GameObject dice, int Index, int diceValue) // imageValues[diceValue] = diceValue + 1
    {
        dicePoolManager.SetDiceValueToDice(dice.gameObject, imageValues[diceValue]);
        usableDices[Index].sprite = images[diceValue];
        currentDiceValues[Index] = imageValues[diceValue]; // 設定新的骰子點數
        textSlots[Index].text = imageTexts[diceValue]; // 更新UI文字
    }
    void SetDicePos(GameObject dice, int Index)
    {
        Vector2 pos = new Vector2(0, 0);
        if (Index < usableDices.Length)
        {
            if (Index < OriginDicePos.Count)
            {
                pos = OriginDicePos[Index];
            }
            else
            {
                int range = 80;
                pos = Temp_NewDicePos[0] + new Vector2(Random.Range(-range, range), Random.Range(-range, range));
            }
        }
        dice.GetComponent<RectTransform>().localPosition = pos;
        dice.GetComponent<S004_DiceMove>().StoreInitialPosition(pos, rootCanvasDirect); // V2：由 Inspector 指定的根 Canvas
    }

    void ResetDiceInfo()
    {
        // 清除所有可互動的骰子資料，並把骰子丟回物件池 
        for (int i = 0; i < usableDices.Length; i++)
        {
            SetDiceInfo(usableDices[i].gameObject, i, 0);
            if (usableDices[i] != null)
            {
                RemoveDice(usableDices[i].gameObject);
                usableDices[i] = null;
            }

            if (textSlots[i] != null)
                textSlots[i] = null;
        }
        SetArrayLength(0, 0, 0);
    }
    void RemoveDice(GameObject dice) //把骰子丟回物件池 
    {
        dicePoolManager.ReturnDice(dice.gameObject);
    }

    void InitUsableDice() // 從骰子物件池中取得相應行動者骰子持有數數量的骰子
    {
        if (usableDices.Length > 0)
        {
            ResetDiceInfo();
        }
        SetArrayLength(CurTurnDiceCount, 0, 0); // imageValues[0] = 1
    }

    #endregion


    #endregion
}
