using Assets.Scripts.GlobalEnums;
using System.Collections.Generic;
using UnityEngine;

public class Endless_GM_GridManager : MonoBehaviour
{
    // 全局敵人名單
    public List<Transform> globalEnemyList; // 統一的敵人列表

    public Transform player;
    public Transform enemy;

    public S001_PlayerController playerController;
    public DialogueOpenClose dialogueClose;
    public DialogueManager dialogueManager;
    public QuestManager quest;

    [Header("多關卡設定")]
    public List<LevelInfo> levels; // 保存每一關的起點、終點和網格列表

    // 用於追踪戰鬥中的敵人
    [SerializeField]
    private List<Transform> encounteredEnemies = new List<Transform>();


    public int currentLevelIndex = 0; // 當前關卡索引 

    private MapTurnBaseManager turnBaseManager; // 新增這行以獲取 MapTurnBaseManager 的參考
    [SerializeField]
    private bool hasTriggeredDialogue = false; // 用來追蹤對話是否已經觸發過

    private static bool hasTriggeredGuideDialogue = false;
    private bool isTriggered = false;  // 用來記錄是否觸發
    private int triggerCount = 0;     // 計算觸發次數



    [System.Serializable]
    public class LevelInfo
    {
        public Transform startGrid; // 每關卡的起始網格
        public Transform endGrid; // 每關卡的終點網格
        public Transform cameraTarget; // 攝影機目標點
        public List<Transform> gridList; // 該關卡的所有網格
        public List<Transform> enemySpawnPoints; // 敵人的生成點
        public List<Transform> enemies; // 該關卡的敵人列表
        public bool isEnemyClearedCheckEnabled = false; // 是否啟用敵人消失檢測
    }
  

    void Start()
    {

        // 確保 turnBaseManager 已正確指派
        turnBaseManager = FindAnyObjectByType<MapTurnBaseManager>();

        if (turnBaseManager == null)
        {
            Debug.LogError("找不到 MapTurnBaseManager，請確認它是否存在於場景中。");
        }

        SetCurrentLevel(currentLevelIndex); // 初始化當前關卡
    }
    void Update()
    {
        CheckForEnemiesCleared();
    }

    // 設定當前關卡
    public void SetCurrentLevel(int levelIndex)
    {
        if (levelIndex >= levels.Count)
        {
            Debug.Log("所有關卡已經完成");
            return;
        }

        // 設定玩家的起點與終點
        player.position = levels[levelIndex].startGrid.position;

        // 更新攝影機目標點
        if (levels[levelIndex].cameraTarget != null)
        {
            Camera.main.GetComponent<CameraController>().SetCameraTarget(levels[levelIndex].cameraTarget);
        }

        // 清空當前關卡的敵人列表以避免重複計算
        levels[levelIndex].enemies.Clear();


        // 生成敵人到當前關卡的生成點
        GenerateEnemiesForLevel(levelIndex);

        Debug.Log("當前關卡網格數量: " + levels[levelIndex].gridList.Count);
    }
    private void GenerateEnemiesForLevel(int levelIndex)
    {
        if (globalEnemyList == null || globalEnemyList.Count == 0)
        {
            Debug.LogError("全局敵人列表為空，無法生成敵人！");
            return;
        }

        var currentLevel = levels[levelIndex];
        for (int i = 0; i < currentLevel.enemySpawnPoints.Count; i++)
        {
            // 從全局敵人列表隨機抽取一個敵人
            int randomEnemyIndex = Random.Range(0, globalEnemyList.Count);
            Transform selectedEnemy = globalEnemyList[randomEnemyIndex];

            // 生成敵人並放置在生成點
            Transform spawnPoint = currentLevel.enemySpawnPoints[i];
            Transform clonedEnemy = Instantiate(selectedEnemy, spawnPoint.position, spawnPoint.rotation);

            // 為克隆出的敵人設置標識
            clonedEnemy.name = selectedEnemy.name + "_Clone"; // 或者設置名字來識別它

            // 加入當前關卡的敵人列表以便追蹤
            currentLevel.enemies.Add(clonedEnemy);

            Debug.Log($"生成敵人 {selectedEnemy.name} 到生成點 {spawnPoint.name}");
        }
    }

