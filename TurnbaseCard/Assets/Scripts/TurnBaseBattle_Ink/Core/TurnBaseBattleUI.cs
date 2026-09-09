using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.GlobalEnums;
using TMPro;
using System.Collections.Generic;
using System;
using DG.Tweening;

public class TurnBaseBattleUI : MonoBehaviour
{
    public TurnBaseBattleManager manager;
    [Header("UI物件的Prefab")]
    [Space(5)]
    public GameObject BattleEmptyPrefab;
    public GameObject UICanvaPrefab;
    public GameObject BackGroundPrefab;

    [Header("UI物件")]
    [SerializeField]
    private Canvas UICanva;
    [SerializeField]
    private GameObject BattleEmpty;
    [SerializeField]
    private GameObject BattleUI;

    [Header("背景圖")]
    public List<Sprite> AllBackGrounds;
    [SerializeField]
    private Image BackGround;

    [Header("角色資訊圖")]
    [SerializeField]
    private GameObject PlayerOne;
    [SerializeField]
    private GameObject Player1Image;
    [SerializeField]
    private GameObject Player1NameAndDiceCount;
    [SerializeField]
    private GameObject Player1HPEmpty;
    [SerializeField]
    private GameObject Player1StatesEmpty;
    [SerializeField]
    private GameObject Player1_DicesEmpty;
    [SerializeField]
    private GameObject Player1Dices;


    [SerializeField]
    private GameObject PlayerTwo;
    [SerializeField]
    private GameObject Player2Image;
    [SerializeField]
    private GameObject Player2NameAndDiceCount;
    [SerializeField]
    private GameObject Player2HPEmpty;
    [SerializeField]
    private GameObject Player2StatesEmpty;
    [SerializeField]
    private GameObject Player2_DicesEmpty;
    [SerializeField]
    private GameObject Player2Dices;
    [Header("卡片")]
    [SerializeField]
    private GameObject Group_Cards;

    [Header("回合數")]
    [SerializeField]
    private GameObject TurnCountEmpty;
    [SerializeField]
    private TMP_Text TurnCountText;
    [SerializeField]
    private TMP_Text WhoseTurn_Text;

    [Header("結算")]
    public List<TMP_FontAsset> TextMaterials;
    public List<Color> TextColors;
    [SerializeField]
    private List<Sprite> VictorySprites;
    [SerializeField]
    private List<Sprite> LoseSprites;
    [SerializeField]
    private List<Sprite> playerProfileImages;

    [SerializeField]
    private GameObject SettlementEmpty;
    [SerializeField]
    private GameObject SettlementBlackPanel;

    [SerializeField] private GameObject settlement_InfoFrames;
    [SerializeField] private Image frame_Up;
    [SerializeField] private Image infoPanel;
    [SerializeField] private Image image_Player1Profile;
    [SerializeField] private TMP_Text Text_Player1_Name;
    [SerializeField] private GameObject LootsScroll;
    [SerializeField] private GameObject Loots_List;
    [SerializeField] List<TMP_Text> Loots;
    [SerializeField] private TMP_Text Text_LostConnect;
    [SerializeField] private Image frame_Down;
    [SerializeField] private TMP_Text Text_Settlement;
    [SerializeField] private GameObject SettlementButton;
    [Header("按鈕")]
    [SerializeField]
    private GameObject ButtonsEmpty;
    [SerializeField]
    private GameObject ToNextTurnButton;
    [SerializeField]
    private GameObject CheatButtons;
    [SerializeField]
    private GameObject ButtonFunctions;
    [SerializeField]
    private GameObject OpenBattleButton;
    [SerializeField]
    private GameObject CausePoisoned;
    [Header("遮擋UI")]
    [SerializeField]
    private GameObject EnemyActionPanel; // 遮擋用

    [Header("編輯器內作弊bool控制，如需要請手動在編輯器中勾選")]
    // public bool IsInEditor = false;
    [Header("測試按鈕面板")]
    public bool IsBattleCheat = false;
    public GameObject ShowCheatButton;
    [Tooltip("作弊功能總開關：控制是否顯示開啟作弊面板的按鈕。原本由 CheatManager.TurnBaseBattleCheatState 這個全域開關控制，該系統尚未移植，改為此處手動勾選。")]
    public bool EnableBattleCheat = false;


