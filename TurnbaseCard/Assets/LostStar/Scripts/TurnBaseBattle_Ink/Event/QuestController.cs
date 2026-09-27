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
    [SerializeField] private TMP_Text titleText;        // 任務名稱
    [SerializeField] private TMP_Text descriptionText;  // 任務描述
    [SerializeField] private TMP_Text rewardText;       // 獎勵說明
    [SerializeField] private Button acceptButton;       // 接受任務
    [SerializeField] private Button closeButton;        // 關閉

    private QuestData current;

    /// <summary>玩家接受了任務（帶 QuestData）；日後任務/存檔系統訂閱以實際登記。</summary>
    public event Action<QuestData> QuestAccepted;
    /// <summary>任務面板關閉。</summary>
    public event Action QuestClosed;

    private void Awake()
    {
        if (mapEventService == null) mapEventService = FindAnyObjectByType<MapEventService>();
        if (playerController == null) playerController = FindAnyObjectByType<S001_PlayerController>();
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

    /// <summary>接受任務：拋出 QuestAccepted 供外部登記，關閉面板。</summary>
    private void Accept()
    {
        if (current != null) QuestAccepted?.Invoke(current);
        Close();
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
