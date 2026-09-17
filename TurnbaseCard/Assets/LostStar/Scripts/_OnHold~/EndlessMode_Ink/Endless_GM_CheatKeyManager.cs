using UnityEngine;

public class Endless_GM_CheatKeyManager : MonoBehaviour
{
    private Endless_GM_GridManager gridManager;

    [Header("啟用作弊功能")]
    public bool enableCheats = true;

    void Start()
    {
        // 嘗試找到 GridManager
        gridManager = FindAnyObjectByType<Endless_GM_GridManager>();

        if (gridManager == null)
        {
            Debug.LogError("找不到 GridManager，請確保它存在於場景中！");
        }
    }

    void Update()
    {
        if (!enableCheats || gridManager == null)
            return;

        // 檢查數字鍵輸入
        for (int i = 0; i <= 9; i++) // 支援數字鍵 0-9
        {
            if (Input.GetKeyDown(KeyCode.Alpha0 + i))
            {
                JumpToLevel(i - 1); // 減 1 是因為關卡索引從 0 開始
            }
        }
    }

    /// <summary>
    /// 跳到指定關卡的起始網格
    /// </summary>
    /// <param name="levelIndex">要跳轉的關卡索引</param>
    void JumpToLevel(int levelIndex)
    {
        if (levelIndex < 0 || levelIndex >= gridManager.levels.Count)
        {
            Debug.Log($"關卡索引 {levelIndex} 無效。有效範圍為 0 至 {gridManager.levels.Count - 1}。");
            return;
        }

        Debug.Log($"跳轉到關卡 {levelIndex + 1} 的起始網格！");

        // 更新當前關卡索引並設定關卡
        gridManager.currentLevelIndex = levelIndex;
        gridManager.SetCurrentLevel(levelIndex);
    }
}
