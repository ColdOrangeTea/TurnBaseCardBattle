using System;
using UnityEngine;
using UnityEngine.Events;
using Assets.Scripts.GlobalEnums.BattleEnum;

/// <summary>
/// 關卡 BOSS 擊殺判定（重構版，由 A_Good_Ink 使用 AI 生成）。
///
/// 用途：偵測「這一關的最後 BOSS 是否被殺掉了」。
/// 作法：訂閱 <see cref="MapTurnBaseManager.EnemyDefeated"/>（地圖敵人於戰鬥勝利、即將被移除時觸發），
///       若被擊敗的敵人型別符合本元件設定的 <see cref="bossEnemyType"/>，即判定 BOSS 已被擊殺，
///       設定 <see cref="IsBossDefeated"/>＝true、觸發 <see cref="BossDefeated"/> 事件與可在 Inspector
///       綁定的 <see cref="onBossDefeated"/>（例如接關卡通關流程／播放結算）。只會判定一次。
///
/// 與舊版差異：不再依賴 V2 重構已移除的 TurnBaseBattleUnitDisplayData／即時讀 HP；改以「敵人被擊敗事件＋
/// 型別比對」判定，脫離已移除系統，也不必每幀輪詢。
/// </summary>
public class BossHPDetect : MonoBehaviour
{
    [Header("BOSS 判定")]
    [Tooltip("這一關的 BOSS 敵人型別；被擊敗的敵人為此型別時＝BOSS 被殺。")]
    [SerializeField] private EnemyType bossEnemyType = EnemyType._Boss_;

    [Tooltip("BOSS 被擊殺時要觸發的動作（可在 Inspector 綁定，例如開啟通關 UI／切換關卡）。")]
    public UnityEvent onBossDefeated;

    /// <summary>本關 BOSS 是否已被擊殺（判定後恆為 true）。</summary>
    public bool IsBossDefeated { get; private set; }

    /// <summary>BOSS 被擊殺時觸發（程式訂閱用；只會觸發一次）。</summary>
    public event Action BossDefeated;

    private void OnEnable()
    {
        MapTurnBaseManager.EnemyDefeated += OnEnemyDefeated;
    }

    private void OnDisable()
    {
        MapTurnBaseManager.EnemyDefeated -= OnEnemyDefeated;
    }

    private void OnEnemyDefeated(Enemy enemy)
    {
        if (IsBossDefeated || enemy == null) return;
        if (enemy.enemyType != bossEnemyType) return; // 不是本關 BOSS，忽略

        IsBossDefeated = true;
        Debug.Log($"[BossHPDetect] 關卡 BOSS（{bossEnemyType}）已被擊殺。");
        BossDefeated?.Invoke();
        onBossDefeated?.Invoke();
    }
}
