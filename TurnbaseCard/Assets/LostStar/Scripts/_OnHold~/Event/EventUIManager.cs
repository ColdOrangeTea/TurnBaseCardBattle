using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Collections;
using System;

public class EventUIManager : MonoBehaviour
{
    public SO_EventManager eventManager; // 事件管理器
    public TMP_Text questionText;             // 問題文本
    public TMP_Text EventTitle;             // 問題標題
    public Button optionAButton;          // 選項A按鈕
    public Button optionBButton;          // 選項B按鈕
    public Button optionCButton;          // 選項C按鈕
    public GameObject EventUI;
    public GameObject EventEmpty;
    public GameObject ResultUI;
    public TMP_Text Result;
    public Button CloseButton;   

    private PlayerStatsManager playerStats; // 玩家屬性

    public event Action EventEnd;


   [SerializeField]
    private SO_Event currentEvent; // 當前事件

    void Start()
    {

        EventEmpty.SetActive(false);

        GameObject Playerstats = GameObject.FindWithTag("PlayerStatsManager");

        if (Playerstats != null)
        {
            playerStats = Playerstats.GetComponent<PlayerStatsManager>();
        }
        else
        {
            Debug.LogWarning("未找到標記為 'PlayerStats' 的 PlayerStatsManager 物件");
        }
    }

    public void ShowEvent()
    {
        currentEvent = eventManager.GetRandomEvent(); // 從 eventManager 取得事件資料

        EventEmpty.SetActive(true);
        EventUI.SetActive(true);
        ResultUI.SetActive(false);

        if (currentEvent != null)
        {
            Debug.Log("取得事件");
            EventTitle.text = currentEvent.Title;
            questionText.text = currentEvent.question;
            optionAButton.GetComponentInChildren<TMP_Text>().text = currentEvent.optionA;
            optionBButton.GetComponentInChildren<TMP_Text>().text = currentEvent.optionB;
            optionCButton.GetComponentInChildren<TMP_Text>().text = currentEvent.optionC;

            // 移除舊的事件防止重複綁定
            optionAButton.onClick.RemoveAllListeners();
            optionBButton.onClick.RemoveAllListeners();
            optionCButton.onClick.RemoveAllListeners();

            // 添加按鈕點擊事件，將結果文本傳遞到 ChooseOption
            optionAButton.onClick.AddListener(() => ChooseOption(currentEvent.effectsA, currentEvent.resultA));
            optionBButton.onClick.AddListener(() => ChooseOption(currentEvent.effectsB, currentEvent.resultB));
            optionCButton.onClick.AddListener(() => ChooseOption(currentEvent.effectsC, currentEvent.resultC));
        }
    }

    private void ChooseOption(List<Effect> effects, string resultText)
    {
        // 應用每個效果
        foreach (var effect in effects)
        {
            ApplyEffect(effect);
        }

        EventUI.SetActive(false);
        ResultUI.SetActive(true);

        // 將結果顯示在 questionText 上
        Result.text = resultText;

     
    }

    public void CloseEvent()
    {
        EventEmpty.SetActive(false);
        EventEnd?.Invoke();
    }

 
    private void ApplyEffect(Effect effect)
    {
        if (playerStats != null)
        {
            switch (effect.effectType)
            {
                case EffectType.AddGold:
                    playerStats.ModifyGold(effect.amount);
             
                    Debug.Log("金錢增加 :" + effect.amount);
                    break;
                case EffectType.ReduceGold:
                    playerStats.ModifyGold(-effect.amount);
                   
                    Debug.Log("金錢減少 :" + effect.amount);
                    break;
                case EffectType.AddHealth:
                    playerStats.ModifyHealth(effect.amount);
                    Debug.Log("血量增加 :" + effect.amount);
                    break;
                case EffectType.ReduceHealth:
                    playerStats.ModifyHealth(-effect.amount);
                    Debug.Log("血量減少 :" + effect.amount);

                    break;
            }
        }
    }
    // 可以選擇再次顯示新事件或隱藏 UI
    // ShowEvent(); // 如果想再次顯示事件
}

