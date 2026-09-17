using UnityEngine;
using UnityEngine.UI;

public class GuideButton_Shop : MonoBehaviour
{

    public DialogueManager dialogueManager;
    public DialogueOpenClose dialogueOpen;

    private Button button;
    private static bool hasTriggeredGuideDialogue = false;

    void Awake()
    {
        // 獲取按鈕組件
        button = GetComponent<Button>();
        if (button != null)
        {
            // 註冊按鈕點擊事件
            button.onClick.AddListener(OnButtonClick);
        }
        else
        {
            Debug.LogWarning("Button component not found on this GameObject.");
        }
    }

    // 按鈕點擊事件處理
    private void OnButtonClick()
    {

        Guide();
        // 關閉這個腳本
        this.enabled = false; // 停用腳本
    }

    // 你的功能
    private void Guide()
    {

        if (hasTriggeredGuideDialogue) return; // 如果已經觸發過，則不執行
        hasTriggeredGuideDialogue = true;
        // 檢查是否有下一段劇情
        SO_DialogueData nextDialogueData = dialogueManager.GetDialogueData();

        if (nextDialogueData != null) // 確保下一段劇情存在
        {
            dialogueOpen.TestStageIntAdd();
            dialogueOpen.OpenDialogue();
        }
    }
}
