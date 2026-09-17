using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.GlobalEnums;
using System;

public class CheatManager : MonoBehaviour
{
    [SerializeField] private GameManager gameManager;

    [Header("作弊的開關狀態")]

    [Header("這是編輯器裡用來顯示實際上的全域靜態bool資料，調改不會影響到實際資料。 CheatState 變了 IsCheat 也變")]
    [SerializeField] private bool IsCheat;
    public static bool CheatState;

    [SerializeField] private bool IsTurnBaseBattleCheat;
    public static bool TurnBaseBattleCheatState = false;

    [SerializeField] private bool IsDialogueCheat;
    public static bool DialogueCheatState = false;

    [Space(10)]

    [Header("作弊UI Prefabs")]
    // public string CheatEmptyTag = "GameCheatEmpty";
    // public GameObject GameCheatMenuEmpty_Prefab; // 作弊選單的Prefab，要建立在CanvaPrefab的子物件
    [SerializeField] private string GameCheatMenuCanvaTag = "GameCheatMenuCanvas";
    public GameObject CheatCanva_Prefab;
    [Space(10)]

    [Header("作弊UI")]
    [SerializeField] private GameObject CheatCanvas;
    [SerializeField] private GameObject GameCheatEmpty;
    [SerializeField] private GameObject GameCheatPanel;
    [Space(10)]
    [Header("作弊UI Buttons")]
    [SerializeField] private Button Button_IsInEditor;
    [SerializeField] private Button Button_EnableCheat;
    [SerializeField] private GameObject CheatEnableMenu;
    [SerializeField] private GameObject Buttons_Cheat;
    [SerializeField] private Button BattleCheat;
    [SerializeField] private Button DialogueCheat;
    [SerializeField] private Button GoldCheat;
    [SerializeField] private Button PlayerMoveResetCheat;

    [Header("地圖傳送 Buttons")]
    [SerializeField] private GameObject MapsTransportMenu;
    [SerializeField] private GameObject Maps_Lists;
    [SerializeField] private Button Level1;
    [SerializeField] private GameObject L1_Levels;
    [SerializeField] private List<Button> L1_Level_List = new List<Button>(7);

    [SerializeField] private Button L1_1;
    [SerializeField] private Button L1_2;
    [SerializeField] private Button L1_3;
    [SerializeField] private Button L1_4;
    [SerializeField] private Button L1_5;
    [SerializeField] private Button L1_6;
    [SerializeField] private Button L1_7;



    public PlayerInventory PlayerInventory;
    public S001_PlayerController PlayerController;
    public Endless_GM_S001_PlayerController endlessplayercontroller;
    // public void LoadScene()
    // {
    //     testSceneName = null;
    //     testSceneName = ToScene.ToString();
    //     SceneManager.LoadScene(testSceneName);
    // }

    #region Button Functions

    #region  Transport
    [Header("作弊地圖傳送")]

    public MapType CheatTransportMapArea = MapType.Undefined;
    private string transportMapName = "";

    void Level1_1Transport()
    {
        GameManager.instance.ChangeSceneByName(MapType.L1_Ifir.ToString()); // 傳送到L1_Ifir

        // 但因為所有小關卡都在同一個Scene，故傳送後須再作調整，將小星星放置正確的關卡點上

    }

    void L1TransportListExpand()
    {
        if (Level1.gameObject.transform.GetChild(1).gameObject.activeInHierarchy)
        {
            Level1.gameObject.transform.GetChild(1).gameObject.SetActive(false);
        }
        else
        {
            Level1.gameObject.transform.GetChild(1).gameObject.SetActive(true);
        }
    }
    #endregion

    void ShowButton_EnableCheat()
    {
        if (gameManager.IsInEditor)
        {
            gameManager.IsInEditor = false;
        }
        else
        {
            gameManager.IsInEditor = true;
        }
        // Button_IsInEditor.gameObject.SetActive(gameManager.IsInEditor);
        Button_EnableCheat.gameObject.SetActive(gameManager.IsInEditor);

        Debug.Log($" gameManager.IsInEditor: {gameManager.IsInEditor}");


    }
    void OpenCheatMenu()
    {
        if (CheatState)
        {
            CheatState = false;
            Button_EnableCheat.transform.GetChild(0).GetComponent<TMP_Text>().text = "Enabled Cheat";
        }
        else
        {
            CheatState = true;
            Button_EnableCheat.transform.GetChild(0).GetComponent<TMP_Text>().text = "Disabled Cheat";

        }
        MapsTransportMenu.SetActive(CheatState);
        CheatEnableMenu.SetActive(CheatState);
        GameCheatPanel.SetActive(CheatState);

    }

