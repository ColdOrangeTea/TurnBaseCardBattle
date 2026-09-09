using Spine.Unity;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;
using System.Collections;
using DG.Tweening;
using Spine;
public class BattleUnitSpineAnimation : MonoBehaviour
{
    // public BattleUnitProfile unitProfile; // 手動掛件
    // public RectTransform EffectDisplayPivot;
    [SerializeField]
    private Color originalColor;

    [Header("Be Attack Effect")]
    public Color HurtColor = Color.red; // 受傷時的顏色
    public float Hurt_FlashDuration = 0.12f; // 閃爍時間
    public int Hurt_FlashTimes = 1; // 閃爍的次數

    [Header("Be Poisoned Effect")]
    public Color PoisonedColor = new Color(); // 受傷時的顏色
    public float Poisoned_FlashDuration = 0.5f; // 閃爍時間
    public int Poisoned_FlashTimes = 1; // 閃爍的次數

    [Header("Be Heal Effect")]

    public Color HealColor = new Color(); // 受傷時的顏色
    public float Heal_FlashDuration = 0.2f; // 閃爍時間
    public int Heal_FlashTimes = 1; // 閃爍的次數

    [Header("Be Recover Effect")]
    public Color RecoverColor = new Color(); // 受傷時的顏色
    public float Recover_FlashDuration = 0.2f; // 閃爍時間
    public int Recover_FlashTimes = 1; // 閃爍的次數

    [Header("Be Purify Effect")]
    public Color PurifyColor = new Color(); // 受傷時的顏色
    public float Purify_FlashDuration = 0.2f; // 閃爍時間
    public int Purify_FlashTimes = 1; // 閃爍的次數

    [Header("Be Dizziness Effect")]
    public Color DizzinessColor = new Color(); // 受傷時的顏色
    public float Dizziness_FlashDuration = 0.12f; // 閃爍時間
    public int Dizziness_FlashTimes = 1; // 閃爍的次數

    [Header("LaserGun Effect")]
    public Color LaserColor = new Color(); // 受傷時的顏色
    public float Laser_FlashDuration = 0.12f; // 閃爍時間
    public int Laser_FlashTimes = 1; // 閃爍的次數

    [Header("Burnt Effect")]
    public Color BurntColor = new Color(); // 受傷時的顏色
    public float Burnt_FlashDuration = 0.12f; // 閃爍時間
    public int Burnt_FlashTimes = 1; // 閃爍的次數

    [Header("HolyProtect Effect")]
    public Color HolyProtectColor = new Color(); // 受傷時的顏色
    public float HolyProtect_FlashDuration = 0.2f; // 閃爍時間
    public int HolyProtect_FlashTimes = 1; // 閃爍的次數

    [Header("StarThreaten Effect")]
    public Color StarThreatenColor = new Color(); // 受傷時的顏色
    public float StarThreaten_FlashDuration = 0.2f; // 閃爍時間
    public int StarThreaten_FlashTimes = 1; // 閃爍的次數

    [Header("Oath Effect")]
    public Color OathColor = new Color(); // 受傷時的顏色
    public float Oath_FlashDuration = 0.2f; // 閃爍時間
    public int Oath_FlashTimes = 1; // 閃爍的次數

    [Header("Animation State")]
    public BattleUnitSpineAnimType AnimType;

