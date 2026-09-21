using UnityEngine;
using TMPro;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System.Diagnostics;

public class S005_NumericalCalculation : MonoBehaviour
{
    DiceEvent diceEvent = new DiceEvent();

    [Header("UI顯示")]
    [SerializeField] private List<TMP_Text> resultTexts = new List<TMP_Text>();

    [Header("計算範圍")]
    public List<RectTransform> panels = new List<RectTransform>();
    [SerializeField] private List<int> areaTotals = new List<int>(); // 每張卡上的骰數總和

    [Header("卡片數據")]
    [SerializeField] bool temp_IsMetTheConditions;
    [SerializeField] bool temp_IsAbleToUse;

    [Header("物件")]
    public GameObject Group_Cards;

    [Header("音效")]
    [SerializeField] private List<AudioClip> cardSoundEffects; // 不同卡片的音效列表（一律經 AudioDirector 播）

    /// <summary>V2：初始化。Group_Cards 由 Inspector 直接指定。</summary>
    public void InitDirect()
    {
        if (Group_Cards == null)
        {
            UnityEngine.Debug.LogWarning($"[{name}] InitDirect：Group_Cards 未指派，無法設定卡片計算面板。");
            return;
        }
        Init();
    }
    public void Init()
    {
        ResetAreaDiceValue();
        SetPanels();
    }

    public void SetPanels()
    {
        panels.Clear();
        for (int i = 0; i < Group_Cards.transform.childCount; i++)
        {
            panels.Add(Group_Cards.transform.GetChild(i).GetComponent<RectTransform>());
        }
    }

    #region "訂閱"
    private void OnEnable()
    {
        DiceEvent.OnDiceInfoSent += GetDice;
    }

    private void OnDisable()
    {
        DiceEvent.OnDiceInfoSent -= GetDice;
    }

    void GetDice(GameObject dice, int diceValue, RectTransform panel) // 從DiceMove取得骰子資料
    {
        for (int i = 0; i < panels.Count; i++)
        {
            if (panels[i] == panel)
            {
                DetectCard(dice, i, diceValue, panel);
                break;
            }
        }
    }

    #endregion

    void DetectCard(GameObject dice, int index, int value, RectTransform panel)
    {
        CardData cardData = panel.GetChild(1).GetComponent<CardData>();
        ResetDraggingDicePos(cardData, dice);

        // 骰子符合條件，能放上去
        if (cardData.CheckDiceDraggedToCardMeetConditions(value))
        {
            areaTotals[index] += value;
            diceEvent.RemoveDiceOnCard(dice); // 用過的骰子要丟回物件池 觸發 S001_DiceSystem 的 RemoveUsedDice()
            if (cardData.CheckIfCardCanUsed(cardData.CheckDiceDraggedToCardMeetConditions(value), value))
            {
                cardData.CardCanBeUsed(cardData.GetIsAbleToUse());// 如果骰子符合條件，放上去時檢測一下能不能用卡
                diceEvent.SendInfoOfCardAndDices(cardData, areaTotals[index]);

                cardData.gameObject.SetActive(false);
                if (cardData.cardType == CardType.Attack) // 第一章攻擊卡不用停用
                {
                    cardData.gameObject.SetActive(true);
                }

                areaTotals[index] = 0;
                // 播放音效，根据卡片类型播放不同音效
                PlayCardTriggeredSound(cardData.cardType);
            }
        }
        else // 如果不符合条件，则重置色子位置
        {
            BattleLog.Log("未達成条件，重置色子位置");
            dice.GetComponent<S004_DiceMove>().ReturnToInitialPosition(); // 重置色子
        }
    }

    void ResetDraggingDicePos(CardData cardData, GameObject dice)
    {
        if (cardData.gameObject.activeInHierarchy == false) // 卡已經用了
        {
            BattleLog.Log("目前位置上的卡已被使用，重置色子位置");
            dice.GetComponent<S004_DiceMove>().ReturnToInitialPosition(); // 重置色子
            return;
        }
    }


    /// <summary>對外播放某張卡的音效（供敵人回合等非拖放路徑補音效用）。</summary>
    public void PlayCardSound(CardType cardType) => PlayCardTriggeredSound(cardType);

    // 根据卡片类型播放不同的音效
    private void PlayCardTriggeredSound(CardType cardType)
    {
        int soundIndex = (int)cardType; // CardType 列舉值對應音效索引
        if (cardSoundEffects.Count > soundIndex && cardSoundEffects[soundIndex] != null)
        {
            // 卡牌對應音效一律交給 AudioDirector 播（吃全域 SFX 音量），不再退回本地 AudioSource
            var clip = cardSoundEffects[soundIndex];
            if (AudioDirector.Instance != null) AudioDirector.Instance.PlaySFX(clip);
            else UnityEngine.Debug.LogWarning("[S005_NumericalCalculation] 場上沒有 AudioDirector，卡片音效未播放。");
        }
    }

    public void ResetAreaDiceValue() // 重製所有色子的點數為0
    {
        areaTotals.Clear();
        for (int j = 0; j < panels.Count; j++)
        {
            areaTotals.Add(0);
        }
    }
}

