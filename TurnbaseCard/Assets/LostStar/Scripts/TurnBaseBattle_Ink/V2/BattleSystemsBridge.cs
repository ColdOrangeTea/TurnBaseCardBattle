using System.Collections;
using UnityEngine;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：戰鬥「子系統橋接」的抽象基底，由 A_Good_Ink 使用 AI 生成的重構版本。
    ///
    /// 用途：把「目前還綁在舊 manager/UI/事件上的子系統」（骰子 S001、抽牌 S002、
    /// 用卡計算 BattleAction、敵人 AI）全部隔離在這個橋接介面後面。
    /// BattleController 只認識這個乾淨介面，不直接碰那些糾纏的舊子系統，
    /// 所有殘餘耦合都集中在 Chunk 4 的具體實作子類別裡，方便日後一次抽乾淨。
    ///
    /// Chunk 3 階段：BattleController 以 null-safe 方式呼叫本橋接，
    /// 因此「未指派橋接」時回合流程仍可正常運轉（只是沒有骰子/卡片表現），可先驗證狀態機。
    /// Chunk 4 階段：提供一個繼承本類別的具體實作，接上骰子/抽牌/計算。
    /// </summary>
    public abstract class BattleSystemsBridge : MonoBehaviour
    {
        /// <summary>戰鬥開始時呼叫一次，讓子系統取得 Controller 參考並做初始化。</summary>
        public virtual void OnBattleStart(BattleController controller) { }

        /// <summary>某位行動者回合開始：擲骰、抽牌、重置投放區等「準備」動作。</summary>
        public virtual void PrepareTurn(BattleUnit current, BattleUnit opponent) { }

        /// <summary>
        /// 讀取並清除「這回合是否被跳過」旗標（例如暈眩）。
        /// Controller 於 PrepareTurn 後呼叫；回傳 true 代表當前行動者這回合直接跳過。
        /// </summary>
        public virtual bool ConsumeSkipFlag() => false;

        /// <summary>某位行動者回合結束：清除場上骰子、結算回合結束效果等。</summary>
        public virtual void EndTurn(BattleUnit current, BattleUnit opponent) { }

        /// <summary>
        /// 敵人回合的 AI 行動（協程）：逐張出牌、每張之間停頓展示特效/音效。
        /// Controller 會 yield 等它跑完。
        /// </summary>
        public virtual IEnumerator RunEnemyTurn(BattleUnit enemy, BattleUnit target) { yield break; }
    }
}
