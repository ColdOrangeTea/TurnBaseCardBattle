using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.GlobalEnums.BattleEnum;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：測試開戰啟動器，由 A_Good_Ink 使用 AI 生成的重構版本。
    ///
    /// 比照舊的 BattleExampleBootstrap + BattleButtonFunction.Test_OpenBattle：
    /// Play 後等一影格（確保各元件初始化完成），組出一份 <see cref="SetBattleSetting"/> 並呼叫
    /// <see cref="BattleController.StartBattle"/> 自動開一場測試戰鬥。
    /// </summary>
    public class BattleV2Bootstrap : MonoBehaviour
    {
        [Tooltip("要驅動的 V2 大腦；留空會自動在場上尋找")]
        [SerializeField] private BattleController controller;

        [Header("測試戰鬥設定")]
        [SerializeField] private CharacterType playerType = CharacterType.Seraphis;
        [SerializeField] private EnemyType enemyType = EnemyType.Yarn;
        [SerializeField] private bool isPlayerFirst = true;
        [SerializeField] private int backgroundIndex = 0;
        [SerializeField] private bool autoStart = true;

        private IEnumerator Start()
        {
            if (!autoStart) yield break;

            yield return null; // 等一影格，確保 BattleController / 子系統都初始化完

            if (controller == null) controller = FindAnyObjectByType<BattleController>();
            if (controller == null)
            {
                Debug.LogWarning("[BattleV2Bootstrap] 找不到 BattleController，無法自動開始戰鬥。");
                yield break;
            }

            Debug.Log("[BattleV2Bootstrap] 自動開始 V2 測試戰鬥");
            controller.StartBattle(BuildSetting());

            // 再等一影格讓卡片抽好、版面就緒，播放進場演出。
            // 重要：BattleScreen.Start() 會把 4 張卡的 CanvasGroup.alpha 設為 0（隱形、不可點），
            // 必須呼叫 SceenAni() 讓它們淡入，否則卡片無法操作。
            yield return null;
            var battleScreen = FindAnyObjectByType<BattleScreen>(FindObjectsInactive.Include);
            if (battleScreen != null)
            {
                Debug.Log("[BattleV2Bootstrap] 播放戰鬥進場演出 SceenAni()");
                battleScreen.SceenAni();
            }
            else
            {
                Debug.LogWarning("[BattleV2Bootstrap] 找不到 BattleScreen，卡片可能維持隱形（alpha=0）。");
            }
        }

        /// <summary>組一份測試用的戰鬥設定（玩家 + 敵人）。</summary>
        private SetBattleSetting BuildSetting()
        {
            TurnBaseBattlePlayerData player = new TurnBaseBattlePlayerData().InitPlayerInfo(playerType);

            TurnBaseBattleEnemyData enemy = new TurnBaseBattleEnemyData().InitEnemyInfo(enemyType);
            enemy.BaseEnemyType = enemyType; // 確保敵人 AI 查得到行為表

            List<TurnBaseBattleUnitData> units = new List<TurnBaseBattleUnitData>() { player, enemy };
            List<bool> gameModes = new List<bool>() { true, false, false }; // 劇情模式

            return new SetBattleSetting(isPlayerFirst, false, units, backgroundIndex, 0, gameModes);
        }
    }
}
