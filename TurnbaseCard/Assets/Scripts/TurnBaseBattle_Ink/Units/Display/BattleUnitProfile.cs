using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;
using TMPro;
using UnityEngine.UI;
using Spine.Unity;
using DG.Tweening;
using UnityEditor.Rendering;
using Unity.VisualScripting;
using System;
public class BattleUnitProfile : MonoBehaviour // 掛TurnBaseBattleUnit 的 PlayerData
{
    public TBBattleVFXPlayer UnitVFXPlayer;
    UIBarMove uIBarMove = new UIBarMove();
    [Header("人物圖像的List")]
    public SO_BattleCharacterProfile characterProfiles; // 手動掛 SO
    public List<GameObject> SpineAnim_characterProfiles = new List<GameObject>(); // List順序對應 CharacterType
    public SO_BattleEnemyProfile enemyProfiles; // 手動掛 SO
    public List<GameObject> SpineAnim_EnemyProfiles = new List<GameObject>();

    // public string UnitAnimState;
    // [SerializeField] 
    private string nameString;
    // [SerializeField] 
    private SkeletonAnimation unitSkeletonAnim;
    [SerializeField]
    private Color originalUnitAnimColor;

    // [SerializeField] 
    private int diceCountInt;
    [SerializeField]
    private int oriHpInt;
    [SerializeField]
    private int hpInt;
    // [SerializeField]
    private List<BattleStatusEffectType> status;
    public List<Sprite> AllStatusImage;
    private float originalHPBarWidth;

    [Header("此人物的資料，物件需手動指定(除了unitSprite)")]
    public TMP_Text NameText;
    public SkeletonGraphic UnitAnim;
    public TMP_Text DiceCountText;
    public TMP_Text HpText;
    public Image HpRedBar;
    public GameObject UnitStatusImageParentPos;
    public List<Image> UnitStatusImage;

    [Header("用來指定圖像的Enum，需從人物或怪物的資料取得Enum")]
    [SerializeField] private CharacterType unitType;
    public EnemyType BaseEnemyType; // 基本的敵人資料型別
    public EnemyType EndlessModeEnemyType;  // 對應的無盡模式敵人型別(是人形機器薯，不包含馬鈴薯型態)
    public Coroutine SpineDelayedInitialize;

    void Awake()
    {
        if (HpRedBar != null)
            originalHPBarWidth = HpRedBar.rectTransform.sizeDelta.x;
    }

    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         PlaySE(CardType.Heal, false, false, BattleStatusEffectType.None);
    //     }
    // }

    public void PlaySE(CardType cardType, bool isDamage, bool isApplyState, BattleStatusEffectType effectType)
    {
        if (UnitVFXPlayer == null)
        {
            // 尚未接上 Spine／特效播放器（例如骨架範例場景）時，安全略過視覺表現，不影響數值流程。
            return;
        }

        Debug.Log("閃爍");
        if (cardType == CardType.Undefined && effectType != BattleStatusEffectType.None) // 觸發狀態，不管是哪張卡片
        {
            UnitVFXPlayer.PlayVFXOnUnit(UnitAnim, cardType, effectType, isDamage, isApplyState);
            UnitVFXPlayer.SwitchUnitSpineAnimState(UnitAnim, cardType, effectType, isDamage, isApplyState);
            return;
        }

        if (isApplyState && effectType != BattleStatusEffectType.None)
        {
            UnitVFXPlayer.PlayVFXOnUnit(UnitAnim, cardType, effectType, isDamage, isApplyState);
            return;
        }
        UnitVFXPlayer.PlayVFXOnUnit(UnitAnim, cardType, effectType, isDamage, isApplyState);
        UnitVFXPlayer.SwitchUnitSpineAnimState(UnitAnim, cardType, effectType, isDamage, isApplyState);
    }

    public void SetUnitName(string name) => nameString = name;
    // public void SetUnitImage(SkeletonGraphic image) => UnitImage = image;
    public void SetUnitDiceCount(int count) => diceCountInt = count;
    public void SetOriHPCount(int value) => oriHpInt = value;
    public void SetCurHPCount(int value) => hpInt = value;
    public void SetHPBarWidthBackToOriginalWidth()
    {
        if (HpRedBar != null)
            HpRedBar.rectTransform.sizeDelta = new Vector2(originalHPBarWidth, HpRedBar.rectTransform.sizeDelta.y);
    }

    public void SetStatus(List<BattleStatusEffectType> statusType) => status = statusType;

