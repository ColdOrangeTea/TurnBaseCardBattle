using Assets.Scripts.GlobalEnums;
using Assets.Scripts.Dialogue;
using Spine.Unity;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridManager : MonoBehaviour
{
    public Transform player;
    public Transform enemy;

    [Header("正比副團長動畫設定")]
    public GameObject Jephthah;

    [Header("正比聖女動畫設定")]
    public SkeletonGraphic GodnessAni;
    public float fadeDuration = 0.1f; // 淡出的時間
    private List<DialogueUnitType> types; // 儲存對話類型的列表

    public S001_PlayerController playerController;
    [SerializeField] private DialogueTypingEffect dialogueTypingEffect; // 引用 DialogueTypingEffect

    [Header("多關卡設定")]
    public List<LevelInfo> levels; // 保存每一關的起點、終點和網格列表


    public int currentLevelIndex = 0; // 當前關卡索引 

    [SerializeField]
    private bool hasTriggeredDialogue = false; // 用來追蹤對話是否已經觸發過

    private static bool hasTriggeredGuideDialogue = false;
    private bool isTriggered = false;  // 用來記錄是否觸發
    [SerializeField]
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
        GodnessAni.enabled = false;
        // 確保取得 DialogueTypingEffect 實例
        dialogueTypingEffect = FindAnyObjectByType<DialogueTypingEffect>();


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

        // 更新當前關卡的敵人生成點
        for (int i = 0; i < levels[levelIndex].enemySpawnPoints.Count; i++)
        {
            if (i < levels[levelIndex].enemies.Count)
            {
                levels[levelIndex].enemies[i].position = levels[levelIndex].enemySpawnPoints[i].position;
                Debug.Log("敵人生成在位置: " + levels[levelIndex].enemySpawnPoints[i].position);
            }
        }

        Debug.Log("當前關卡網格數量: " + levels[levelIndex].gridList.Count);
    }

    // 返回當前關卡中的敵人列表
    public List<Transform> GetEnemiesInCurrentLevel()
    {
        return levels[currentLevelIndex].enemies;
    }

    public void ClearEnemiesFromBattleField() //用來刪除敵人物件
    {
        List<Transform> enemies = GetEnemiesInCurrentLevel();
        foreach (Transform enemy in enemies)
        {
            Destroy(enemy.gameObject); // 刪除當前關卡的每個敵人
        }
        enemies.Clear(); // 清空當前關卡的敵人列表
        Debug.Log("當前關卡的敵人已全部清除");

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


    // 已觸發過「敵人全數清除」的關卡索引。
    // CheckForEnemiesCleared() 由 Update 每幀呼叫，需要這個集合避免同一關重複觸發。
    private readonly HashSet<int> clearedLevels = new HashSet<int>();

    // 敵人清除後觸發的功能
    private void OnEnemiesCleared()
    {
        // 同一關只觸發一次
        if (!clearedLevels.Add(currentLevelIndex)) return;

        Debug.Log($"關卡 {currentLevelIndex} 的所有敵人已被消滅");

        // 原本此處會透過 QuestManager 完成當前任務，任務系統尚未移植。
        // 之後接上新的任務／關卡推進系統時，在此加入對應處理。
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
        currentLevelIndex++;

        // 更新當前關卡
        if (currentLevelIndex < levels.Count)
        {
            SetCurrentLevel(currentLevelIndex);
        }
        else
        {
            Debug.Log("遊戲完成！沒有更多關卡");
        }


        // 根據觸發次數執行不同邏輯
        switch (triggerCount)
        {
            case 0:
                TriggerDialogueMethod1();
                break;
            case 1:
                TriggerDialogueMethod2();
                break;
            case 2:
                TriggerDialogueMethod3();
                break;
            case 3:
                Debug.Log("第四次轉場");
                break;
            case 4:
                Debug.Log("第五次轉場");
                break;
            case 5:
                TriggerDialogueMethod4();
                break;
            default:
                Debug.Log("觸發次數已達上限，不再觸發對話");
                return; // 次數超過限制，直接返回
        }

        // 增加觸發次數
        CountTrigger();

      
    }
    // 方法1：執行第一種觸發邏輯
    private void TriggerDialogueMethod1()
    {
        Debug.Log("執行方法1：開啟主線對話");
    }

    // 方法2：執行第二種觸發邏輯
    private void TriggerDialogueMethod2()
    {
        Debug.Log("執行方法2：開啟主線對話2");
    }

    private void TriggerDialogueMethod3()
    {
        Debug.Log("執行方法3：開啟主線對話3");
    }
    private void TriggerDialogueMethod4()
    {
        Jephthah.SetActive(false);
        GodnessAni.enabled = true;
        GodnessAni.AnimationState.SetAnimation(0, "Watch", true); // 播放 run 動畫，並設置為循環播放
        Debug.Log("執行方法3：開啟主線對話3");
    }

    //計算文本觸發次數
    private void CountTrigger()
    {
        triggerCount++;
        Debug.Log("跳關文本次數: " + triggerCount);
    }

    #region 正比聖女ㄅ播放邏輯

    IEnumerator FadeIn()
    {
        Color startColor = new Color(GodnessAni.color.r, GodnessAni.color.g, GodnessAni.color.b, 0f); // 完全透明
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f); // 完全不透明

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            GodnessAni.color = Color.Lerp(startColor, endColor, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        GodnessAni.color = endColor; // 確保最終顏色為不透明
    }

    IEnumerator FadeOut()
    {
        Color startColor = GodnessAni.color;
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 0f); // 完全透明

        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            GodnessAni.color = Color.Lerp(startColor, endColor, elapsedTime / fadeDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        GodnessAni.color = endColor; // 確保最終顏色為透明
    }

    #endregion


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
        Transform playerGrid = GetGridAtPosition(player.position); // 找到玩家所在的網格
        Transform enemyGrid = GetGridAtPosition(enemy.position);   // 找到敵人所在的網格

        // 檢查是否是同一個網格
        return playerGrid == enemyGrid;
    }



}