    public void SwitchUnitAnimState(int trackIndex, BattleUnitSpineAnimType oriAnimState, BattleUnitSpineAnimType nextAnimState, SkeletonGraphic unitGraphic, bool isLoop)
    {
        if (unitGraphic.SkeletonData.FindAnimation(AnimType.ToString()) == null) // 確認動畫存在
        {
            Debug.LogError($"動畫 {nextAnimState.ToString()} 不存在於 SkeletonDataAsset 中！"); return;
        }

        if (nextAnimState.ToString() != unitGraphic.AnimationState.ToString())
        {
            TrackEntry trackEntry = unitGraphic.AnimationState.SetAnimation(0, nextAnimState.ToString(), isLoop); // 播放新的動畫
            trackEntry.Complete += entry =>
                {
                    // 當新動畫播放完畢後，自動切回預設動畫
                    unitGraphic.AnimationState.SetAnimation(0, oriAnimState.ToString(), true);
                    Debug.Log($"動畫 {nextAnimState.ToString()} 播放完成，切回預設動畫 {oriAnimState}");
                };
        }

        // unitGraphic.Initialize(true);
    }
    public void DisplayColorEffect(SkeletonGraphic unitGraphic, CardType cardType, bool isDamage, bool isApplyState, BattleStatusEffectType effectType)
    {
        Debug.Log("DisplayEffect觸發");
        Debug.Log($" {isDamage} {isApplyState} {effectType}DisplayEffect觸發");

        if (cardType == CardType.Undefined && effectType != BattleStatusEffectType.None) // 不是在用卡時觸發的，回合時觸發
        {
            switch (effectType)
            {
                case BattleStatusEffectType.Poisoned:
                    {
                        FlashColor(unitGraphic, Poisoned_FlashDuration, Poisoned_FlashTimes, originalColor, PoisonedColor);
                        return;
                    }
                case BattleStatusEffectType.Burnt:
                    {
                        FlashColor(unitGraphic, Burnt_FlashDuration, Burnt_FlashTimes, originalColor, BurntColor);
                        return;
                    }
            }
        }

        // 用卡當下施加狀態
        if (isApplyState && effectType != BattleStatusEffectType.None)
        {
            switch (cardType)
            {
                case CardType.Poisoned:
                    {
                        Debug.Log("DisplayEffect觸發下毒");
                        FlashColor(unitGraphic, Poisoned_FlashDuration, Poisoned_FlashTimes, originalColor, PoisonedColor);
                        return;
                    }
                case CardType.Dizziness:
                    {
                        if (isDamage)
                            FlashColor(unitGraphic, Dizziness_FlashDuration, Dizziness_FlashTimes, originalColor, DizzinessColor);
                        return;
                    }
                case CardType.Burnt:
                    {
                        FlashColor(unitGraphic, Burnt_FlashDuration, Burnt_FlashTimes, originalColor, BurntColor);
                        return;
                    }
                case CardType.HolyProtect:
                    {
                        FlashColor(unitGraphic, HolyProtect_FlashDuration, HolyProtect_FlashTimes, originalColor, HolyProtectColor);
                        return;
                    }
                case CardType.StarThreaten:
                    {
                        FlashColor(unitGraphic, StarThreaten_FlashDuration, StarThreaten_FlashTimes, originalColor, StarThreatenColor);
                        return;
                    }
                case CardType.Oath:
                    {
                        FlashColor(unitGraphic, Oath_FlashDuration, Oath_FlashTimes, originalColor, OathColor);
                        return;
                    }

            }
        }

        // 用卡當下造成傷害
        if (isDamage)
        {
            switch (cardType)
            {
                case CardType.Attack:
                    {
                        Debug.Log("DisplayEffect觸發攻擊");
                        FlashColor(unitGraphic, Hurt_FlashDuration, Hurt_FlashTimes, originalColor, HurtColor);
                        return;
                    }
                case CardType.Poisoned:
                    {
                        FlashColor(unitGraphic, Poisoned_FlashDuration, Poisoned_FlashTimes, originalColor, PoisonedColor);
                        return;
                    }
                case CardType.Dizziness:
                    {
                        FlashColor(unitGraphic, Dizziness_FlashDuration, Dizziness_FlashTimes, originalColor, DizzinessColor);
                        return;
                    }
                case CardType.HeavyAttack:
                    {
                        FlashColor(unitGraphic, Hurt_FlashDuration, Hurt_FlashTimes, originalColor, HurtColor);
                        return;
                    }
                case CardType.LazerGun:
                    {
                        FlashColor(unitGraphic, Laser_FlashDuration, Laser_FlashTimes, originalColor, LaserColor);
                        return;
                    }
                case CardType.Oath:
                    {
                        FlashColor(unitGraphic, Oath_FlashDuration, Oath_FlashTimes, originalColor, OathColor);
                        return;
                    }
            }
        }
        else
        {
            switch (cardType) // 影響數值不為0 可能是補血
            {
                case CardType.Burnt:
                    {
                        FlashColor(unitGraphic, Burnt_FlashDuration, Burnt_FlashTimes, originalColor, BurntColor);
                        return;
                    }
                case CardType.HolyProtect:
                    {
                        FlashColor(unitGraphic, HolyProtect_FlashDuration, HolyProtect_FlashTimes, originalColor, HolyProtectColor);
                        return;
                    }
                case CardType.StarThreaten:
                    {
                        FlashColor(unitGraphic, StarThreaten_FlashDuration, StarThreaten_FlashTimes, originalColor, StarThreatenColor);
                        return;
                    }
                case CardType.LazerGun:
                    {
                        FlashColor(unitGraphic, Heal_FlashDuration, Heal_FlashTimes, originalColor, HealColor);
                        // FlashColor(unitGraphic, Hurt_FlashDuration, Hurt_FlashTimes, originalColor, HurtColor);
                        return;
                    }
                case CardType.Recover:
                    {
                        FlashColor(unitGraphic, Recover_FlashDuration, Recover_FlashTimes, originalColor, RecoverColor);
                        return;
                    }
                case CardType.Heal:
                    {
                        // PlayPS(ps, EffectDisplayPivot);
                        FlashColor(unitGraphic, Heal_FlashDuration, Heal_FlashTimes, originalColor, HealColor);
                        return;
                    }
                case CardType.Purify:
                    {
                        FlashColor(unitGraphic, Purify_FlashDuration, Purify_FlashTimes, originalColor, PurifyColor);
                        return;
                    }
            }
        }
    }



    #region FlashColor

    void FlashColor(SkeletonGraphic unitGraphic, float duration, int flashTimes, Color oriColor, Color nextColor)
    {
        Debug.Log("DisplayEffect觸發攻擊閃爍");

        if (!unitGraphic.IsValid)
            unitGraphic.Initialize(true);
        Sequence sequence = DOTween.Sequence()  // 設置為紅色並閃爍回原始顏色
            .Append(unitGraphic.DOColor(nextColor, duration)) // 改變為紅色
            .Append(unitGraphic.DOColor(oriColor, duration)) // 恢復原色
            .SetLoops(flashTimes); // 重複多次
    }

    #endregion

    public void SetOriginSpineAnimColor(Color oriColor) => originalColor = oriColor; // 將該Unit的原始顏色保存
    public void ResetUnitAnimColor(SkeletonGraphic unitGraphic, Color oriColor) => unitGraphic.color = oriColor; // 重製顏色
    public void SetUnitAnimState(int trackIndex, SkeletonGraphic unitGraphic, BattleUnitSpineAnimType animState, bool isLoop)
    {
        unitGraphic.AnimationState.SetAnimation(trackIndex, animState.ToString(), isLoop);
        unitGraphic.Initialize(true);
    }


}
