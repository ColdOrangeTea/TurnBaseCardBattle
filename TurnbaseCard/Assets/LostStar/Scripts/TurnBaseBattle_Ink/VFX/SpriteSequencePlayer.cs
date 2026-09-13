using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;
using UnityEngine.UI;

public class SpriteSequencePlayer : MonoBehaviour
{
    [Header("單位")]
    public Image DisplayUseCardEffect; // 顯示特效的地方
    public Image DisplayStatusEffect; // 顯示特效的地方

    #region  特效圖像

    [Header("普通攻擊")]
    public SO_BattleVisualEffect AttackSpritesPrefabs;
    public List<Sprite> AttackImages;

    [Header("重攻擊")]
    public SO_BattleVisualEffect HeavyAttackSpritesPrefabs;
    public List<Sprite> HeavyAttackImages;

    [Header("雷射槍")]
    public SO_BattleVisualEffect LaserGunSpritesPrefabs;

    public List<Sprite> LaserGunImages;

    [Header("毒")]
    public SO_BattleVisualEffect PoisionSpritesPrefabs;

    public List<Sprite> PoisionImages;
    public List<Sprite> PoisionStatusImages;


    [Header("暈眩")]
    public SO_BattleVisualEffect DizzinessSpritesPrefabs;

    public List<Sprite> DizzinessImages;
    public List<Sprite> DizzinessStatusImages;


    [Header("回復狀態")]
    public SO_BattleVisualEffect RecoverSpritesPrefabs;

    public List<Sprite> RecoverImages;

    [Header("灼燒")]
    public SO_BattleVisualEffect BurntSpritesPrefabs;

    public List<Sprite> BurntImages;
    public List<Sprite> BurntStatusImages;


    [Header("治療")]
    public SO_BattleVisualEffect HealSpritesPrefabs;

    public List<Sprite> HealImages;

    [Header("神聖保護")]
    public SO_BattleVisualEffect HolyProtectSpritesPrefabs;

    public List<Sprite> HolyProtectImages;
    public List<Sprite> HolyProtectStatusImages;


    [Header("彗星威嚇")]
    public SO_BattleVisualEffect StarThreatenSpritesPrefabs;

    public List<Sprite> StarThreatenImages;
    public List<Sprite> StarThreatenStatusImages;


    [Header("毒誓")]
    public SO_BattleVisualEffect OathSpritesPrefabs;

    public List<Sprite> OathImages;
    public List<Sprite> OathStatusImages;


    [Header("淨化")]
    public SO_BattleVisualEffect PurifySpritesPrefabs;

    public List<Sprite> PurifyImages;
    #endregion

    [Header("每幀間隔時間")]
    public Sprite EmptySprite; // 每幀間隔時間

    public float frameInterval = 0.05f; // 每幀間隔時間
    [SerializeField] private int currentFrame = 0;     // 當前幀索引

    Coroutine effectPlayer;

    public CardType test_CardType;
    public bool test_IsDamage;
    public bool test_IsApplyState;

    void Start()
    {
        Init();
        DisplayUseCardEffect.sprite = EmptySprite;
        DisplayStatusEffect.sprite = EmptySprite;
    }
    void Update()
    {
        // TestPlay(test_CardType);
    }

    /// <summary>
    /// 呼叫以播動畫
    /// </summary>
    /// <param name="cardType"></param>

    public void PlayVisualEffect(CardType cardType, bool isDamage, bool isApplyState, BattleStatusEffectType effectType)
    {
        Debug.Log($"PlayVisualEffect {cardType} {isDamage} {isApplyState} {effectType}");
        //回合施加狀態時，不會傳入卡片種類
        if (cardType == CardType.Undefined && effectType != BattleStatusEffectType.None) // 不是在用卡時觸發的，回合時觸發
        {
            Debug.Log($"PlayVisualEffect {cardType} {isDamage} {isApplyState} {effectType}");

            PlayTriggerEffectSpritesSequence(effectType, DisplayUseCardEffect, isDamage, isApplyState);
            return;
        }

        // 用卡當下施加狀態
        if (isApplyState && effectType != BattleStatusEffectType.None)
        {
            Debug.Log($"PlayVisualEffect {cardType} {isDamage} {isApplyState} {effectType}");

            PlayUseCardSpritesSequence(cardType, DisplayUseCardEffect, isDamage, isApplyState);
            return;
        }

        if (effectType != BattleStatusEffectType.None)
        {
            Debug.Log($"PlayVisualEffect {cardType} {isDamage} {isApplyState} {effectType}");

            PlayUseCardSpritesSequence(cardType, DisplayStatusEffect, isDamage, isApplyState);
            return;
        }
        // 用卡當下造成傷害
        if (isDamage)
        {
            Debug.Log($"PlayVisualEffect {cardType} {isDamage} {isApplyState} {effectType}");
            PlayUseCardSpritesSequence(cardType, DisplayStatusEffect, isDamage, isApplyState);
            return;
        }
        else
        {
            Debug.Log($"PlayVisualEffect {cardType} {isDamage} {isApplyState} {effectType}");

            PlayUseCardSpritesSequence(cardType, DisplayStatusEffect, isDamage, isApplyState);
            return;
        }

    }

