using UnityEngine;

/// <summary>
/// 戰鬥音訊掛件：把整場戰鬥的 BGM 都交給 <see cref="AudioDirector"/> 的單一頻道管理，永不與地圖 BGM 疊音。
///   - 開戰：切戰鬥 BGM（PushBGM，記住原本地圖/商店 BGM 的位置）。
///   - 戰鬥結束（結算畫面）：換成勝利／失敗音樂（ReplaceBGM，堆疊不變）。
///   - 回到地圖（自由控制）：還原原本的 BGM（PopBGM）。
///
/// 兩條開戰路徑（漫遊敵人碰撞、BossCombat 事件格）都經 <see cref="MapFlowController"/> 統一觸發，
/// 所以兩種戰鬥都會換 BGM。由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class BattleAudioHook : MapFlowHookBase
{
    [Tooltip("戰鬥背景音樂")]
    [SerializeField] private AudioClip battleBGM;
    [Tooltip("結算：勝利音樂")]
    [SerializeField] private AudioClip victoryBGM;
    [Tooltip("結算：失敗音樂")]
    [SerializeField] private AudioClip loseBGM;

    private bool pushed;   // 保險：只有真的 Push 過才 Replace/Pop，避免堆疊不平衡把 BGM 關掉

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
        if (audio == null || !pushed) return;
        AudioClip settlement = playerWin ? victoryBGM : loseBGM;
        if (settlement != null) audio.ReplaceBGM(settlement);   // 結算畫面播勝/敗音樂（不動堆疊）
    }

    public override void OnEnterFreeControl()
    {
        var audio = AudioDirector.Instance;
        if (audio != null && pushed)
        {
            audio.PopBGM();             // 回到地圖：還原原本的 BGM（地圖/商店）
            pushed = false;
        }
    }
}