    // 檢查敵人列表是否為空
    private bool IsEnemyListValid(int levelIndex)
    {
        if (levels[levelIndex].enemies == null || levels[levelIndex].enemies.Count == 0)
        {
            Debug.LogWarning($"關卡 {levelIndex} 的敵人列表為空，無法生成敵人！");
            return false;
        }
        return true;
    }


    // 返回當前關卡中的敵人列表
    public List<Transform> GetEnemiesInCurrentLevel()
    {
        return levels[currentLevelIndex].enemies;
    }

    // 標記敵人為遭遇的敵人
    public void MarkEnemyAsEncountered(Transform enemy)
    {
        if (!encounteredEnemies.Contains(enemy))
        {
            encounteredEnemies.Add(enemy);
            Destroy(enemy.gameObject); // 銷毀敵人物件
            Debug.Log($"敵人 {enemy.name} 已被標記為遭遇敵人並隱藏");
        }
    }

    public void ClearEncounteredEnemiesFromBattleField()
    {
        foreach (Transform enemy in encounteredEnemies)
        {
            if (enemy != null)
            {
                Destroy(enemy.gameObject); // 銷毀敵人物件
            }
        }
        encounteredEnemies.Clear(); // 清空遭遇敵人的列表
        Debug.Log("遭遇的敵人已全部隱藏");
    }

    public void ShowHiddenEnemies()
    {
        foreach (Transform enemy in encounteredEnemies)
        {
            if (enemy != null)
            {
                enemy.gameObject.SetActive(true); // 顯示敵人物件
            }
        }
        Debug.Log("隱藏的敵人已重新顯示");
    }

    // 檢查特定關卡的敵人是否已全部消失
    private void CheckForEnemiesCleared()
    {

        // 只檢查啟用了敵人清除檢測的關卡
        if (levels[currentLevelIndex].isEnemyClearedCheckEnabled && GetEnemiesInCurrentLevel().Count == 0)
        {
            OnEnemiesCleared();
        }
    }


    // 敵人清除後觸發的功能
    private void OnEnemiesCleared()
    {
        //// 檢查是否已經觸發過對話
        //if (!hasTriggeredDialogue)
        //{
        //    Debug.Log($"關卡 {currentLevelIndex} 的所有敵人已被消滅，觸發下一步行動");

        //    dialogueClose.TestStageIntAdd();
        //    dialogueClose.OpenDialogue();

        //    hasTriggeredDialogue = true; // 設置為 true，表示已經觸發過對話
        //}

        // 獲取當前的任務
        QuestData currentQuest = quest.GetQuest(); // 假設這是一個返回當前任務的方法

        if (currentQuest != null)
        {
            // 在此您可以根據需要決定是否成功
            bool isSuccess = true; // 您可以根據遊戲邏輯來設定成功與否

            // 完成當前任務
            quest.CompleteQuest(currentQuest, isSuccess);
        }
        else
        {
            Debug.Log("沒有當前任務可完成");
            // 如果沒有任務，這裡可以選擇執行其他邏輯，例如觸發其他事件
        }



    }

    // 返回特定位置上的网格（根據當前關卡查找）
    public Transform GetGridAtPosition(Vector3 position)
    {
        Transform closestGrid = null;
        float closestDistance = Mathf.Infinity;

        foreach (var grid in levels[currentLevelIndex].gridList)
        {
            float distance = Vector3.Distance(grid.position, position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestGrid = grid;
            }
        }

        return closestGrid;
    }

