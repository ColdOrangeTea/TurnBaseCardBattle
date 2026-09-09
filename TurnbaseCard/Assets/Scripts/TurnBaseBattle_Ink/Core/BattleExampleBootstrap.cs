using System.Collections;
using UnityEngine;

/// <summary>
/// 戰鬥範例場景的啟動器（此腳本由 A_Good_Ink 使用 AI 生成）。
/// 掛在範例場景中，Play 後等一個影格（確保 TurnBaseBattleManager 等都完成初始化），
/// 再呼叫 <see cref="BattleButtonFunction.Test_OpenBattle"/> 自動開一場測試戰鬥，方便觀察整套腳本如何運作。
/// 若場上已有「開始戰鬥」按鈕，也可以關掉 autoStart 改用手動點擊。
/// </summary>
public class BattleExampleBootstrap : MonoBehaviour
{
    [Tooltip("Play 後是否自動開始一場測試戰鬥")]
    public bool autoStart = true;

    [Tooltip("要驅動的按鈕功能腳本；留空會自動在場上尋找")]
    public BattleButtonFunction battleButtonFunction;

    IEnumerator Start()
    {
        if (!autoStart) yield break;

        // 等一個影格，確保所有 Awake/Start（含 TurnBaseBattleManager 的初始化）都跑完
        yield return null;

        if (battleButtonFunction == null)
            battleButtonFunction = FindAnyObjectByType<BattleButtonFunction>();

        if (battleButtonFunction == null)
        {
            Debug.LogWarning("[BattleExampleBootstrap] 找不到 BattleButtonFunction，無法自動開始戰鬥。");
            yield break;
        }

        Debug.Log("[BattleExampleBootstrap] 自動開始測試戰鬥");
        battleButtonFunction.Test_OpenBattle();

        // 再等一個影格讓卡片抽好、版面就緒，播放進場演出（BattleScreen 會把卡片淡入到定位）
        yield return null;
        var battleScreen = FindAnyObjectByType<BattleScreen>(FindObjectsInactive.Include);
        if (battleScreen != null)
        {
            Debug.Log("[BattleExampleBootstrap] 播放戰鬥進場演出 SceenAni()");
            battleScreen.SceenAni();
        }
    }
}
