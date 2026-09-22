using System.Collections;
using UnityEngine;

/// <summary>
/// 示範用的地圖流程掛件：踏到寶箱、真正打開之前，先播一段「光效」演出（這裡用等待＋log 代表）。
/// 展示 <see cref="MapFlowHookBase"/> 的用法——覆寫 <see cref="OnBeforeEvent"/> 回傳 IEnumerator，
/// 流程會等它播完才開寶箱。實務上把等待換成動畫/粒子/音效即可。
///
/// 用法：把本元件掛到場上任一 GameObject；<see cref="MapFlowController"/> 會自動抓到並在對應時機呼叫。
/// 這是示範，之後不需要可直接移除本元件。由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class SampleTreasureGlintHook : MapFlowHookBase
{
    [Tooltip("演出持續秒數（實際專案改成動畫時長）")]
    [SerializeField] private float glintSeconds = 0.5f;

    // 供測試/除錯觀察
    public static int TimesPlayed { get; private set; }
    public static bool IsPlaying { get; private set; }

    public override IEnumerator OnBeforeEvent(GridEventType type, NodeEvent grid)
    {
        if (type != GridEventType.Treasure) yield break; // 只管寶箱

        IsPlaying = true;
        BattleLog.Log("[SampleHook] ✨ 寶箱光效演出中…（流程等我播完才開寶箱）");
        // 這裡示範用等待代表演出；實務換成 Animator/ParticleSystem/AudioSource 播放並等它結束
        yield return new WaitForSeconds(glintSeconds);
        TimesPlayed++;
        IsPlaying = false;
    }
}
