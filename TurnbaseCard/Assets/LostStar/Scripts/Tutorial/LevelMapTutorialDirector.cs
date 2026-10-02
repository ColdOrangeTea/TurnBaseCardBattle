using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TurnBaseBattleV2;

/// <summary>
/// 地圖新手教學／劇情對話導演（由 A_Good_Ink 使用 AI 生成）。
///
/// 依「第一次發生」的時機播放指定的對話（DialogueData），全部只播一次：
///   - 第一次進入某 Stage（例：L1-Stage1-1 → LostStarL1-1、L1-1-1；L1-Stage1-2 → L1-2）
///   - 第一次接下任務（任務面板按「接受」並關閉後）
///   - 第一次碰到敵人（撞上後、開戰前）
///   - 教學戰（第一場戰鬥）：開戰時播對話；玩家第一次出牌後播一段；第一個回合結束、換手前再播一段，之後結束教學
///   - 教學戰勝利、回到地圖後（任務完成領獎面板之前）
///   - 第一次開寶箱（寶箱面板關閉後）
///
/// 接法：掛在場上任一常駐物件即可。本元件是 <see cref="MapFlowHookBase"/>，由 MapFlowController
/// 自動收集並在「開戰前／事件後／戰後回地圖」等時機 yield 等它播完；Stage 進入與戰鬥回合則直接訂閱
/// <see cref="LevelMapManager.StageEntered"/>、<see cref="BattleController.BattleStarted"/>／回合結束插播。
/// 對話期間會暫扣地圖點擊（<see cref="S001_PlayerController.PushInputHold"/>）；戰鬥中則由對話 UI 的全螢幕底板擋住操作。
/// </summary>
public class LevelMapTutorialDirector : MapFlowHookBase
{
    /// <summary>一組依序播放的對話（可多段），並可選擇段與段之間要不要收起對話 UI。</summary>
    [Serializable]
    public class DialogueSequence
    {
        [Tooltip("依序播放的對話段落")]
        public List<DialogueData> dialogues = new List<DialogueData>();
        [Tooltip("勾選＝段與段之間不收起 UI，播完一段直接接下一段到全部結束（跳過＝跳過剩下全部）；\n" +
                 "不勾＝每段播完先淡出收起 UI，再開下一段（跳過＝只跳過目前這段）。")]
        public bool keepUIOpenBetween = false;

        public bool HasAny
        {
            get
            {
                if (dialogues == null) return false;
                foreach (var d in dialogues) if (d != null) return true;
                return false;
            }
        }
    }

    [Serializable]
    public class StageDialogue
    {
        [Tooltip("第一次進入這個 Stage 時播放")]
        public StageInfo stage;
        public DialogueSequence sequence = new DialogueSequence();
    }

    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private TriggerDialogue dialogue;
    [SerializeField] private LevelMapManager levelMap;
    [SerializeField] private S001_PlayerController player;

    [Header("第一次進入 Stage")]
    [SerializeField] private List<StageDialogue> stageDialogues = new List<StageDialogue>();
    [Tooltip("進入 Stage 後等多久再開始對話（等鏡頭/轉場就位）")]
    [SerializeField][Min(0f)] private float stageDialogueDelay = 0.6f;

    [Header("地圖事件")]
    [Tooltip("第一次接下任務（關閉任務面板後）")]
    [SerializeField] private DialogueSequence firstQuestAccepted = new DialogueSequence();
    [Tooltip("第一次碰到敵人（撞上後、進戰鬥前）")]
    [SerializeField] private DialogueSequence firstEnemyEncounter = new DialogueSequence();
    [Tooltip("第一次開寶箱（寶箱面板關閉後）")]
    [SerializeField] private DialogueSequence firstTreasure = new DialogueSequence();

    [Header("教學戰（第一場戰鬥）")]
    [Tooltip("開戰時（卡片進場後）")]
    [SerializeField] private DialogueSequence tutorialBattleStart = new DialogueSequence();
    [Tooltip("開戰後等多久再播（等卡片進場演出）")]
    [SerializeField][Min(0f)] private float tutorialBattleStartDelay = 1.2f;
    [Tooltip("教學戰中，玩家第一次用骰子打出卡片後")]
    [SerializeField] private DialogueSequence tutorialFirstCardUsed = new DialogueSequence();
    [Tooltip("出牌後等多久再播（讓玩家先看到卡片效果/特效）")]
    [SerializeField][Min(0f)] private float tutorialFirstCardDelay = 0.8f;
    [Tooltip("玩家第一個回合結束、換敵人前；播完即結束教學")]
    [SerializeField] private DialogueSequence tutorialFirstTurnEnd = new DialogueSequence();
    [Tooltip("教學戰勝利、回到地圖後（任務完成領獎面板之前）")]
    [SerializeField] private DialogueSequence tutorialBattleWon = new DialogueSequence();