    #region  Get Set
    public GameObject GetPlayer1_DicesEmpty() => Player1_DicesEmpty;
    public GameObject GetPlayer1Dices() => Player1Dices;
    public GameObject GetPlayer2Dices() => Player2Dices;
    public GameObject GetGroup_Cards() => Group_Cards;
    public GameObject GetToNextTurnButton() => ToNextTurnButton;
    public GameObject GetEnemyActionPanel() => EnemyActionPanel;
    public TMP_Text GetTurnCountText() => TurnCountText;
    public TMP_Text GetWhoseTurn_Text() => WhoseTurn_Text;
    public GameObject GetBattleEmpty() => BattleEmpty;
    public GameObject GetButtonsEmpty() => ButtonsEmpty;

    public List<Sprite> GetVictorySprites() => VictorySprites;
    public List<Sprite> GetLoseSprites() => LoseSprites;

    #endregion

    #region "測試作弊用" 

    public void CheatButtonsControl()
    {
        if (IsBattleCheat == true)
        {
            CheatButtons.SetActive(true);
        }
        else
        {
            CheatButtons.SetActive(false);
        }
    }
    #endregion

    public void SetBackGround(int bgIndex)
    {
        if (bgIndex < AllBackGrounds.Count)
        {
            if (AllBackGrounds[bgIndex] != null)
                BackGround.sprite = AllBackGrounds[bgIndex];
        }
        else
        {
            BackGround.sprite = AllBackGrounds[0];
            Debug.LogWarning($"bgIndex: {bgIndex} 輸入不正確或者超過 AllBackGrounds.Count ! 指定為預設場景 AllBackGrounds[0]");
        }

    }

    public void OpenBattleCheatFunction()
    {
        if (EnableBattleCheat == true)
        {
            ShowCheatButton.SetActive(true);
        }
        else
        {
            ShowCheatButton.SetActive(false);
        }

        if (IsBattleCheat == true)
        {
            CheatButtons.SetActive(true);
        }
        else
        {
            CheatButtons.SetActive(false);
        }
    }
    public void InitFromTurnBaseBattleManager()
    {
        Init();
        InitActive();
    }

    void InitActive()
    {
        BattleEmpty.SetActive(true);
        BattleUI.gameObject.SetActive(false);
        ButtonsEmpty.gameObject.SetActive(true);
        ToNextTurnButton.SetActive(false);
        TurnCountText.text = "回合數：" + (manager.RoundCount + 1);
        SettlementEmpty.SetActive(false);
        EnemyActionPanel.SetActive(false);
        OpenBattleCheatFunction();

    }

    public void CloseBattleUI()
    {
        BattleUI.SetActive(false);
        ButtonsEmpty.SetActive(true);
        ToNextTurnButton.SetActive(false);
        CheatButtonsControl();
    }
    public void OpenBattleUI()
    {
        BattleEmpty.SetActive(true);
        BattleUI.SetActive(true);
        BackGround.gameObject.SetActive(true);

        SettlementEmpty.SetActive(false);
        LootsScroll.SetActive(false);
        Loots_List.SetActive(false);
        Text_LostConnect.gameObject.SetActive(false);
        ButtonsEmpty.SetActive(true);
        ToNextTurnButton.SetActive(true);
        CheatButtonsControl();
    }

    public void SetTurnText()
    {
        TurnCountText.text = "回合數：" + (manager.RoundCount + 1).ToString();
    }
    public void SetWhoseTurn()
    {
        WhoseTurn_Text.text = manager.curWhoseTurn.ToString() + "的回合";
    }
    public void IfPlayerCanAct()
    {

    }

    public void OpenBattleSettlement(string resultText)
    {
        Text_Settlement.text = resultText;
        SettlementEmpty.SetActive(true);


        PlaySettlementAnimation(SettlementEmpty);
    }

    #region  "物件初始化"

    #region  "Init Module"