    private IEnumerator playEffectSprite(Image unitEffectPos, List<Sprite> effectSprites, float frameInter)
    {
        yield return StartCoroutine(PlayAnimation(unitEffectPos, effectSprites, frameInter));
    }

    private IEnumerator PlayAnimation(Image unitEffectPos, List<Sprite> effectSprites, float frameInter)
    {
        currentFrame = 0;
        unitEffectPos.sprite = EmptySprite;
        while (currentFrame <= effectSprites.Count)
        {

            if (currentFrame >= effectSprites.Count)
            {
                unitEffectPos.sprite = effectSprites[effectSprites.Count - 1];
                break;
            }
            unitEffectPos.sprite = effectSprites[currentFrame];
            // currentFrame = (currentFrame + 1) % effectSprites.Count;
            currentFrame++;
            yield return new WaitForSeconds(frameInter);
        }
    }
    void PlayTriggerEffectSpritesSequence(BattleStatusEffectType statusEffectType, Image effectPos, bool isDamage, bool isApplyState)
    {
        if (effectPlayer != null)
        {
            Debug.Log("StopCoroutine");

            StopCoroutine(effectPlayer);
            currentFrame = 0;
        }
        switch (statusEffectType)
        {
            case BattleStatusEffectType.Burnt:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, BurntStatusImages, frameInterval));
                    break;
                }
            case BattleStatusEffectType.Dizziness:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, DizzinessStatusImages, frameInterval));
                    break;
                }
            case BattleStatusEffectType.HolyProtect:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, HolyProtectStatusImages, frameInterval));
                    break;
                }
            case BattleStatusEffectType.Oath:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, OathStatusImages, frameInterval));
                    break;
                }
            case BattleStatusEffectType.Poisoned:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, PoisionStatusImages, frameInterval));
                    break;
                }
            case BattleStatusEffectType.StarThreaten:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, StarThreatenStatusImages, frameInterval));
                    break;
                }
            default:
                {
                    Debug.Log($"取得StatusEffectType: {statusEffectType}，無特效需撥放");
                    break;
                }


        }
    }

    void PlayUseCardSpritesSequence(CardType cardType, Image effectPos, bool isDamage, bool isApplyState)
    {
        if (effectPlayer != null)
        {
            Debug.Log("StopCoroutine");

            StopCoroutine(effectPlayer);
            currentFrame = 0;
        }
        switch (cardType)
        {
            case CardType.Attack:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, AttackImages, frameInterval));
                    break;
                }
            case CardType.Burnt:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, BurntImages, frameInterval));
                    break;
                }
            case CardType.Dizziness:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, DizzinessImages, frameInterval));
                    break;
                }
            case CardType.Heal:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, HealImages, frameInterval));
                    break;
                }
            case CardType.HeavyAttack:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, HeavyAttackImages, frameInterval));
                    break;
                }
            case CardType.HolyProtect:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, HolyProtectImages, frameInterval));
                    break;
                }
            case CardType.LazerGun:
                {
                    if (isDamage)
                    {
                        effectPlayer = StartCoroutine(playEffectSprite(effectPos, LaserGunImages, frameInterval));
                    }
                    else
                    {
                        effectPlayer = StartCoroutine(playEffectSprite(effectPos, HealImages, frameInterval));
                    }
                    break;
                }

            case CardType.Oath:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, OathImages, frameInterval));
                    break;
                }

            case CardType.Poisoned:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, PoisionImages, frameInterval));
                    break;
                }
            case CardType.Purify:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, PurifyImages, frameInterval));
                    break;
                }
            case CardType.Recover:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, RecoverImages, frameInterval));
                    break;
                }
            case CardType.StarThreaten:
                {
                    effectPlayer = StartCoroutine(playEffectSprite(effectPos, StarThreatenImages, frameInterval));
                    break;
                }
            default:
                {
                    Debug.Log($"取得CardType: {cardType}，無特效需撥放");
                    break;
                }


        }

    }

    void TestPlay(CardType cardType)
    {
        if (Input.GetKeyDown(KeyCode.A))
        {
            PlayUseCardSpritesSequence(cardType, DisplayUseCardEffect, test_IsDamage, test_IsApplyState);
        }

    }

    void Init()
    {
        if (DisplayUseCardEffect == null)
            Debug.LogError($"DisplayUseCardEffect 為 Null 請檢查物件是否有引用。");

        if (DisplayStatusEffect == null)
        {
            Debug.LogError($"DisplayStatusEffect 為 Null 請檢查物件是否有引用。");
        }


        if (AttackSpritesPrefabs == null)
        {
            Debug.LogError($"AttackSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            AttackImages.Clear();
            AttackImages.AddRange(AttackSpritesPrefabs.UseCardEffectSprites);
        }




        if (HeavyAttackSpritesPrefabs == null)
        {
            Debug.LogError($"HeavyAttackSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            HeavyAttackImages.Clear();
            HeavyAttackImages.AddRange(HeavyAttackSpritesPrefabs.UseCardEffectSprites);
        }


        if (LaserGunSpritesPrefabs == null)
        {
            Debug.LogError($"LaserGunSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            LaserGunImages.Clear();
            LaserGunImages.AddRange(LaserGunSpritesPrefabs.UseCardEffectSprites);
        }


        if (PoisionSpritesPrefabs == null)
        {
            Debug.LogError($"PoisionSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            PoisionImages.Clear();
            PoisionImages.AddRange(PoisionSpritesPrefabs.UseCardEffectSprites);
            PoisionStatusImages.AddRange(PoisionSpritesPrefabs.EffectSprites);
        }


        if (DizzinessSpritesPrefabs == null)
        {
            Debug.LogError($"DizzinessSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            DizzinessImages.Clear();
            DizzinessImages.AddRange(DizzinessSpritesPrefabs.UseCardEffectSprites);
            DizzinessStatusImages.AddRange(DizzinessSpritesPrefabs.EffectSprites);

        }


        if (LaserGunSpritesPrefabs == null)
        {
            Debug.LogError($"LaserGunSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            LaserGunImages.Clear();
            LaserGunImages.AddRange(LaserGunSpritesPrefabs.UseCardEffectSprites);
        }

        if (RecoverSpritesPrefabs == null)
        {
            Debug.LogError($"RecoverSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            RecoverImages.Clear();
            RecoverImages.AddRange(RecoverSpritesPrefabs.UseCardEffectSprites);
        }

        if (BurntSpritesPrefabs == null)
        {
            Debug.LogError($"BurntSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            BurntImages.Clear();
            BurntImages.AddRange(BurntSpritesPrefabs.UseCardEffectSprites);
            BurntStatusImages.AddRange(BurntSpritesPrefabs.EffectSprites);

        }


        if (HealSpritesPrefabs == null)
        {
            Debug.LogError($"HealSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            HealImages.Clear();
            HealImages.AddRange(HealSpritesPrefabs.UseCardEffectSprites);
        }


        if (HolyProtectSpritesPrefabs == null)
        {
            Debug.LogError($"HolyProtectSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            HolyProtectImages.Clear();
            HolyProtectImages.AddRange(HolyProtectSpritesPrefabs.UseCardEffectSprites);
            HolyProtectStatusImages.AddRange(HolyProtectSpritesPrefabs.EffectSprites);

        }


        if (StarThreatenSpritesPrefabs == null)
        {
            Debug.LogError($"StarThreatenSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            StarThreatenImages.Clear();
            StarThreatenImages.AddRange(StarThreatenSpritesPrefabs.UseCardEffectSprites);
            StarThreatenStatusImages.AddRange(StarThreatenSpritesPrefabs.EffectSprites);

        }



        if (OathSpritesPrefabs == null)
        {
            Debug.LogError($"OathSpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            OathImages.Clear();
            OathImages.AddRange(OathSpritesPrefabs.UseCardEffectSprites);
            OathStatusImages.AddRange(OathSpritesPrefabs.EffectSprites);

        }


        if (PurifySpritesPrefabs == null)
        {
            Debug.LogError($"PurifySpritesPrefabs 為 Null 請檢查物件是否有引用。");
        }
        else
        {
            PurifyImages.Clear();
            PurifyImages.AddRange(PurifySpritesPrefabs.UseCardEffectSprites);

        }

    }
}