    [Header("進度保存")]
    [Tooltip("勾選＝「已播過」記在 PlayerPrefs，重開遊戲也不再播；不勾＝每次 Play 都從頭（測試方便）。")]
    [SerializeField] private bool persistProgress = false;
    [SerializeField] private string saveKeyPrefix = "Tutorial_L1_";

    // 進度旗標鍵
    private const string KeyQuest = "FirstQuest";
    private const string KeyEncounter = "FirstEncounter";
    private const string KeyTreasure = "FirstTreasure";
    private const string KeyBattle = "TutorialBattle";
    private const string KeyBattleFirstCard = "TutorialBattleFirstCard";
    private const string KeyBattleTurnEnd = "TutorialBattleTurnEnd";
    private const string KeyBattleWon = "TutorialBattleWon";

    private readonly HashSet<string> done = new HashSet<string>();
    private bool busy;                     // 同時只播一組對話
    private bool tutorialBattleActive;     // 教學戰進行中（第一回合結束前）
    private bool wonDialoguePending;       // 教學戰打過、還沒播勝利後對話
    private bool cardDialoguePending;      // 「第一次出牌」對話等待/播放中（回合結束插播要等它播完）
    private BattleController subscribedBattle;
    private Func<BattleUnit, IEnumerator> turnEndInterlude;

    private void Awake()
    {
        if (dialogue == null) dialogue = FindAnyObjectByType<TriggerDialogue>(FindObjectsInactive.Include);
        if (levelMap == null) levelMap = FindAnyObjectByType<LevelMapManager>();
        if (player == null) player = FindAnyObjectByType<S001_PlayerController>();
        if (dialogue == null) Debug.LogWarning($"[{name}] LevelMapTutorialDirector 找不到 TriggerDialogue（對話 UI），教學對話將不會播放。");
        turnEndInterlude = OnBattleTurnEnded;
    }

    private void OnEnable()
    {
        // LevelMapManager.Start 才進入起始 Stage，OnEnable 一定早於它，故開場那次也收得到
        if (levelMap != null) levelMap.StageEntered += OnStageEntered;
    }

    private void OnDisable()
    {
        if (levelMap != null) levelMap.StageEntered -= OnStageEntered;
    }

    private void Start()
    {
        // BattleController.Instance 於其 Awake 設定
        subscribedBattle = BattleController.Instance;
        if (subscribedBattle != null)
        {
            subscribedBattle.BattleStarted += OnBattleStarted;
            subscribedBattle.CardUsed += OnBattleCardUsed;
            subscribedBattle.AddTurnEndInterlude(turnEndInterlude);
        }

        // 保險：若起始 Stage 的進入事件早於訂閱（例如本元件晚啟用），補判一次（已播過會自動略過）
        if (levelMap != null && levelMap.CurrentStage != null) OnStageEntered(levelMap.CurrentStage);
    }

    private void OnDestroy()
    {
        if (subscribedBattle != null)
        {
            subscribedBattle.BattleStarted -= OnBattleStarted;
            subscribedBattle.CardUsed -= OnBattleCardUsed;
            subscribedBattle.RemoveTurnEndInterlude(turnEndInterlude);
        }
    }

    // ────────────────────────────────────────────────────────────────
    // 觸發時機
    // ────────────────────────────────────────────────────────────────

    private void OnStageEntered(StageInfo stage)
    {
        if (stage == null) return;
        StageDialogue entry = stageDialogues.Find(s => s != null && s.stage == stage);
        if (entry == null || !TryMarkOnce("Stage_" + stage.name)) return;
        StartCoroutine(PlayAfterDelay(entry.sequence, stageDialogueDelay));
    }

    public override IEnumerator OnBeforeBattle(Enemy enemy)
    {
        if (TryMarkOnce(KeyEncounter)) yield return PlayList(firstEnemyEncounter);
    }

    public override IEnumerator OnAfterEvent(NodeEventType type, NodeEvent grid)
    {
        if (type == NodeEventType.quest)
        {
            // 只有真的「接下」任務才算（面板按接受）；直接關閉不算
            bool accepted = grid != null && grid.questData != null
                && QuestTracker.Instance != null && QuestTracker.Instance.IsActive(grid.questData);
            if (accepted && TryMarkOnce(KeyQuest)) yield return PlayList(firstQuestAccepted);
        }
        else if (type == NodeEventType.Treasure)
        {
            if (TryMarkOnce(KeyTreasure)) yield return PlayList(firstTreasure);
        }
    }

    private void OnBattleStarted()
    {
        if (!TryMarkOnce(KeyBattle)) return;
        tutorialBattleActive = true;
        wonDialoguePending = true;
        StartCoroutine(PlayAfterDelay(tutorialBattleStart, tutorialBattleStartDelay));
    }