    public void GetUnitType(CharacterType unit, EnemyType baseEnemyType, EnemyType endlessModeEnemyType) // 對應的無盡模式敵人型別
    {
        (unitType, this.BaseEnemyType, this.EndlessModeEnemyType) = (unit, baseEnemyType, endlessModeEnemyType);
    }

    public void UpdateStatus()
    {
        // 清空已存在的圖像
        foreach (Image img in UnitStatusImage)
        {
            Destroy(img.gameObject);
        }
        UnitStatusImage.Clear();

        if (status == null || UnitStatusImageParentPos == null || AllStatusImage == null) return; // 尚未接上狀態圖示（骨架範例場景）時略過

        foreach (BattleStatusEffectType st in status)
        {
            int index = (int)st;

            if (index >= 0 && index < AllStatusImage.Count)
            {
                // 在指定父物件下產生新物件
                GameObject newStatusImageObj = new GameObject("StatusImage_" + st, typeof(Image));
                newStatusImageObj.transform.SetParent(UnitStatusImageParentPos.transform, false);

                Image newStatusImage = newStatusImageObj.GetComponent<Image>();
                newStatusImage.sprite = AllStatusImage[index];

                UnitStatusImage.Add(newStatusImage);
            }
            else
            {
                Debug.LogWarning("Index out of range for status: " + st);
            }
        }
    }

    public void UpdateNameText()
    {
        if (NameText != null)
        {
            if (nameString != null)
                NameText.text = nameString;
        }
    }

    public void UpdateProfileUI() // 之後能把要更新的UI功能都寫在這邊，方便調用
    {
        UpdateHpUI();
        UpdateDiceCountText();
        UpdateNameText();
        UpdateStatus();
    }

    // void UpdateHpUI()
    // {
    //     UpdateHpText();
    //     float hpRatio = (float)hpInt / oriHpInt;
    //     HpRedBar.rectTransform.sizeDelta = new Vector2(originalHPBarWidth * hpRatio, HpRedBar.rectTransform.sizeDelta.y);
    // }

    void UpdateHpUI()
    {
        if (HpRedBar == null || HpText == null || oriHpInt <= 0)
        {
            // 尚未接上血條/文字（骨架範例場景）時，退回單純更新數字，避免除以零或解析空字串。
            UpdateHpText(hpInt);
            return;
        }
        float hpRatio = (float)hpInt / oriHpInt;  // 計算血量比例
        float targetWidth = originalHPBarWidth * hpRatio;  // 計算目標寬度
        int startHp = ParseCurrentHpText(hpInt);  // 獲取當前顯示的血量數值(更新畫面當下，hpInt已是更新後的數值，只能透過text取得先前的值)
        if (startHp == hpInt) return;
        Debug.Log($"{oriHpInt} {hpInt} {startHp} 血量數字動畫完成！");
        uIBarMove.UIWidth_SmoothReduce(HpRedBar.rectTransform, targetWidth, 0.5f, Ease.OutQuad, null);
        uIBarMove.UINumberTextSmoothReduce(oriHpInt, startHp, hpInt,
        (startNum) =>
        {
            UpdateHpText(startNum);
        });
    }
    /// <summary>從 HpText 目前文字（格式「n / m」）解析出目前顯示的 HP；解析失敗時回傳 fallback。</summary>
    int ParseCurrentHpText(int fallback)
    {
        if (HpText != null && !string.IsNullOrEmpty(HpText.text))
        {
            string[] parts = HpText.text.Split('/');
            if (parts.Length > 0 && int.TryParse(parts[0].Trim(), out int parsed))
                return parsed;
        }
        return fallback;
    }
    public void UpdateHpText(int startHp)
    {
        if (HpText != null)
        {
            HpText.text = startHp.ToString() + " / " + oriHpInt.ToString();
            // Debug.Log($"HP:{startHp} 血量動畫完成，更新血量條大小。");
        }
    }
    public void GetOriginSpineAnimColor()
    {
        if (UnitAnim != null)
        {
            if (EndlessModeEnemyType != EnemyType.Undefined_Temp_ThisIsTypeEndNumber) // 是天塔人形薯
            {
                originalUnitAnimColor = new Color(0.78f, 0.9f, 0.9f); // #6E8C96 對應的 RGB 值
                UnitVFXPlayer.GetOriginSpineAnimColor(originalUnitAnimColor);
                // unitSpineAnim.SetOriginSpineAnimColor(originalUnitAnimColor);
            }
            else
            {
                originalUnitAnimColor = new Color(1f, 1f, 1f); // 保存原始顏色
                UnitVFXPlayer.GetOriginSpineAnimColor(originalUnitAnimColor);

                // unitSpineAnim.SetOriginSpineAnimColor(originalUnitAnimColor);
            }

        }

    }