    void InitBattleUI()
    {
        GameObject GetBattleUIChild(int index) => BattleUI.transform.GetChild(index).gameObject;

        BackGround = GetBattleUIChild(0).GetComponent<Image>();
        PlayerTwo = GetBattleUIChild(1);
        PlayerOne = GetBattleUIChild(2);
        Group_Cards = GetBattleUIChild(3);
        Player2_DicesEmpty = GetBattleUIChild(4);
        Player2Dices = Player2_DicesEmpty.transform.GetChild(1).gameObject;
        Player1_DicesEmpty = GetBattleUIChild(5);
        Player1Dices = Player1_DicesEmpty.transform.GetChild(1).gameObject;
        TurnCountEmpty = GetBattleUIChild(6);
        TurnCountText = TurnCountEmpty.transform.GetChild(1).GetComponent<TMP_Text>();
        WhoseTurn_Text = TurnCountEmpty.transform.GetChild(2).GetComponent<TMP_Text>();

        SettlementEmpty = GetBattleUIChild(7);
    }

    void InitButtons()
    {
        GameObject GetBattleButtonsChild(int index) => ButtonsEmpty.transform.GetChild(index).gameObject;

        ToNextTurnButton = GetBattleButtonsChild(0);
        ShowCheatButton = GetBattleButtonsChild(1).gameObject;
        CheatButtons = GetBattleButtonsChild(2);

        OpenBattleButton = CheatButtons.transform.GetChild(0).gameObject;
        CausePoisoned = CheatButtons.transform.GetChild(1).gameObject;
    }
    void InitPlayers()
    {
        GameObject GetPlayerOneChild(int index) => PlayerOne.transform.GetChild(index).gameObject;
        Player1Image = GetPlayerOneChild(0).gameObject;
        Player1NameAndDiceCount = GetPlayerOneChild(1).gameObject;
        Player1HPEmpty = GetPlayerOneChild(2).gameObject;
        Player1StatesEmpty = GetPlayerOneChild(3).gameObject;

        GameObject GetPlayerTwoChild(int index) => PlayerTwo.transform.GetChild(index).gameObject;
        Player2Image = GetPlayerTwoChild(0).gameObject;
        Player2NameAndDiceCount = GetPlayerTwoChild(1).gameObject;
        Player2HPEmpty = GetPlayerTwoChild(2).gameObject;
        Player2StatesEmpty = GetPlayerTwoChild(3).gameObject;



    }
    public void SetSettlementUI(bool isWin)
    {
        SettlementEmpty.SetActive(true);
        if (isWin)
        {
            frame_Up.sprite = VictorySprites[0];
            infoPanel.sprite = VictorySprites[1];
            frame_Down.sprite = VictorySprites[2];

            image_Player1Profile.sprite = playerProfileImages[0];

            Text_Settlement.color = TextColors[0];
            Text_Player1_Name.color = TextColors[0]; // 0 是勝利

            // Text_Player1_Name.color = new Color(0.333f, 0.996f, 1.0f); // RGB值對應 #55FEFF// 指定TMP_Text的顏色

            LootsScroll.gameObject.SetActive(true);
            Loots_List.gameObject.SetActive(true);
            Text_LostConnect.gameObject.SetActive(false);

            List<TMP_Text> Loots = new List<TMP_Text>();
            // 遍歷所有子物件
            foreach (Transform child in Loots_List.transform)
            {
                // 嘗試獲取 TMP_Text 組件
                TMP_Text tmpText = child.GetComponent<TMP_Text>();

                // 如果存在 TMP_Text 組件，則加入列表
                if (tmpText != null)
                {
                    Loots.Add(tmpText);
                }
            }

        }
        else
        {
            frame_Up.sprite = LoseSprites[0];
            infoPanel.sprite = LoseSprites[1];
            frame_Down.sprite = LoseSprites[2];

            image_Player1Profile.sprite = playerProfileImages[0];

            Text_Settlement.color = TextColors[1];
            Text_Player1_Name.color = TextColors[1]; // 1 是失敗
            Text_LostConnect.color = TextColors[1];

            LootsScroll.SetActive(false);
            Loots_List.SetActive(false);
            Text_LostConnect.gameObject.SetActive(true);
        }
    }

