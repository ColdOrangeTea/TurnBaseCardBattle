using System.Collections;
using UnityEngine;

/// <summary>
/// 地圖流程的「表現掛載點」基底：預設全是空實作，子類別只覆寫需要的階段，
/// 用來插入動畫 / UI 顯示 / 劇情 / 音效等表現，而不必改動核心流程（<see cref="MapFlowController"/>）。
///
/// 設計重點：
///   - 會「花時間、要播完才繼續」的階段回傳 <see cref="IEnumerator"/>（controller 會 yield 等它播完）；
///   - 瞬間、射後不理的階段用 void。
///   - 鐵律：Hook 只做「表現」，不要在裡面決定遊戲邏輯（要不要開戰、給不給獎勵）。
///
/// 由 A_Good_Ink 使用 AI 生成。
/// </summary>
public abstract class MapFlowHookBase : MonoBehaviour
{
    // ── 會等待的階段（預設 yield break＝空）──
    /// <summary>開始移動前。</summary>
    public virtual IEnumerator OnBeforeMove(Transform target) { yield break; }
    /// <summary>移動落格後。</summary>
    public virtual IEnumerator OnAfterMove(Transform landedGrid) { yield break; }
    /// <summary>事件真正開始前（此時已鎖住玩家；插入的演出會先播完才開事件）。</summary>
    public virtual IEnumerator OnBeforeEvent(NodeEventType type, NodeEvent grid) { yield break; }
    /// <summary>事件結束後、恢復流程前。</summary>
    public virtual IEnumerator OnAfterEvent(NodeEventType type, NodeEvent grid) { yield break; }

    // ── 瞬間、射後不理的階段（預設空）──
    /// <summary>回到「自由探索」狀態時。</summary>
    public virtual void OnEnterFreeControl() { }
    /// <summary>任何狀態切換時。</summary>
    public virtual void OnStateChanged(MapFlowState from, MapFlowState to) { }

    // ── 戰鬥生命週期（兩條開戰路徑：漫遊敵人碰撞、BossCombat 事件格，都會統一觸發）──
    /// <summary>戰鬥開始時（用來切戰鬥 BGM 等）。</summary>
    public virtual void OnBattleStarted() { }
    /// <summary>戰鬥結束時（playerWin＝玩家是否獲勝）。</summary>
    public virtual void OnBattleEnded(bool playerWin) { }
}