    // 教學戰中、玩家第一次用骰子打出卡片 → 稍等特效後播對話
    private void OnBattleCardUsed(BattleUnit user, Assets.Scripts.GlobalEnums.BattleEnum.CardType cardType)
    {
        if (!tutorialBattleActive) return;
        var bc = BattleController.Instance;
        if (bc == null || user != bc.PlayerUnit) return;
        if (!tutorialFirstCardUsed.HasAny || !TryMarkOnce(KeyBattleFirstCard)) return;
        StartCoroutine(PlayFirstCardDialogue());
    }

    private IEnumerator PlayFirstCardDialogue()
    {
        cardDialoguePending = true;
        if (tutorialFirstCardDelay > 0f) yield return new WaitForSeconds(tutorialFirstCardDelay);
        // 這一張就把敵人打倒（戰鬥已結束）時不播，避免蓋在結算畫面上
        if (tutorialBattleActive) yield return PlayList(tutorialFirstCardUsed);
        cardDialoguePending = false;
    }

    // 回合結束插播：教學戰中、玩家的第一個回合結束 → 播對話，播完結束教學
    private IEnumerator OnBattleTurnEnded(BattleUnit endedUnit)
    {
        if (!tutorialBattleActive) yield break;
        var bc = BattleController.Instance;
        if (bc == null || endedUnit != bc.PlayerUnit) yield break;

        // 出牌對話還在等待/播放（例如出牌後立刻按結束回合）→ 先讓它播完，順序維持「出牌對話 → 回合結束對話」
        while (cardDialoguePending) yield return null;

        tutorialBattleActive = false; // 教學到此結束，之後正常操作
        if (TryMarkOnce(KeyBattleTurnEnd)) yield return PlayList(tutorialFirstTurnEnd);
    }

    public override void OnBattleEnded(bool playerWin)
    {
        tutorialBattleActive = false;
    }

    public override IEnumerator OnAfterBattleReturned(bool playerWin)
    {
        if (!playerWin || !wonDialoguePending) yield break;
        wonDialoguePending = false;
        if (TryMarkOnce(KeyBattleWon)) yield return PlayList(tutorialBattleWon);
    }

    // ────────────────────────────────────────────────────────────────
    // 播放
    // ────────────────────────────────────────────────────────────────

    private IEnumerator PlayAfterDelay(DialogueSequence seq, float delay)
    {
        if (seq == null || !seq.HasAny) yield break;
        if (player != null) player.PushInputHold(); // 等待期間也先扣住，避免玩家搶先點走
        if (delay > 0f) yield return new WaitForSeconds(delay);
        yield return PlayList(seq);
        if (player != null) player.PopInputHold();
    }

    /// <summary>
    /// 播放一組對話（依 <see cref="DialogueSequence.keepUIOpenBetween"/> 決定段落間要不要收起 UI），
    /// 全部結束才返回；期間暫扣地圖點擊。
    /// </summary>
    private IEnumerator PlayList(DialogueSequence seq)
    {
        if (dialogue == null || seq == null || !seq.HasAny) yield break;

        while (busy) yield return null;
        busy = true;
        if (player != null) player.PushInputHold();

        yield return dialogue.PlayDialogueSequenceRoutine(seq.dialogues, seq.keepUIOpenBetween);

        if (player != null) player.PopInputHold();
        busy = false;
    }

    // ────────────────────────────────────────────────────────────────
    // 「只播一次」進度
    // ────────────────────────────────────────────────────────────────

    /// <summary>第一次呼叫回傳 true 並記為已播；之後都回傳 false。</summary>
    private bool TryMarkOnce(string key)
    {
        if (done.Contains(key)) return false;
        if (persistProgress && PlayerPrefs.GetInt(saveKeyPrefix + key, 0) == 1) { done.Add(key); return false; }

        done.Add(key);
        if (persistProgress) { PlayerPrefs.SetInt(saveKeyPrefix + key, 1); PlayerPrefs.Save(); }
        return true;
    }

    [ContextMenu("重置教學進度（清除已播紀錄）")]
    private void ResetProgress()
    {
        var keys = new List<string> { KeyQuest, KeyEncounter, KeyTreasure, KeyBattle, KeyBattleFirstCard, KeyBattleTurnEnd, KeyBattleWon };
        foreach (var s in stageDialogues) if (s != null && s.stage != null) keys.Add("Stage_" + s.stage.name);
        foreach (var k in keys) PlayerPrefs.DeleteKey(saveKeyPrefix + k);
        PlayerPrefs.Save();
        done.Clear();
        Debug.Log($"[{name}] 已重置教學進度。");
    }
}
