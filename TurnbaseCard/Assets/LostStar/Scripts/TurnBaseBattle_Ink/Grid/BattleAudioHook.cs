using UnityEngine;

/// <summary>
/// 戰鬥音訊掛件：開戰時把 BGM 切成戰鬥 BGM，戰鬥結束還原原本的 BGM（地圖/商店）。
/// 透過 <see cref="AudioDirector"/> 的單一 BGM 頻道＋堆疊來做，確保不與地圖 BGM 疊音。
///
/// 兩條開戰路徑（漫遊敵人碰撞、BossCombat 事件格）都會經由 <see cref="MapFlowController"/>
/// 統一觸發 <see cref="OnBattleStarted"/> / <see cref="OnBattleEnded"/>，所以兩種戰鬥都會換 BGM。
///
/// 用法：掛到場上任一 GameObject（通常與其他 MapFlowHook 同物件），指定 <see cref="battleBGM"/>。
/// 由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class BattleAudioHook : MapFlowHookBase
{
    [Tooltip("戰鬥背景音樂")]
    [SerializeField] private AudioClip battleBGM;

    private bool pushed;   // 保險：只有真的 Push 過才 Pop，避免堆疊不平衡把 BGM 關掉

    public override void OnBattleStarted()
    {
        var audio = AudioDirector.Instance;
        if (audio != null && battleBGM != null)
        {
            audio.PushBGM(battleBGM);   // 切戰鬥 BGM（記住原本那首的位置）
            pushed = true;
        }
    }

    public override void OnBattleEnded(bool playerWin)
    {
        var audio = AudioDirector.Instance;
        if (audio != null && pushed)
        {
            audio.PopBGM();             // 還原原本的 BGM（地圖/商店）
            pushed = false;
        }
    }
}
