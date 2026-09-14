using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;
using System.Linq; // 引入 TextMeshPro 命名空間

public class S002_DrawCardsSystem : MonoBehaviour
{
    #region 變數宣告區
    public S005_NumericalCalculation numericalCalculation;
    [SerializeField]
    [Header("存儲預製件的陣列")]
    private GameObject[] cardPrefabs; // 存儲卡片預製件的陣列

    [SerializeField]
    [Header("卡片的父物件")]
    private Transform[] cardSlots; // 存儲Canvas中Image對象的父物件位置 要手動指定

    [SerializeField]
    [Header("變更圖像的按鈕")]
    private Button changeImageButton; // 變更圖像按鈕
    public Button ToNextTurnButton;

    [SerializeField]
    [Header("初始卡片Prefab")]
    private GameObject initialCardPrefab; // 初始卡片Prefab

    [SerializeField]
    [Header("重新抽取的卡片Prefab")]
    private GameObject rerollCardPrefab; // 重新抽取的卡片Prefab

    [SerializeField]
    [Header("存儲要保留的TextMeshPro元件")]
    private TextMeshProUGUI[] textComponents; // 存儲要保留的 TextMeshPro 元件

    public List<CardData> cardDatas = new List<CardData>();

    #endregion

    #region MonoBehaviour方法
    private void OnEnable()
    {
        if (changeImageButton != null)
            changeImageButton.onClick.AddListener(OnDrawCardClick); // 替此按鈕加上抽卡事件
    }
    private void OnDisable()
    {
        if (changeImageButton != null)
            changeImageButton.onClick.RemoveListener(OnDrawCardClick); // 移除抽卡事件
    }
    #endregion

    #region 功能方法

    #region  "公共方法"
    public void DrawCards()
    {
        if (cardDatas == null || cardDatas.Count <= 0) // 初次抽卡
        {
            OnDrawCardClick();
        }
        else
        {
            OnRefillCard();
        }

    }
    #endregion

    private void SetRectTransform(ref GameObject prefab) // 設置RectTransform的位置和縮放
    {
        RectTransform rectTransform = prefab.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = Vector2.zero; // 設置為相對於父物件的正中央
            rectTransform.localScale = Vector3.one; // 確保縮放為正常大小
        }
    }

    #region DrawCards
    void OnRefillCard()
    {
        GameObject[] selectedPrefabs = SelectRandomPrefabs(4); // 隨機抽卡

        BattleLog.Log("OnRefillCard OnRefillCard OnRefillCard" + selectedPrefabs.Length);
        int temp_RefillCount = 0;
        for (int i = 0; i < cardSlots.Length; i++)
        {
            if (temp_RefillCount > selectedPrefabs.Length) return;

            ResetCardData(cardDatas[i]); // 把用過的卡(被設為不可見的)的資料重製
            ClearImageSlot(cardSlots[i]); // 把用過的卡(被設為不可見的)的物件刪除
            GameObject newPrefab = null;
            if (i == 0) // 第1張牌
            {
                newPrefab = Instantiate(cardPrefabs[0], cardSlots[i].position, Quaternion.identity, cardSlots[i]);// 實例化並設置新的預製件
            }
            else
            {
                BattleLog.Log("OnRefillCard OnRefillCard OnRefillCard " + selectedPrefabs[temp_RefillCount] + " " + temp_RefillCount);
                newPrefab = Instantiate(selectedPrefabs[temp_RefillCount], cardSlots[i].position, Quaternion.identity, cardSlots[i]);// 實例化並設置新的預製件
                temp_RefillCount++;
            }
            InitCardDatas(newPrefab, i);
            SetRectTransform(ref newPrefab); // 確保 RectTransform 的位置是正確的
        }

        List<Transform> temp_Slots = cardSlots.ToList();
        SetCardsVisible(temp_Slots, true); // 所有卡物件都設為可見
    }

    void OnDrawCardClick() // 全部的卡重抽
    {
        List<Transform> temp_Slots = cardSlots.ToList();
        SetCardsVisible(temp_Slots, true); // 所有卡物件都設為可見

        GameObject[] selectedPrefabs = SelectRandomPrefabs(4);
        ResetAllCardDatas();

        for (int i = 0; i < cardSlots.Length; i++) // 清除現有的子物件，但保留指定的 TextMeshPro 元件
        {
            ClearImageSlot(cardSlots[i]);

            if (i == 0) // 第1張牌
                selectedPrefabs[i] = cardPrefabs[0];

            GameObject newPrefab = Instantiate(selectedPrefabs[i], cardSlots[i].position, Quaternion.identity, cardSlots[i]);// 實例化並設置新的預製件

            InitCardDatas(newPrefab, i);
            SetRectTransform(ref newPrefab); // 確保 RectTransform 的位置是正確的
        }
    }

    #endregion


    void ResetCardData(CardData card)
    {
        for (int i = 0; i < cardDatas.Count; i++) // 清除現有的卡片組件資料
        {
            cardDatas.Remove(card);
        }
    }

    void ResetAllCardDatas() => cardDatas.Clear();

    void InitCardDatas(GameObject gameObject, int index)// 墨水新增
    {
        cardDatas.Insert(index, gameObject.GetComponent<CardData>());
        // cardDatas.Add(gameObject.GetComponent<CardData>());
    }

    /// <summary>所有卡的可見狀態都設為輸入的bool狀態</summary>
    void SetCardsVisible(List<Transform> slotsPos, bool isVisible) // 
    {
        if (slotsPos == null || slotsPos.Count <= 0) return;

        foreach (var pos in slotsPos)
        {
            if (!pos.gameObject.activeInHierarchy)
            {
                pos.gameObject.SetActive(isVisible);
            }
        }

        if (cardDatas == null || cardDatas.Count <= 0) return;

        foreach (var card in cardDatas)
        {
            if (!card.gameObject.activeInHierarchy)
            {
                card.gameObject.SetActive(isVisible);
            }
        }

    }

    // 隨機選擇預製件
    private GameObject[] SelectRandomPrefabs(int count)
    {
        GameObject[] selectedPrefabs = new GameObject[count]; // 已選擇的預製件

        for (int i = 0; i < count; i++)  // for loop 'i' is cards'order
        {
            // 從可用的預製件中隨機選擇一個
            int randomIndex = UnityEngine.Random.Range(1, cardPrefabs.Length); // cardPrefabs[0] 為普攻卡
            selectedPrefabs[i] = cardPrefabs[randomIndex];
        }

        return selectedPrefabs;
    }

    #region Delete Cards

    private void ClearImageSlot(Transform slot) // 清除卡片的父位置中的子物件，只保留TextMeshPro元件的子物件
    {
        foreach (Transform child in slot)
        {
            // 檢查當前子物件是否為要保留的文本元件
            bool shouldKeep = false;
            foreach (TextMeshProUGUI textComponent in textComponents)
            {
                if (child.gameObject == textComponent.gameObject)
                {
                    shouldKeep = true;
                    break;
                }
            }

            // 如果不是要保留的元件，銷毀它
            if (!shouldKeep)
            {
                Destroy(child.gameObject);
            }
        }
    }
    #endregion

    #endregion
}