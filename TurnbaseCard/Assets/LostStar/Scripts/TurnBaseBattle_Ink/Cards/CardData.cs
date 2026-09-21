using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TMPro;
using System.Collections.Generic;
using System;


public class CardData : MonoBehaviour
{
    [Header("卡片種類")]
    public CardType cardType; // 每種卡片Prefab記得要手動指派
    private BattleCardInfo info;

    [Header("目前先自己手動填中文名稱和敘述")]
    public string CardName;
    public string Tw_CardName;
    public string Tw_Description;

    [Header("卡片條件")]
    // bool isMetTheConditions = false;
    [SerializeField] bool isAbleToUse = false;
    // [SerializeField] int laserGunNeededValue = 10;
    [SerializeField] private int accumulatedValue = 10; // 累積骰數才能觸發的數值

    public const string EVEN = "偶數";
    public const string ODD = "奇數";

    [Header("UI顯示")]
    public UnityEngine.UI.Image diceFrame;
    public TMP_Text Name; // 用於顯示需要數值的TextMeshPro    
    public List<TMP_Text> Description = new List<TMP_Text>(); // 用於顯示需要數值的TextMeshPro 
    public TMP_Text RequiredValueText; // 用於顯示需要數值的TextMeshPro    

    [Header("音效")]
    public AudioSource Use_SFX; // 新增音效播放源

    // [SerializeField] int temp_DiceValue;

    #region Get Set Functions
    void UpdateRequiredValue(int value) => accumulatedValue -= value;

    // public bool GetIsMetTheConditions() => isMetTheConditions;

    public bool GetIsAbleToUse() => isAbleToUse;
    public void SetIsAbleToUse(bool isAbleTo) => isAbleToUse = isAbleTo;

    public BattleCardInfo GetBattleCardInfo() => info;

    #endregion

    void Start()
    {
        InitCardData();
        InitCardPulledDiceCondition();
    }

    void InitCardData()
    {
        foreach (int item in Enum.GetValues(typeof(CardType)))
        {
            if (item == (int)cardType)
            {
                info = BattleCard.SendCardInfo(cardType);
                return;
            }
        }
    }

    void InitCardPulledDiceCondition()
    {
        (bool odd, bool even, bool accumulated, int accu_Value, bool designed, int desi_Value, bool isEqualTo, bool isGreaterThan, bool isLessThan) = (
        info.RequiredOddDiceValue, info.RequiredEvenDiceValue, info.RequiredAccumulatedDiceValue,
        info.Accu_DiceValue, info.RequireDesignatedDiceValue, info.Desi_DiceValue, info.IsEqualTo, info.IsGreaterThan, info.IsLessThan);
        if (!odd && !even && !accumulated && !designed)
        {
            RequiredValueText.text = "X";
            return;
        }
        if (odd == true)
        {
            RequiredValueText.text = "X=" + ODD;
            return;
        }
        else if (even == true)
        {
            RequiredValueText.text = "X=" + EVEN;
            return;
        }
        else if (accumulated == true)
        {
            RequiredValueText.text = accu_Value.ToString();
            accumulatedValue = accu_Value;
            return;
        }
        else if (designed == true)
        {
            if (isEqualTo)
            {
                RequiredValueText.text = desi_Value.ToString();
                if (isGreaterThan)
                {
                    RequiredValueText.text = "X>=" + desi_Value.ToString();
                    return;
                }
                else if (isLessThan)
                {
                    RequiredValueText.text = "X<=" + desi_Value.ToString();
                    return;
                }
                return;
            }
            else if (isGreaterThan)
            {
                RequiredValueText.text = "X>" + desi_Value.ToString();
                return;
            }
            else if (isLessThan)
            {
                RequiredValueText.text = "X<" + desi_Value.ToString();
                return;
            }
        }
    }
    public void ResetCardConditions()
    {
        isAbleToUse = false; // 卡片不符合使用條件(重製)
    }