    void SetTurnBaseBattleCheat()
    {
        if (IsTurnBaseBattleCheat) // 勾選後的Bool值
        {
            IsTurnBaseBattleCheat = false;
        }
        else
        {
            IsTurnBaseBattleCheat = true;
        }

        TurnBaseBattleCheatState = IsTurnBaseBattleCheat; // 更改全域靜態的資料
        if (GameManager.TurnBaseBattleManagerInstance != null)
        {
            GameManager.TurnBaseBattleManagerInstance.BattleUI.OpenBattleCheatFunction();
        }
        else
        {
            Debug.LogError($"作弊開啟失敗 TurnBaseBattleManagerInstance 不存在於當前的Scene中");

        }

        // IsCheat = isGoingToCheat;
        // CheatState = IsCheat;
        Debug.Log($"TurnBaseBattleCheatState: {TurnBaseBattleCheatState}");
    }
    void SetDialogueCheat()
    {
        if (IsDialogueCheat) // 勾選後的Bool值
        {
            IsDialogueCheat = false;
        }
        else
        {
            IsDialogueCheat = true;
        }

        DialogueCheatState = IsDialogueCheat; // 更改全域靜態的資料
        if (GameManager.DialogueManagerInstance != null)
        {
            GameManager.DialogueManagerInstance.OpenDialogueCheatFunction();
        }
        else
        {
            Debug.LogError($"作弊開啟失敗 DialogueManagerInstance 不存在於當前的Scene中");

        }


        Debug.Log($"DialogueCheatState: {DialogueCheatState}");
    }

    void GainGoldCheat()
    {
        PlayerInventory.gold += 1000;
        PlayerInventory.UpdateGoldText();
    }

    void ResetMoveCheat()
    {
        if (PlayerController !=null)
        {
            PlayerController.ResetPlayerMove();
        }
        else
        {
            endlessplayercontroller.ResetPlayerMove();
        }

        
    }


    #endregion

    void Update()
    {
        if (IsCheat != CheatState) // 實時更新靜態變數
        {
            IsCheat = CheatState;
        }
    }

    #region  Init

    void InitCheatCanvas()
    {
        GameCheatEmpty = CheatCanvas.transform.GetChild(0).gameObject;
        Button_IsInEditor = CheatCanvas.transform.GetChild(1).GetComponent<Button>();

        GameObject GetGameCheatEmptyChild(int index) => GameCheatEmpty.transform.GetChild(index).gameObject;

        GameCheatPanel = GetGameCheatEmptyChild(0);
        Button_EnableCheat = GetGameCheatEmptyChild(1).GetComponent<Button>();

        CheatEnableMenu = GetGameCheatEmptyChild(2);
        InitCheatFunction();

        MapsTransportMenu = GetGameCheatEmptyChild(3);
        InitMapsTransport();
    }
    void InitMapsTransport()
    {
        GameObject GetMapsTransportMenuChild(int index) => MapsTransportMenu.transform.GetChild(index).gameObject;

        Maps_Lists = GetMapsTransportMenuChild(2);

        GameObject GetMaps_ListsChild(int index) => Maps_Lists.transform.GetChild(index).gameObject;

        Level1 = GetMaps_ListsChild(0).GetComponent<Button>();

        L1_Levels = Level1.transform.GetChild(1).gameObject;
        GameObject GetL1_LevelsChild(int index) => L1_Levels.transform.GetChild(index).gameObject;
        L1_Level_List.Clear();
        L1_Level_List.Add(GetL1_LevelsChild(0).GetComponent<Button>());
        L1_Level_List.Add(GetL1_LevelsChild(1).GetComponent<Button>());
        L1_Level_List.Add(GetL1_LevelsChild(2).GetComponent<Button>());
        L1_Level_List.Add(GetL1_LevelsChild(3).GetComponent<Button>());
        L1_Level_List.Add(GetL1_LevelsChild(4).GetComponent<Button>());
        L1_Level_List.Add(GetL1_LevelsChild(5).GetComponent<Button>());
        L1_Level_List.Add(GetL1_LevelsChild(6).GetComponent<Button>());

        AddMapsTransportButtonListeners();
    }

