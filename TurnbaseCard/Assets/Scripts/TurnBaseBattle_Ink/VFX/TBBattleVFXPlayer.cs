using Spine.Unity;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;
using System.Collections;
using DG.Tweening;
using Spine;

public class TBBattleVFXPlayer : MonoBehaviour
{
    [Header("程式腳本")]

    public BattleUnitSpineAnimation battleUnitSpineAnimation;
    public SpriteSequencePlayer spriteSequencePlayer;

    public void GetOriginSpineAnimColor(Color originalUnitAnimColor)
    {
        battleUnitSpineAnimation.SetOriginSpineAnimColor(originalUnitAnimColor);
    }
    public void SetUnitOriginAnimState(int trackIndex, SkeletonGraphic unitGraphic, BattleUnitSpineAnimType animState, bool isLoop)
    {
        battleUnitSpineAnimation.SetUnitAnimState(0, unitGraphic, animState, isLoop);
    }
    /// <summary>
    /// 切換Spine動畫的狀態
    /// </summary>
    /// <param name="unitGraphic"></param>
    /// <param name="cardType"></param>
    /// <param name="effectType"></param>
    /// <param name="isDamage"></param>
    /// <param name="isApplyState"></param>
    public void SwitchUnitSpineAnimState(SkeletonGraphic unitGraphic, CardType cardType, BattleStatusEffectType effectType,
    bool isDamage, bool isApplyState)
    {
        if (isDamage)
        {
            battleUnitSpineAnimation.SwitchUnitAnimState
            (
                0, BattleUnitSpineAnimType.Idle, BattleUnitSpineAnimType.Hurt, unitGraphic, false
            );

        }
        else
        {
            battleUnitSpineAnimation.SwitchUnitAnimState
            (
                0, BattleUnitSpineAnimType.Idle, BattleUnitSpineAnimType.Idle, unitGraphic, true
            );
        }
    }

    /// <summary>
    /// 顯示特效和變色
    /// </summary>
    /// <param name="unitGraphic"></param>
    /// <param name="cardType"></param>
    /// <param name="effectType"></param>
    /// <param name="isDamage"></param>
    /// <param name="isApplyState"></param>
    public void PlayVFXOnUnit(SkeletonGraphic unitGraphic,
    CardType cardType, BattleStatusEffectType effectType, bool isDamage, bool isApplyState)
    {
        battleUnitSpineAnimation.DisplayColorEffect(unitGraphic, cardType, isDamage, isApplyState, effectType);
        spriteSequencePlayer.PlayVisualEffect(cardType, isDamage, isApplyState, effectType);
    }

}