    public bool CheckIfCardCanUsed(bool isDiceCanPutOnCard, int diceValue) // 傳回卡片能否用
    {
        if (!isDiceCanPutOnCard) return false;

        if (!info.RequiredAccumulatedDiceValue)
        {
            return true;
        }
        else
        {
            UpdateRequiredValue(diceValue);
            RequiredValueText.text = accumulatedValue.ToString();
            BattleLog.Log($"目前累積數值:{accumulatedValue} 滿足骰子條件: {isAbleToUse}");
            if (accumulatedValue <= 0)
            {
                accumulatedValue = 0;
                return true;
            }
            else
            {
                return false;
            }
        }
        // return false;
    }
    public bool CheckDiceDraggedToCardMeetConditions(int diceValue) // 傳回骰子符合條件
    {
        // SetIsMetTheConditions(false); 
        // isAbleToUse = false; // 卡片不符合使用條件(重製)

        (bool odd, bool even, bool accumulated, int accu_Value,
        bool designed, int desi_Value,
        bool isEqualTo, bool isGreaterThan, bool isLessThan) =
        (
        info.RequiredOddDiceValue, info.RequiredEvenDiceValue, info.RequiredAccumulatedDiceValue, info.Accu_DiceValue,
        info.RequireDesignatedDiceValue, info.Desi_DiceValue,
        info.IsEqualTo, info.IsGreaterThan, info.IsLessThan
        );

        if (!odd && !even && !accumulated && !designed)
        {
            if (diceValue > 0)
            {
                // SetIsAbleToUse(true); // 卡片符合使用條件
                return true;
            }
        }

        if (designed)  // 累積且指定只能放甚麼大小的骰子
        {
            if ((isEqualTo && diceValue == desi_Value) ||
                (isGreaterThan && diceValue > desi_Value) ||
                (isLessThan && diceValue < desi_Value))
            {
                // SetIsAbleToUse(true); // 卡片符合使用條件
                // TriggerCardEffect();
                return true;
            }
        }

        if (!accumulated) // 非累積
        {
            if (odd && diceValue % 2 == 1)
            {
                // SetIsAbleToUse(true); // 卡片符合使用條件
                // TriggerCardEffect();
                return true;
            }
            else if (even && diceValue % 2 == 0)
            {
                // SetIsAbleToUse(true); // 卡片符合使用條件
                // TriggerCardEffect();
                return true;
            }
        }
        else // 累積
        {
            if (diceValue > 0)
            {
                // UpdateRequiredValue(diceValue);
                // RequiredValueText.text = accumulatedValue.ToString();
                // BattleLog.Log($"目前累積數值:{accumulatedValue} 滿足骰子條件: {isAbleToUse}");

                // if (accumulatedValue <= 0)
                // {
                //     accumulatedValue = 0;
                //     // SetIsAbleToUse(true); // 卡片符合使用條件
                //     // TriggerCardEffect();
                //     return true;
                // }
                // else
                // {
                //     return true;
                // }
                return true;
            }
            else if (designed) // 累積且指定只能放甚麼大小的骰子
            {
                if ((isEqualTo && diceValue == desi_Value) ||
                    (isGreaterThan && diceValue > desi_Value) ||
                    (isLessThan && diceValue < desi_Value))
                {
                    if (accumulatedValue <= 0)
                    {
                        accumulatedValue = 0;
                        // SetIsAbleToUse(true); // 卡片符合使用條件
                        // TriggerCardEffect();
                        return true;
                    }
                }
            }
        }




        return false;
    }
    public void CardCanBeUsed(bool isCanUseCard) // 可以就觸發
    {
        if (!isCanUseCard) return; // 不能用卡就退回

        if (!info.RequiredAccumulatedDiceValue) // 不用累積
        {
            TriggerCardEffect(isCanUseCard);
        }
        else
        {
            if (accumulatedValue <= 0)
            {
                TriggerCardEffect(isCanUseCard);
            }
        }

    }

    private void TriggerCardEffect(bool isCanUseCard)
    {
        // SetIsMetTheConditions(isCanUseCard);
        // SetIsAbleToUse(isCanUseCard);

        BattleLog.Log("Triggering card effect...");

        // 卡片音效一律交給 AudioDirector 播（吃全域 SFX 音量），不再退回本地 AudioSource
        if (Use_SFX == null || Use_SFX.clip == null)
        {
            UnityEngine.Debug.LogWarning("[CardData] Use_SFX 或其 clip 未指派，無法播放卡片音效。");
            return;
        }
        if (AudioDirector.Instance != null) AudioDirector.Instance.PlaySFX(Use_SFX.clip);
        else UnityEngine.Debug.LogWarning("[CardData] 場上沒有 AudioDirector，卡片音效未播放。");
    }

}
