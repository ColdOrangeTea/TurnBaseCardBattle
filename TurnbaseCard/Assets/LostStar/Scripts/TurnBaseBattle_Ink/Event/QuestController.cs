using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 任務事件（quest）UI 控制器（仿 <see cref="EventController"/> 的解耦版，由 A_Good_Ink 使用 AI 生成）。
///
/// 玩家走到「quest」事件格 → <see cref="MapEventService.QuestRequested"/> → 本元件讀取該格
/// <see cref="NodeEvent.questData"/>（<see cref="QuestData"/>）顯示任務資訊（名稱／描述／獎勵）；
/// 接受→拋 <see cref="QuestAccepted"/>、關閉→拋 <see cref="QuestClosed"/>。
///
/// 與舊 QuestManager/QuestUI 差異：不依賴 V2 重構已移除的 DialogueManager／DialogueOpenClose／
/// PlayerInventory；任務接受用事件對外拋，日後由任務/存檔系統訂閱實際入袋，本元件只顯示與拋出。
/// </summary>
public class QuestController : MonoBehaviour
{
    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private MapEventService mapEventService;
    [Tooltip("開任務時暫停地圖點擊、關閉後恢復；可留空。")]
    [SerializeField] private S001_PlayerController playerController;

    [Header("任務 UI")]
    [Tooltip("任務面板根（開/關）；對應 QuestEmpty prefab 的根物件")]
    [SerializeField] private GameObject questPanelRoot;
    [Tooltip("任務內容面板（接任務時顯示）；留空會在 questPanelRoot 下依名稱 QuestBG 自動尋找")]
    [SerializeField] private GameObject questBG;
    [Tooltip("完成領獎面板（任務完成時顯示）；留空會依名稱 QuestResult 自動尋找")]
    [SerializeField] private GameObject questResult;
    [SerializeField] private TMP_Text titleText;        // 任務名稱
    [SerializeField] private TMP_Text descriptionText;  // 任務描述
    [SerializeField] private TMP_Text rewardText;       // 獎勵說明（QuestBG）
    [SerializeField] private TMP_Text resultRewardText; // 完成獎勵說明（QuestResult，可空）
    [SerializeField] private Button acceptButton;       // 接受任務
    [SerializeField] private Button closeButton;        // 關閉
    [Tooltip("發道具獎勵用的資料庫；留空會 Resources.Load(\"Item/ShopItemDatabase\")")]
    [SerializeField] private ItemDatabase itemDatabase;

    private QuestData current;

    /// <summary>任務面板目前是否開著（接任務或領獎面板）。</summary>
    public bool IsOpen => questPanelRoot != null && questPanelRoot.activeSelf;

    /// <summary>玩家接受了任務（帶 QuestData）；日後任務/存檔系統訂閱以實際登記。</summary>
    public event Action<QuestData> QuestAccepted;
    /// <summary>任務面板關閉。</summary>
    public event Action QuestClosed;

    private void Awake()
    {
        if (mapEventService == null) mapEventService = FindAnyObjectByType<MapEventService>();
        if (playerController == null) playerController = FindAnyObjectByType<S001_PlayerController>();
        // 自動尋找 QuestBG / QuestResult 子面板
        if (questPanelRoot != null)
        {
            if (questBG == null) { var t = questPanelRoot.transform.Find("QuestBG"); if (t != null) questBG = t.gameObject; }
            if (questResult == null) { var t = questPanelRoot.transform.Find("QuestResult"); if (t != null) questResult = t.gameObject; }
        }
        if (itemDatabase == null) itemDatabase = Resources.Load<ItemDatabase>("Item/ShopItemDatabase");
        if (questPanelRoot != null) questPanelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (mapEventService != null) mapEventService.QuestRequested += OnQuestRequested;
    }

    private void OnDisable()
    {
        if (mapEventService != null) mapEventService.QuestRequested -= OnQuestRequested;
    }

