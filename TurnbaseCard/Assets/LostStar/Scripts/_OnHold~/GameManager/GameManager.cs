using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.GlobalEnums;
using System;
using Unity.VisualScripting;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;
    [SerializeField]
    private CheatManager cheatManager;
    public static DialogueManager DialogueManagerInstance;
    public static TurnBaseBattleManager TurnBaseBattleManagerInstance;


    [Header("編輯器內作弊bool控制，只有勾了這個才能用作弊功能。如需要請手動在編輯器中勾選")]
    public bool IsInEditor = true;
    public static bool InEditorState;

    [Space(10)]
    [SerializeField]
    private DialogueManager temp_DialogueManager;
    [SerializeField]
    private TurnBaseBattleManager temp_TurnBaseBattleManager;


    [Header("這是編輯器裡用來顯示實際上的全域靜態bool資料，調改不會影響到實際資料")]
    [SerializeField]
    private List<bool> SelectGameModes = new List<bool>()
    {
        false,
        false,
        false
    };
    /// <summary>gameModes[0] = IsStoryMode    gameModes[1] = IsMultiplayer   gameModes[2] = IsEndlessMode</summary>
    private static List<bool> GameModes = new List<bool>()
    {
        false,
        false,
        false
    };
    public List<bool> p_GameModes
    {
        get { return GameModes; }
        set { GameModes = value; }
    }
    /// <summary>gameModes[0] = IsStoryMode    gameModes[1] = IsMultiplayer   gameModes[2] = IsEndlessMode，檢查是否「是指定的模式」 
    /// </summary> <param name="actionToExecute"></param>
    public static void ExecuteIfIsSpecifyMode(Action actionToExecute, int GMIndex)
    {
        if (GameManager.instance != null)
        {
            if (GameManager.instance.p_GameModes == SelectGameMode.GetGamemodesBools(GMIndex)) // Check if it is Specify mode
            {
                Debug.Log($"觸發 遊戲模式{GMIndex} 特有的事件 {actionToExecute}");
                actionToExecute?.Invoke(); // Execute the provided action
            }
        }
    }

    /// <summary>檢查是否「不是無盡模式」 </summary> <param name="actionToExecute"></param>
    public static void ExecuteIfNotEndlessMode(Action actionToExecute)
    {
        // if (GameManager.instance != null)
        // {
        if (GameManager.instance.p_GameModes != SelectGameMode.GetGamemodesBools(2)) // Check if it's not endless mode
        {
            actionToExecute?.Invoke(); // Execute the provided action
        }
        // }
    }
    #region  Unity Life Cycle Function

    // 理論上GM在開啟後不會被禁用，所以把「只會觸發一次的功能」寫在這
    void Awake() // 在生成（instantiated）的時候就開始執行。（不過呢，如果GameObject在一開始是Inactive狀態就不會執行，等到active後才會執行），
    {
        GameManagerSingleton();
    }

    void Update()
    {
        UpdateBoolDataForEditorView();
    }

    #endregion


    #region  SelectGameMode

    public string SelectGameModeTag = "SelectGameMode";
    public string HomePageNameTag = "HomePage";
    [SerializeField] private SelectGameMode selectGameMode;

    void UpdateBoolDataForEditorView()
    {
        if (InEditorState != IsInEditor)
        {
            InEditorState = IsInEditor;
        }

        if (SelectGameModes.Count != 3)
        {
            SelectGameModes = new List<bool>()
            {
                false,
                false,
                false
            };
        }
        if (SelectGameModes[0] != GameModes[0])
            SelectGameModes[0] = GameModes[0];

        if (SelectGameModes[1] != GameModes[1])
            SelectGameModes[1] = GameModes[1];

        if (SelectGameModes[2] != GameModes[2])
            SelectGameModes[2] = GameModes[2];
    }

    void FindSelectGameMode()
    {
        // Debug.Log($"目前場景: {CurrentSceneName}");
        if (CurrentSceneName == "HomePage")
        {
            GameObject selectMode = GameObject.FindWithTag(SelectGameModeTag);
            if (selectMode != null)
            {
                selectMode.GetComponent<SelectGameMode>().gameObject.SetActive(true);
                selectGameMode = selectMode.GetComponent<SelectGameMode>();
            }
        }
        else
        {
            selectGameMode = null;
        }

    }
    #endregion
    #region  Scene Name

    public string CurrentSceneName = "";
    public MapType CurrentMapArea;

    public bool IsSceneInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i); // 獲取場景的完整路徑
            string name = System.IO.Path.GetFileNameWithoutExtension(path); // 提取場景名稱

            if (name == sceneName)
            {
                Debug.Log("Scene 存在於 Build Settings 中: " + sceneName);
                return true;
            }
        }

        Debug.LogError("Scene 不存在於 Build Settings 中: " + sceneName);
        return false;
    }

    /// <summary>以SceneName進行場景的切換Scene</summary>
    public void ChangeSceneByName(string SceneName)
    {
        SetALLManagerActive();
        IsSceneInBuild(SceneName);
        SceneManager.LoadScene(SceneName);
    }

    /// <summary>以BuildIndex進行場景的切換Scene</summary>
    public void ChangeSceneByIndex(int SceneIndex)
    {
        SetALLManagerActive();
        SceneManager.LoadScene(SceneIndex);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) // 在場景載入後調用的方法
    {
        UI_SceneSwitcher sceneSwitcher = FindAnyObjectByType<UI_SceneSwitcher>();
        Debug.Log($"UI_SceneSwitcher 是否需要觸發轉場: {sceneSwitcher != null}");

        if (sceneSwitcher != null)
        {
            sceneSwitcher.PlayTPTrans();
            return;
        }
        Init();
        if (p_GameModes != SelectGameMode.GetGamemodesBools(0) && CurrentMapArea == MapType.L1_Ifir)
        {
            p_GameModes = SelectGameMode.GetGamemodesBools(0);
            Debug.Log($"玩家在故事模式的地圖中，但遊戲模式不正確，進行修正 {p_GameModes}");
        }
        else if (p_GameModes != SelectGameMode.GetGamemodesBools(2) && CurrentMapArea == MapType.EndlessModeRoom)
        {
            p_GameModes = SelectGameMode.GetGamemodesBools(2);
            Debug.Log($"玩家在無盡模式的地圖中，但遊戲模式不正確，進行修正 {p_GameModes}");
        }
    }

    private void LogCurrentSceneInfo()  // 取得並輸出當前場景的名稱和 Build Index
    {
        Scene currentScene = SceneManager.GetActiveScene(); // 獲取當前場景
        int buildIndex = currentScene.buildIndex; // 場景的 Build Index
        string sceneName = currentScene.name; // 場景的名稱
        CurrentSceneName = sceneName;

        Debug.Log($"目前場景名稱: {CurrentSceneName}, Build Index: {buildIndex}");

        foreach (MapType map in Enum.GetValues(typeof(MapType)))
        {
            if (CurrentSceneName == map.ToString())
            {
                CurrentMapArea = map;
                return;
            }
        }
        CurrentMapArea = MapType.Undefined;
        Debug.LogWarning($"CurrentSceneName: {CurrentSceneName} Can Not find correspond MapType,maybe sceneName is not in MapType."); // 破英文
        return;

    }

    #endregion

    #region  Init  


    void Init()
    {
        LogCurrentSceneInfo();   // 初始化時先取得當前場景資訊
        FindSelectGameMode();

        IninManager(ref DialogueManagerInstance, ref temp_DialogueManager, DIALOGUE_MANAGER);
        IninManager(ref TurnBaseBattleManagerInstance, ref temp_TurnBaseBattleManager, TURN_BASE_BATTLE_MANAGER);

        cheatManager = GetComponent<CheatManager>();
        cheatManager.InitCheatFromGameManager();
    }


    #region  Managers

    public const string DIALOGUE_MANAGER = "DialogueManager";

    public const string TURN_BASE_BATTLE_MANAGER = "TurnBaseBattleManager";

    void SetALLManagerActive() // 如果物件的啟用狀態為false，是沒辦法指派引用或初始化的，因此需要在轉場前確保管理器啟用，這樣才不會發生指派引用或初始化失敗的情況
    {
        if (DialogueManagerInstance != null)
            SetActiveInstance(ref DialogueManagerInstance, true);

        if (TurnBaseBattleManagerInstance != null)
            SetActiveInstance(ref TurnBaseBattleManagerInstance, true);
    }
    void SetActiveInstance<T>(ref T instance, bool isActive) where T : Component
    {
        instance.gameObject.SetActive(isActive);
    }

    void IninManager<T>(ref T instance, ref T temp_instance, string tagName) where T : ManagerMono<T>
    {
        // 先確保管理器找到
        if (instance == null)
        {
            FindManagerInstance(ref instance, tagName);
            // Debug.Log($"目前場景名稱: {CurrentSceneName}, 尋找{tagName}: {instance != null}");
        }

        temp_instance = instance;
        // 檢查所有管理器都已加載
        if (instance != null)
        {
            SetActiveInstance(ref instance, true);
            instance.InitFromGameManager();

            Debug.Log($"Init 目前場景名稱: '{CurrentSceneName}' , ManagerTag: '{tagName}' , '{instance.name}' 已初始化");
        }

    }

    /// <summary> 只會在沒有管理器時找，正常運作的話，每個管理器只會觸發一次。參數：1.管理器實例　2.Tag名稱</summary>
    void FindManagerInstance<T>(ref T instance, string tagName) where T : Component
    {
        // Find all objects with the specified tag
        GameObject[] managerObjects = GameObject.FindGameObjectsWithTag(tagName);

        if (managerObjects.Length == 0)
        {
            // Debug.LogWarning($"找不到標籤為 '{tagName}' 的物件！");
            return;
        }
        else
        {
            foreach (GameObject obj in managerObjects)
            {
                T currentComponent = obj.GetComponent<T>();
                if (instance == null && currentComponent != null)
                {
                    instance = currentComponent;
                    // DontDestroyOnLoad(instance);

                    // Debug.Log($"保存物件: {instance.name}，標籤: '{tagName}'。");
                }
                else
                {
                    obj.SetActive(false);
                    Destroy(obj);
                    // Debug.Log($"刪除重複的物件: {obj.name}，標籤: '{tagName}'。");
                    // Debug.Log($"目前已有的物件: {instance.name}，標籤: '{tagName}'。");
                }
            }
        }
    }

    #endregion

    void GameManagerSingleton()
    {
        if (instance == null)
        {
            instance = this;
            // 理論上GM在開啟後不會被禁用或刪除引用，所以把「只會綁定一次的功能」寫在這
            SceneManager.sceneLoaded += OnSceneLoaded; // 註冊場景載入事件
            DontDestroyOnLoad(gameObject); // 確保跨場景存在
        }
        else if (instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
            return;
        }

    }

    #endregion
}
