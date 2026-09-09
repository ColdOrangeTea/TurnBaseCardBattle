using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 對話框底部按鈕（跳過 / 自動 / 紀錄）的通用控制器：
/// 由外部注入狀態讀寫方法，按下時切換狀態並播放對應的開關動畫。
/// </summary>
public class BottomButtonController : MonoBehaviour
{
    [Header("動畫設定")]
    public string startAnim = "Bottom_Btn_On_Start";
    public string loopAnim = "Bottom_Btn_On_Loop";
    public string endAnim = "Bottom_Btn_On_End";

    [Header("外部控制")]
    // 狀態存取方法（由外部注入，見 TriggerDialogue.ButtonBind）
    public Func<bool> getState;
    public Action<bool> setState;

    private Animation anim;

    private void Awake()
    {
        anim = GetComponent<Animation>();
        if (anim == null)
        {
            Debug.LogWarning($"{name} 缺少 Animation 組件，按鈕動畫將無法播放。");
        }
    }

    /// <summary>
    /// 當按下按鈕時呼叫：切換外部狀態並播放對應動畫。
    /// </summary>
    public void OnButtonPressed()
    {
        if (getState == null || setState == null)
        {
            Debug.LogWarning($"{name} 尚未綁定 getState / setState。");
            return;
        }

        bool nextState = !getState();

        // 切換外部狀態
        setState(nextState);

        if (anim == null) return;

        // 播放對應動畫
        if (nextState)
        {
            // 如果有設定 loop 動畫名稱，就執行 Start → Loop
            if (!string.IsNullOrEmpty(loopAnim))
                StartCoroutine(PlayAnimationThenLoop(startAnim, loopAnim));
            else
                anim.Play(startAnim); // 沒有 loop，單純播 start
        }
        else
        {
            anim.Play(endAnim);
        }
    }

    /// <summary>播放第一段動畫，結束後接著播放循環動畫。</summary>
    private IEnumerator PlayAnimationThenLoop(string firstClip, string loopClip)
    {
        anim.Play(firstClip);
        while (anim.isPlaying)
        {
            yield return null;
        }
        anim.Play(loopClip);
    }
}