    private void OnQuestRequested(NodeEvent grid)
    {
        if (grid == null) return;
        if (grid.questData == null)
        {
            Debug.LogWarning($"[QuestController] 任務節點「{grid.name}」未指定 QuestData，無法顯示任務內容。");
            return;
        }
        Open(grid.questData);
    }

    /// <summary>開啟任務：暫停地圖點擊、以指定的 <see cref="QuestData"/> 填入名稱／描述／獎勵。</summary>
    public void Open(QuestData q)
    {
        if (q == null) return;
        current = q;

        if (questPanelRoot != null) questPanelRoot.SetActive(true);
        if (questBG != null) questBG.SetActive(true);        // 接任務：顯示內容面板
        if (questResult != null) questResult.SetActive(false); // 關閉完成面板
        if (playerController != null) playerController.DisablePlayerInputForCheck();

        if (titleText != null) titleText.text = q.questName;
        if (descriptionText != null) descriptionText.text = q.description;
        if (rewardText != null)
            rewardText.text = q.successReward != null
                ? $"金幣 {q.successReward.gold}" + (string.IsNullOrEmpty(q.successReward.item) ? "" : $"、道具 {q.successReward.item}")
                : "";

        if (acceptButton != null)
        {
            acceptButton.onClick.RemoveAllListeners();
            acceptButton.onClick.AddListener(Accept);
        }
        BattleLog.Log($"[QuestController] 開啟任務：{q.questName}");
    }

    /// <summary>接受任務：拋出 QuestAccepted、登記到 QuestTracker 開始追蹤條件，關閉面板。</summary>
    private void Accept()
    {
        if (current != null)
        {
            QuestAccepted?.Invoke(current);
            if (QuestTracker.Instance != null) QuestTracker.Instance.RegisterActiveQuest(current);
        }
        Close();
    }

    /// <summary>
    /// 任務完成：顯示 QuestResult 領獎面板、關閉 QuestBG，並把成功獎勵（金幣／道具）發到中樞。
    /// 供任務/追蹤系統在判定任務達成時呼叫（可帶入該任務的 QuestData；不帶則用目前這筆）。
    /// </summary>
    public void CompleteQuest(QuestData q = null)
    {
        QuestData quest = q != null ? q : current;
        if (quest == null) { Debug.LogWarning("[QuestController] CompleteQuest：沒有任務可完成。"); return; }
        current = quest;

        if (questPanelRoot != null) questPanelRoot.SetActive(true);
        if (questResult != null) questResult.SetActive(true); // 完成：顯示領獎面板
        if (questBG != null) questBG.SetActive(false);         // 關閉內容面板
        if (playerController != null) playerController.DisablePlayerInputForCheck();

        GrantReward(quest.successReward);
        if (resultRewardText != null && quest.successReward != null)
            resultRewardText.text = $"任務完成！獲得 金幣 {quest.successReward.gold}" +
                (string.IsNullOrEmpty(quest.successReward.item) ? "" : $"、道具 {quest.successReward.item}");

        BattleLog.Log($"[QuestController] 任務完成：{quest.questName}");
    }

    // 發放獎勵到中樞：金幣直接加；道具依名稱查資料庫後加入背包
    private void GrantReward(RewardData reward)
    {
        if (reward == null || LevelMapInitializer.Instance == null) return;
        if (reward.gold != 0) LevelMapInitializer.Instance.ChangeMoney(reward.gold);
        if (!string.IsNullOrEmpty(reward.item) && itemDatabase != null)
        {
            var item = itemDatabase.GetItemByName(reward.item);
            if (item != null) LevelMapInitializer.Instance.AddItem(item);
            else Debug.LogWarning($"[QuestController] 找不到獎勵道具「{reward.item}」");
        }
    }

    /// <summary>關閉任務 UI、恢復地圖點擊。</summary>
    public void Close()
    {
        if (questPanelRoot != null) questPanelRoot.SetActive(false);
        if (playerController != null) playerController.EnablePlayerInput();
        current = null;
        QuestClosed?.Invoke();
        BattleLog.Log("[QuestController] 關閉任務");
    }
}