    void AddMapsTransportButtonListeners()
    {
        RemoveAllButtonListeners(Level1);
        Level1.GetComponent<Button>().onClick.AddListener(L1TransportListExpand);

        RemoveAllButtonListeners(L1_Level_List[0]);
        L1_Level_List[0].GetComponent<Button>().onClick.AddListener(Level1_1Transport);
    }

    void RemoveAllButtonListeners(Button button)
    {
        if (button.onClick.GetPersistentEventCount() > 0)
        {
            button.onClick.RemoveAllListeners();
        }
    }

    void InitCheatFunction()
    {
        GameObject GetCheatEnableMenuChild(int index) => CheatEnableMenu.transform.GetChild(index).gameObject;

        Buttons_Cheat = GetCheatEnableMenuChild(2);
        GameObject GetButtons_CheatChild(int index) => Buttons_Cheat.transform.GetChild(index).gameObject;

        BattleCheat = GetButtons_CheatChild(0).GetComponent<Button>();
        DialogueCheat = GetButtons_CheatChild(1).GetComponent<Button>();
        GoldCheat = GetButtons_CheatChild(2).GetComponent<Button>();
        PlayerMoveResetCheat = GetButtons_CheatChild(3).GetComponent<Button>();

        AddCheatFunctionButtonListeners();

        // GameCheatPanel.SetActive(false);
        // Button_EnableCheat.gameObject.SetActive(true);
        // CheatEnableMenu.SetActive(false);
    }

    void AddCheatFunctionButtonListeners()
    {
        RemoveAllButtonListeners(Button_EnableCheat);
        Button_EnableCheat.GetComponent<Button>().onClick.AddListener(OpenCheatMenu);
        // Debug.Log($"Button_EnableCheat GetPersistentEventCount: {Button_EnableCheat.GetComponent<Button>().onClick.GetPersistentEventCount()}");

        RemoveAllButtonListeners(Button_IsInEditor);
        Button_IsInEditor.onClick.AddListener(ShowButton_EnableCheat);
        // Debug.Log($" Button_IsInEditor.onClick.GetPersistentEventCount(): {Button_IsInEditor.onClick.GetPersistentEventCount()}");

        RemoveAllButtonListeners(BattleCheat);
        BattleCheat.GetComponent<Button>().onClick.AddListener(SetTurnBaseBattleCheat);

        RemoveAllButtonListeners(DialogueCheat);
        DialogueCheat.GetComponent<Button>().onClick.AddListener(SetDialogueCheat);

        RemoveAllButtonListeners(GoldCheat);
        GoldCheat.GetComponent<Button>().onClick.AddListener(GainGoldCheat);

        RemoveAllButtonListeners(PlayerMoveResetCheat);
        PlayerMoveResetCheat.GetComponent<Button>().onClick.AddListener(ResetMoveCheat);
    }


    public void InitCheatFromGameManager()
    {
        if (gameManager == null)
            gameManager = GameManager.instance;
        Init();
        InitActive();
    }

    void InitActive()
    {
        if (gameManager.IsInEditor)
        {
            Button_EnableCheat.gameObject.SetActive(true);
        }
        else
        {
            Button_EnableCheat.gameObject.SetActive(false);
        }

        if (IsCheat)
        {
            GameCheatPanel.SetActive(true);

            Button_EnableCheat.gameObject.SetActive(true);
            CheatEnableMenu.SetActive(true);
            MapsTransportMenu.SetActive(true);
        }
        else
        {
            GameCheatPanel.SetActive(false);

            Button_EnableCheat.gameObject.SetActive(false);
            CheatEnableMenu.SetActive(false);
            MapsTransportMenu.SetActive(false);
        }

        L1_Levels.gameObject.SetActive(false);
    }

    void Init()
    {
        GameObject canvas = null;
        // 嘗試尋找場景中的作弊 UI（尋找具有特定名稱或標籤的物件）
        GameObject existingCheatUI = GameObject.FindWithTag(GameCheatMenuCanvaTag);

        // 如果找不到作弊 UI，則創建一個新的
        if (existingCheatUI == null)
        {
            canvas = Instantiate(CheatCanva_Prefab); // 創建一個新的CheatCanvas
            canvas.name = "CheatCanvas"; // 設置 Canvas 的名稱
        }
        else
        {
            Debug.Log("作弊功能的 Canva 已存在。");
            canvas = existingCheatUI;
        }

        CheatCanvas = canvas;
        InitCheatCanvas();
    }

    #endregion

}