    void InitSettlement()
    {
        GameObject GetSettlementEmptyChild(int index) => SettlementEmpty.transform.GetChild(index).gameObject;
        SettlementBlackPanel = GetSettlementEmptyChild(0).gameObject;
        settlement_InfoFrames = GetSettlementEmptyChild(1).gameObject;

        frame_Up = settlement_InfoFrames.transform.GetChild(0).GetComponent<Image>();
        infoPanel = settlement_InfoFrames.transform.GetChild(1).GetComponent<Image>();
        frame_Down = settlement_InfoFrames.transform.GetChild(2).GetComponent<Image>();

        image_Player1Profile = infoPanel.transform.GetChild(0).GetComponent<Image>();

        Text_Player1_Name = infoPanel.transform.GetChild(1).GetComponent<TMP_Text>();
        LootsScroll = infoPanel.transform.GetChild(2).gameObject;
        Loots_List = LootsScroll.transform.GetChild(0).GetChild(0).gameObject;
        List<TMP_Text> Loots = new List<TMP_Text>();

        Text_LostConnect = infoPanel.transform.GetChild(3).gameObject.GetComponent<TMP_Text>();

        Text_Settlement = GetSettlementEmptyChild(2).gameObject.GetComponent<TMP_Text>();
        SettlementButton = GetSettlementEmptyChild(3).gameObject;
    }

    void AddButtonListeners() // 目前只有測試用的按鈕要設置
    {
        if (manager.BattleAction != null)
        {
            // UseCardButton.GetComponent<Button>().onClick.AddListener(manager.BattleAction.OnUseCard);
            CausePoisoned.GetComponent<Button>().onClick.AddListener(manager.BattleAction.CausePoisoned);
            // ToNextTurnButton.GetComponent<Button>().onClick.AddListener(manager.ToNextTurn);
        }
        else
        {
            Debug.LogWarning("沒有抓取到BattleAction!!");
        }
    }

    #endregion

    void Init()
    {
        InitBattleEmpty();
        GameObject GetBattleEmptyChild(int index) => BattleEmpty.transform.GetChild(index).gameObject;

        BattleUI = GetBattleEmptyChild(0);
        InitBattleUI();

        ButtonsEmpty = GetBattleEmptyChild(1);
        InitButtons();

        EnemyActionPanel = GetBattleEmptyChild(2);

        InitPlayers();
        InitSettlement();

        AddButtonListeners();
    }

    void InitBattleEmpty()
    {
        UICanva = GameObject.FindWithTag("UI_Canva")?.GetComponent<Canvas>() ??
        Instantiate(UICanvaPrefab).GetComponent<Canvas>();

        if (UICanva == null)
        {
            Debug.LogError("UI_Canva Null");
        }
        // 如果找到 BattleEmpty 則指定場上的 BattleEmpty，沒有則建立
        BattleEmpty = GameObject.FindWithTag("BattleEmpty") ?? Instantiate(BattleEmptyPrefab, UICanva.transform);

    }
    #endregion
    #region  "轉場動畫"
    public void PlaySettlementAnimation(GameObject settlementObject)
    {
        RectTransform settlementTransform = settlementObject.GetComponent<RectTransform>();

        // 定义动画序列
        Sequence sequence = DOTween.Sequence();

        // 使用DOJumpAnchorPos来模拟弹跳效果
        sequence.Append(settlementTransform.DOJumpAnchorPos(new Vector2(settlementTransform.anchoredPosition.x, 1000), 150, 1, 0.5f).SetEase(Ease.OutQuad))
                .Append(settlementTransform.DOJumpAnchorPos(new Vector2(settlementTransform.anchoredPosition.x, 0), 125, 1, 0.5f).SetEase(Ease.OutQuad))
                .Append(settlementTransform.DOJumpAnchorPos(new Vector2(settlementTransform.anchoredPosition.x, 250), 75, 1, 0.5f).SetEase(Ease.OutQuad))
                .Append(settlementTransform.DOJumpAnchorPos(new Vector2(settlementTransform.anchoredPosition.x, 0), 50, 1, 0.5f).SetEase(Ease.OutQuad))
                .Append(settlementTransform.DOJumpAnchorPos(new Vector2(settlementTransform.anchoredPosition.x, 100), 25, 1, 0.5f).SetEase(Ease.OutQuad))
                .Append(settlementTransform.DOJumpAnchorPos(new Vector2(settlementTransform.anchoredPosition.x, 0), 0, 1, 0.5f).SetEase(Ease.OutQuad));
    }

    #endregion
}
