using UnityEngine;
using TMPro;
using UnityEngine.UI; // 引入 UI 命名空間

public class QuestUI : MonoBehaviour
{
    public TextMeshProUGUI requirementText;    // 任務需求文本
    public TextMeshProUGUI RewardText;    // 任務需求文本
    public TextMeshProUGUI FailureText;    // 任務需求文本
    public TextMeshProUGUI progressText;       // 任務進度文本 (新增)
    public TextMeshProUGUI progressText2;       // 任務進度文本 (新增)

    public GameObject questPanel;              // 任務顯示面板
    public GameObject questBG;
    public GameObject questListPanel;              // 任務顯示面板
    public GameObject questResultPanel;

    public Button acceptButton;                 // 接受任務按鈕
    public Button CloseButton;
    public QuestManager questManager;           // 參考 QuestManager


    public QuestData currentQuest; // 當前顯示的任務

    private void Start()
    {
        // 設定按鈕的點擊事件
        acceptButton.onClick.AddListener(HandleAcceptQuest);
        CloseButton.onClick.AddListener(HideQuest);
    }

    public void DisplayQuest(QuestData quest)
    {
        currentQuest = quest; // 設置當前任務
        questPanel.SetActive(true);
        questResultPanel.SetActive(false);
        requirementText.text = quest.description;
        progressText.text = quest.questName;
        RewardText.text = $" 血量回10滴\n金幣{quest.successReward.gold}枚 ";
        FailureText.text = $"失敗獎勵: 無";

        UpdateQuestProgress(0, 1); // 初始化進度顯示

    }
    public void UpdateQuestProgress(int current, int total) // 新增進度更新方法
    {

        progressText2.text = $"擊敗偷走毛線球的人: ({current}/{total})"; // 更新進度文本
    }

    public void HideQuest()
    {
        // 清除或隱藏任務 UI
        questPanel.SetActive(false);
    }
    public void ShowQuest()
    {
        // 清除或隱藏任務 UI
        questPanel.SetActive(true);
    }

    public void HideQuestBG()
    {
        questBG.SetActive(false);
    }
    public void HideQuestList()
    {
        questListPanel.SetActive(false);
        // 清除或隱藏任務 UI
        progressText.gameObject.SetActive(false);
        progressText2.gameObject.SetActive(false);
    }

    public void ShowQuestList()
    {
        questListPanel.SetActive(true);
        progressText.gameObject.SetActive(true);
        progressText2.gameObject.SetActive(true);
    }
    public void ShowQuestResult()
    {
        questResultPanel.SetActive(true);
    }

    public void HideQuestResult()
    {
        questResultPanel.SetActive(false);
    }

    private void HandleAcceptQuest()
    {
        if (currentQuest != null)
        {
            questManager.AcceptCurrentQuest(); // 通知 QuestManager 接受任務
            Debug.Log($"接受任務: {currentQuest.questName}");
        }
    }

}
