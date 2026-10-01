using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum; // EnemyType / Enemy

/// <summary>
/// 任務追蹤（由 A_Good_Ink 使用 AI 生成）。接任務後登記為「進行中」，訂閱遊戲事件判定完成條件，
/// 達成時呼叫場上的 <see cref="QuestController.CompleteQuest(QuestData)"/>（跳 QuestResult 領獎面板＋發獎）。
///
/// 設計為跨場景常駐單例（與 <see cref="LevelMapInitializer"/> 一致），讓進行中的任務跨關卡保留。
/// 條件以 <see cref="QuestConditionType"/> 分流；目前實作「擊敗指定類型敵人(DefeatEnemyType)」——
/// 訂閱 <see cref="MapTurnBaseManager.EnemyDefeated"/>，被擊敗敵人型別符合任務 targetEnemyType 即完成。
/// 未來條件（連勝 N 場 / 限定牌打贏）：在此加對應事件訂閱與判定即可（WinWithOnlyCards 需先有出牌事件）。
/// </summary>
public class QuestTracker : MonoBehaviour
{
    public static QuestTracker Instance { get; private set; }

    // 進行中的任務（已接、尚未完成）
    private readonly List<QuestData> active = new List<QuestData>();

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[QuestTracker] 已存在常駐實例，移除重複的：{name}");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        if (transform.parent != null) transform.SetParent(null); // DontDestroyOnLoad 需為根物件
        DontDestroyOnLoad(gameObject);
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable() { MapTurnBaseManager.EnemyDefeated += OnEnemyDefeated; }
    private void OnDisable() { MapTurnBaseManager.EnemyDefeated -= OnEnemyDefeated; }

    /// <summary>接任務時登記為進行中（由 QuestController.Accept 呼叫）。</summary>
    public void RegisterActiveQuest(QuestData quest)
    {
        if (quest == null || active.Contains(quest)) return;
        active.Add(quest);
        Debug.Log($"[QuestTracker] 登記進行中任務：{quest.questName}（條件 {quest.conditionType}）");
    }

    /// <summary>目前是否有進行中的指定任務。</summary>
    public bool IsActive(QuestData quest) => quest != null && active.Contains(quest);

    // 擊敗敵人：比對進行中任務裡的 DefeatEnemyType 條件
    private void OnEnemyDefeated(Enemy enemy)
    {
        if (enemy == null || active.Count == 0) return;
        for (int i = active.Count - 1; i >= 0; i--)
        {
            var q = active[i];
            if (q == null) { active.RemoveAt(i); continue; }
            if (q.conditionType == QuestConditionType.DefeatEnemyType && enemy.enemyType == q.targetEnemyType)
            {
                active.RemoveAt(i);
                Complete(q);
            }
        }
    }

    private void Complete(QuestData quest)
    {
        Debug.Log($"[QuestTracker] 任務完成條件達成：{quest.questName}");
        var qc = Object.FindAnyObjectByType<QuestController>(FindObjectsInactive.Include);
        if (qc != null) qc.CompleteQuest(quest);
        else Debug.LogWarning("[QuestTracker] 場上找不到 QuestController，無法顯示完成面板（獎勵未發）。");
    }
}