    // 轉移到下一關卡
    public void MoveToNextLevel()
    {
        // 隨機選擇一個關卡索引
        int randomLevelIndex = Random.Range(0, levels.Count);

        // 更新當前關卡索引
        currentLevelIndex = randomLevelIndex;

        // 設定選定關卡為當前關卡
        if (currentLevelIndex < levels.Count)
        {
            SetCurrentLevel(currentLevelIndex);
            Debug.Log($"傳送到隨機關卡：關卡索引 {currentLevelIndex}");
        }
        else
        {
            Debug.LogError("關卡索引超出範圍！");
        }

        DeleteAllClonedEnemies();

        SetCurrentLevel(randomLevelIndex);
    }

    // 查找從起點到終點的路徑
    public List<Transform> FindPath(Vector3 playerPosition, Transform target)
    {
        List<Transform> path = new List<Transform>();

        Transform start = GetGridAtPosition(playerPosition);
        if (start == null)
        {
            Debug.Log("未找到玩家所在的網格!");
            return path;
        }

        if (target == null || !levels[currentLevelIndex].gridList.Contains(target))
        {
            Debug.Log("未找到目標網格!");
            return path;
        }

        HashSet<Transform> visited = new HashSet<Transform>();
        bool pathFound = SearchPath(start, target, path, visited);

        if (!pathFound)
        {
            Debug.Log("無法到達目標網格!");
        }

        return path;
    }

    private bool SearchPath(Transform current, Transform target, List<Transform> path, HashSet<Transform> visited)
    {
        visited.Add(current);
        path.Add(current);

        if (current == target)
        {
            return true;
        }

        GridData currentGridData = current.GetComponent<GridData>();
        foreach (Transform neighbor in currentGridData.connectedGrids)
        {
            if (!visited.Contains(neighbor))
            {
                if (SearchPath(neighbor, target, path, visited))
                {
                    return true;
                }
            }
        }

        path.Remove(current);
        return false;
    }

    // 檢查玩家和敵人是否在同一個網格上
    public bool IsPlayerAndEnemyOnSameGrid(Transform player, Transform enemy)
    {
        // 確保 player 和 enemy 不為 null 或已被銷毀
        if (player == null || enemy == null)
        {
            Debug.LogWarning("Player or Enemy transform is null or destroyed.");
            return false;
        }

        Transform playerGrid = GetGridAtPosition(player.position); // 找到玩家所在的網格
        Transform enemyGrid = GetGridAtPosition(enemy.position);   // 找到敵人所在的網格

        // 檢查是否是同一個網格
        return playerGrid == enemyGrid;
    }


    public void ResetEnemySpawnPoints()
    {

        // 重新生成敵人到指定生成點
        for (int i = 0; i < levels[currentLevelIndex].enemySpawnPoints.Count; i++)
        {
            if (levels[currentLevelIndex].enemies.Count > 0) // 確保列表不為空
            {
                // 隨機選擇一個敵人
                int randomEnemyIndex = Random.Range(0, levels[currentLevelIndex].enemies.Count);
                Transform selectedEnemy = levels[currentLevelIndex].enemies[randomEnemyIndex];

                // 生成敵人並放置在生成點
                Transform spawnPoint = levels[currentLevelIndex].enemySpawnPoints[i];
                Instantiate(selectedEnemy, spawnPoint.position, spawnPoint.rotation);

                // 可選：從列表中移除已生成的敵人，避免重複
                //levels[currentLevelIndex].enemies.RemoveAt(randomEnemyIndex);
            }
        }

        Debug.Log($"重置了關卡 {currentLevelIndex} 的敵人生成位置");
    }


    // 刪除當前場景中所有的克隆敵人
    public void DeleteAllClonedEnemies()
    {
        // 查找場景中的所有敵人 (包含所有活動物件)
        GameObject[] allEnemies = GameObject.FindGameObjectsWithTag("Enemy");

        foreach (var enemy in allEnemies)
        {
            // 檢查敵人名稱是否包含 "_Clone"
            if (enemy.name.Contains("_Clone"))
            {
                Destroy(enemy); // 銷毀克隆敵人
                Debug.Log($"已刪除克隆敵人: {enemy.name}");
            }
        }
    }


}
