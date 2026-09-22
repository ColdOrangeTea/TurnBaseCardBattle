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
    [Header("卡片共用 Prefab（抽卡都用這一個，資料由 SO 帶入）")]
    private GameObject cardPrefab;

    [SerializeField]
    [Header("基礎攻擊卡（固定放在第 1 格）")]
    private SO_CardData basicAttackCard;

    [SerializeField]
    [Header("可抽到的卡片（隨機池）")]
    private List<SO_CardData> drawableCards = new List<SO_CardData>();

    [SerializeField]
    [Header("卡片的父物件")]
    private Transform[] cardSlots; // 存儲Canvas中Image對象的父物件位置 要手動指定

    [SerializeField]
    [Header("變更圖像的按鈕")]
    private Button changeImageButton; // 變更圖像按鈕
    public Button ToNextTurnButton;

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
        SO_CardData[] selected = SelectRandomCards(cardSlots.Length); // 隨機抽卡（第 1 格另外固定基礎攻擊卡）

        int temp_RefillCount = 0;
        for (int i = 0; i < cardSlots.Length; i++)
        {
            ResetCardData(cardDatas[i]); // 把用過的卡(被設為不可見的)的資料重製
            ClearImageSlot(cardSlots[i]); // 把用過的卡(被設為不可見的)的物件刪除

            SO_CardData so;
            if (i == 0) so = basicAttackCard;                 // 第1張牌固定基礎攻擊卡
            else so = selected[temp_RefillCount++];           // 其餘由隨機池帶入

            GameObject newCard = SpawnCard(so, cardSlots[i]);
            InitCardDatas(newCard, i);
        }

        List<Transform> temp_Slots = cardSlots.ToList();
        SetCardsVisible(temp_Slots, true); // 所有卡物件都設為可見
    }

    void OnDrawCardClick() // 全部的卡重抽
    {
        List<Transform> temp_Slots = cardSlots.ToList();
        SetCardsVisible(temp_Slots, true); // 所有卡物件都設為可見

        SO_CardData[] selected = SelectRandomCards(cardSlots.Length);
        ResetAllCardDatas();

        for (int i = 0; i < cardSlots.Length; i++) // 清除現有的子物件，但保留指定的 TextMeshPro 元件
        {
            ClearImageSlot(cardSlots[i]);

            SO_CardData so = (i == 0) ? basicAttackCard : selected[i]; // 第1張牌固定基礎攻擊卡
            GameObject newCard = SpawnCard(so, cardSlots[i]);
            InitCardDatas(newCard, i);
        }
    }

    /// <summary>用共用 prefab 生成一張卡，並把 SO 資料帶進 CardData（種類/呈現/行為/音效皆由 SO 決定）。</summary>
    private GameObject SpawnCard(SO_CardData so, Transform slot)
    {
        if (cardPrefab == null)
        {
            BattleLog.Log("[S002] 未指派卡片共用 prefab(cardPrefab)，無法生成卡片。");
            return null;
        }
        GameObject go = Instantiate(cardPrefab, slot.position, Quaternion.identity, slot);
        CardData cd = go.GetComponent<CardData>();
        if (cd == null) cd = go.GetComponentInChildren<CardData>(true);
        if (cd != null && so != null) cd.Setup(so);
        SetRectTransform(ref go); // 確保 RectTransform 的位置是正確的
        return go;
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

    // 從隨機池隨機挑選卡片資料（回傳 count 張；第 1 格會由呼叫端改成基礎攻擊卡）
    private SO_CardData[] SelectRandomCards(int count)
    {
        SO_CardData[] selected = new SO_CardData[count];
        if (drawableCards == null || drawableCards.Count == 0)
        {
            BattleLog.Log("[S002] 隨機卡池(drawableCards)為空，抽卡將只有基礎攻擊卡。");
            return selected;
        }
        for (int i = 0; i < count; i++)
        {
            selected[i] = drawableCards[UnityEngine.Random.Range(0, drawableCards.Count)];
        }
        return selected;
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