    IEnumerator DelayedInitialize(SkeletonGraphic skeletonGraphic) // 強制初始化
    {
        yield return new WaitForEndOfFrame();
        skeletonGraphic.Initialize(true);
        UnitVFXPlayer.SetUnitOriginAnimState(0, UnitAnim, BattleUnitSpineAnimType.Idle, true);
        // unitSpineAnim.SetUnitAnimState(0, UnitAnim, BattleUnitSpineAnimType.Idle, true);
    }

    public void UpdateDiceCountText()
    {
        if (DiceCountText != null)
        {
            DiceCountText.text = "x" + diceCountInt.ToString();
        }
    }


    #region  InitModule
    public void InitSpineAnim()
    {
        TurnBaseBattleEnemyData data = new TurnBaseBattleEnemyData();
        EnemyType forCheckEnemyType = BaseEnemyType;
        if (UnitAnim != null)
        {
            // Debug.Log(unitAnim);
            if (unitSkeletonAnim != null)
            {
                UnitAnim.skeletonDataAsset = unitSkeletonAnim.skeletonDataAsset;
                UnitAnim.startingLoop = true;
                SpineDelayedInitialize = StartCoroutine(DelayedInitialize(UnitAnim));
                UnitAnim.color = originalUnitAnimColor;
                if (unitType == CharacterType.Enemy)
                {
                    if (forCheckEnemyType == EnemyType.Godness)
                    {
                        UnitAnim.transform.localScale = new Vector3(0.7f, 0.7f, 0.7f);
                    }
                    else
                    {
                        UnitAnim.transform.localScale = new Vector3(1, 1, 1);
                    }

                }

            }
            else
            {
                Debug.LogError("unitAnim is null! Please assign a prefab.");
            }
        }
    }
    void InitUnitType()
    {
        int characterTypeCountEnd = (int)CharacterType.Enemy;
        int enemyTypeCount = (int)EnemyType.Undefined_Temp_ThisIsTypeEndNumber;
        int thisNumOfCharacter = (int)unitType;
        TurnBaseBattleEnemyData data = new TurnBaseBattleEnemyData();
        int thisNumOfEnemy = Convert.ToInt32(BaseEnemyType);

        // if (EndlessModeEnemyType != EnemyType.Undefined_Temp_ThisIsTypeEndNumber) // 是天塔人形薯 但不必找，因為是共享故事模式敵人的資料
        // {
        //     thisNumOfEnemy = Convert.ToInt32(EndlessModeEnemyType);
        // }
        // int thisNumOfEnemy = (int)enemy;

        if (thisNumOfCharacter < characterTypeCountEnd) // 玩家角色
        {
            for (int i = 0; i < characterTypeCountEnd; i++)
            {
                if (thisNumOfCharacter == i)
                {
                    unitSkeletonAnim = SpineAnim_characterProfiles[i].GetComponent<SkeletonAnimation>();
                    break;
                }
            }
        }
        else
        {
            if (unitType == CharacterType.Undefined_Temp_ThisIsTypeEndNumber)
            {
                Debug.LogError("沒設定好");
                return;
            }
            if (unitType == CharacterType.Enemy)
            {
                for (int i = 0; i < enemyTypeCount; i++)
                {
                    if (thisNumOfEnemy == i)
                    {
                        unitSkeletonAnim = SpineAnim_EnemyProfiles[i].GetComponent<SkeletonAnimation>();
                        break;
                    }
                }
            }
        }
    }

    #endregion
    public void InitProfile()
    {
        if (characterProfiles == null || enemyProfiles == null)
        {
            // 尚未指定角色／敵人圖像 SO（骨架範例場景）時，略過 Spine 初始化，其餘 UI（HP/名稱/骰數）仍正常運作。
            Debug.LogWarning($"[{name}] InitProfile：未指定 characterProfiles / enemyProfiles，略過 Spine 圖像初始化。");
            return;
        }
        SpineAnim_characterProfiles = characterProfiles.characterProfile;
        SpineAnim_EnemyProfiles = enemyProfiles.enemyProfile;

        InitUnitType();
        InitSpineAnim();

    }
}
