using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class QuestList : MonoBehaviour
{
    public TextMeshProUGUI progressText;       // 任務進度文本 (新增)
    public void UpdateQuestProgress(int current, int total) // 新增進度更新方法
    {
        progressText.text = $"任務進度: ({current}/{total})"; // 更新進度文本
    }
}
