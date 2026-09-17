using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Endless_GM_GameOver : MonoBehaviour
{
    [SerializeField] private Endless_GM_HintBox e_GM_HintBox;
    [Header("UI物件設定")]
    public GameObject GameOverEmpty; // 提示 Box 的 RectTransform
    public Canvas GameOverCanvas; // 顯示此UI的Canvas 
    public GameObject GameOverUI;
    public Image GameOverPanel; // 提示 Box 的 RectTransform      
    public Image PlayerProfile;
    public GameObject GameOverTexts;
    public Button BackToHomePageButton; // 跳回標題的按鈕
    public TMP_Text Text_BackToHomePageButton;
    public TMP_Text Text_GameOverTitle;

    public TMP_FontAsset GSGR;
    public Color Button_TextColor;

    [SerializeField] private bool isGameOverPanelOpen = false;

    [Header("字串設定")]
    public List<string> SettlementText = new List<string>(){
        @"旅途持續時間",
        @"擊敗敵人數量",
        @"收穫物資",
        @"繼 續",
        @"旅途結算",


    };
    public TMP_Text Text_TimeCount;
    public TMP_Text Text_TimeNumber;
    public TMP_Text Text_DefeatEnemyCount;
    public TMP_Text Text_DefeatEnemyNumber;
    public TMP_Text Text_FragmentsCount;
    public TMP_Text Text_FragmentsNumber;

    [SerializeField] private float elapsedTime; // 經過時間
    [SerializeField] private bool isTiming;     // 是否正在計時
    [SerializeField] private int DefeatEnemyCount = 0; // 打倒多少人
    [SerializeField] private int TimeCountCount = 0; // 經過時間
    [SerializeField] private int FragmentsCount = 0; // 過多少次地圖
    [Header("標題字移動動畫設定")]
    [SerializeField] private Vector2 TitleText_OriginPosition = new Vector2(0, 600); // 原本的位置
    [SerializeField] private Vector2 TitleText_PopOutPosition = new Vector2(0, 380); // 跳出後停留的位置
    [SerializeField] private float titleText_AnimationDuration = 1.1f; // 動畫持續時間
    public Ease TitleText_easeType = Ease.OutBack;

    [Header("結束空物件設定")]
    [SerializeField] private Vector2 empty_OriginPosition = new Vector2(0, 0); // 原本的位置

    [Header("按鈕移動動畫設定")]
    [SerializeField] private Vector2 button_OriginPosition = new Vector2(0, -800); // 原本的位置
    [SerializeField] private Vector2 button_PopOutPosition = new Vector2(0, -400); // 跳出後停留的位置
    [SerializeField] private float buttonAnimationDuration = 1.2f; // 動畫持續時間
    public Ease Button_easeType = Ease.InCubic;

    [Header("面板移動動畫設定")]

    [SerializeField] private Vector2 panel_OriginPosition = new Vector2(0, 0); // 跳出後停留的位置
    [SerializeField] private Vector2 panel_OriginSize = new Vector3(0, 1, 1); // 展開的位置（螢幕內）
    [SerializeField] private Vector2 panel_OpenedSize = new Vector3(1, 1, 1); // 展開的位置（螢幕內）
    [SerializeField] private float panelAnimationDuration = 1f; // 動畫持續時間
    public Ease Panel_easeType = Ease.InOutBack;

    [Header("玩家頭像晃動動畫設定")]
    [SerializeField] private float profileShakeAmplitude = 10f; // 晃動的幅度
    [SerializeField] private float profileShakeDuration = 0.5f; // 每次晃動的持續時間
    [SerializeField] private int profileShakeVibrato = 3; // 每次晃動的震動次數
    [SerializeField] private bool profileShakeLoop = true; // 是否循環晃動
    public Ease Profile_easeType = Ease.InOutSine;
    public LoopType profile_LoopType = LoopType.Yoyo;

    void Start()
    {
        Timing();
        Init();
        if (GameManager.instance.CurrentSceneName == MapType.L1_Ifir.ToString())
        {
            BackToHomePageButton.onClick.RemoveAllListeners();
            BackToHomePageButton.onClick.AddListener(PressGameOverButtonToSpaceCraft);
        }
        else if (GameManager.instance.CurrentSceneName == MapType.EndlessModeRoom.ToString())
        {
            BackToHomePageButton.onClick.RemoveAllListeners();
            BackToHomePageButton.onClick.AddListener(PressGameOverButtonToSpaceCraft);
        }
    }


    void Timing()
    {
        elapsedTime = 0f;
        isTiming = true;
    }
    void Update()
    {
        if (isTiming)
        {
            elapsedTime += Time.deltaTime;
        }
        // 先註解掉測試用的功能
        // if (Input.GetKeyDown(KeyCode.V))
        // {
        //     OpenGameOver();
        // }

    }

    public void PressGameOverButtonToHomePage()
    {
        SelectGameMode.ResetGameMode();
        GameManager.instance.ChangeSceneByName(MapType.HomePage.ToString());
    }
    public void PressGameOverButtonToSpaceCraft()
    {
        SelectGameMode.ResetGameMode();
        GameManager.instance.ChangeSceneByName(MapType.L9_SpaceCraft.ToString());
    }
    public void OpenGameOver()
    {
        // GameManager.ExecuteIfIsSpecifyMode(() =>
        // {
        isTiming = false;
        if (isGameOverPanelOpen)
        {
            isGameOverPanelOpen = false;

            if (e_GM_HintBox != null)
            {
                e_GM_HintBox.GetHintBoxButton().interactable = true;
            }

            // ResetUIPosition();
        }
        else
        {
            isGameOverPanelOpen = true;

            if (e_GM_HintBox != null)
            {
                e_GM_HintBox.GetHintBoxButton().interactable = false;
            }

            ResetUIPosition();

        }
        DisplayGameOverPanel(isGameOverPanelOpen);
        ExpandPanelBox(isGameOverPanelOpen); // 展开面板
        // }, 2);
    }

    void DisplayGameOverPanel(bool open)
    {
        if (open)
        {
            GameOverEmpty.gameObject.SetActive(true); // 提示 Box 的 RectTransform 
            GameOverCanvas.gameObject.SetActive(true); // 顯示此UI的Canvas  
            GameOverUI.gameObject.SetActive(true);
            BackToHomePageButton.gameObject.SetActive(true); // 跳回標題的按鈕
            GameOverPanel.gameObject.SetActive(true);
            GameOverTexts.gameObject.SetActive(true);

            Text_TimeCount.gameObject.SetActive(false);
            Text_TimeNumber.gameObject.SetActive(false);

            Text_DefeatEnemyCount.gameObject.SetActive(false);
            Text_DefeatEnemyNumber.gameObject.SetActive(false);

            Text_FragmentsCount.gameObject.SetActive(false);
            Text_FragmentsNumber.gameObject.SetActive(false);

            Text_GameOverTitle.gameObject.SetActive(true);
            Text_BackToHomePageButton.gameObject.SetActive(true);

            SetTextContent();

            // PlayerProfile.gameObject.SetActive(true);
            // GameOverTexts.gameObject.SetActive(true);
            // Text_BackToHomePageButton.gameObject.SetActive(true);
            // Text_GameOverTitle.gameObject.SetActive(true);
            // SetTextContent();
            // SetButtonsInteractable(true);// 動畫完成後重新啟用按鈕
        }
        else
        {
            InitActive();
        }

    }

    void ExpandPanelBox(bool open)
    {

        SetButtonsInteractable(false); // 禁用按鈕   
        Vector2 button_TargetPosition = open ? button_PopOutPosition : button_OriginPosition; // 設定目標位置
        Vector2 panel_TargetSize = open ? panel_OpenedSize : panel_OriginSize; // 設定目標位置
        Vector2 titleText_TargetSize = open ? TitleText_PopOutPosition : TitleText_OriginPosition; // 跳出後停留的位置

        // 確保之前的動畫已经完成或終止
        GameOverPanel.transform.DOKill(); // 终止之前的動畫
        BackToHomePageButton.GetComponent<RectTransform>().DOKill();
        Text_GameOverTitle.GetComponent<RectTransform>().DOKill();

        // 使用 DOAnchorPos 進行 GameOverPanel 的Size動畫，並在完成時執行回調，先面板，再按鈕
        GameOverPanel.transform.DOScale(panel_OpenedSize, panelAnimationDuration).SetEase(Panel_easeType)
            .OnComplete(() =>
            {
                Text_GameOverTitle.GetComponent<RectTransform>().DOAnchorPos(titleText_TargetSize, titleText_AnimationDuration)
                .SetEase(TitleText_easeType);

                BackToHomePageButton.GetComponent<RectTransform>().DOAnchorPos(button_TargetPosition, buttonAnimationDuration)
                    .SetEase(Button_easeType)
                    .OnComplete(() =>
                    {
                        PlayerProfile.gameObject.SetActive(true); // 顯示頭像
                        StartProfileShake(); // 啟動頭像晃動動畫

                        Text_TimeCount.gameObject.SetActive(true);
                        Text_TimeNumber.gameObject.SetActive(true);

                        Text_DefeatEnemyCount.gameObject.SetActive(true);
                        Text_DefeatEnemyNumber.gameObject.SetActive(true);

                        Text_FragmentsCount.gameObject.SetActive(true);
                        Text_FragmentsNumber.gameObject.SetActive(true);

                        SetButtonsInteractable(true);// 動畫完成後重新啟用按鈕
                    });
            });
    }

    void StartProfileShake()
    {
        if (PlayerProfile == null)
        {
            Debug.LogError("PlayerProfile 未設置。請檢查相關設定！");
            return;
        }

        RectTransform profileRect = PlayerProfile.GetComponent<RectTransform>();

        if (profileShakeLoop)
        {
            // 使用 DOTween 的 DOAnchorPosY 實現上下晃動並循環
            profileRect.DOAnchorPosY(profileRect.anchoredPosition.y + profileShakeAmplitude, profileShakeDuration)
                .SetEase(Profile_easeType)
                .SetLoops(-1, profile_LoopType); // 無限循環
        }
        else
        {
            profileRect.DOAnchorPosY(profileRect.anchoredPosition.y + profileShakeAmplitude, profileShakeDuration)
                .SetEase(Profile_easeType)
                .SetLoops(profileShakeVibrato, profile_LoopType);
        }
    }

    void SetTextContent()
    {
        // 將經過時間轉換為時:分:秒格式
        int hours = Mathf.FloorToInt(elapsedTime / 3600);
        int minutes = Mathf.FloorToInt((elapsedTime % 3600) / 60);
        int seconds = Mathf.FloorToInt(elapsedTime % 60);
        Text_TimeCount.text = SettlementText[0];
        Text_TimeNumber.text = $"{hours:00}:{minutes:00}:{seconds:00}";

        Text_DefeatEnemyCount.text = SettlementText[1];
        Text_DefeatEnemyNumber.text = $" {DefeatEnemyCount}";

        Text_FragmentsCount.text = SettlementText[2];
        Text_FragmentsNumber.text = SettlementText[5] + $"{FragmentsCount}";

        Text_BackToHomePageButton.color = Button_TextColor;
        Text_BackToHomePageButton.font = GSGR;
        Text_BackToHomePageButton.text = SettlementText[3];

        Text_GameOverTitle.text = SettlementText[4];

    }


    void SetButtonsInteractable(bool state) // 方法：設置按鈕的可用狀態
    {
        BackToHomePageButton.interactable = state;
    }
    void ResetUIPosition()
    {
        // GameOverEmpty.GetComponent<RectTransform>().anchoredPosition = emptyOrigin_Position;
        GameOverPanel.GetComponent<RectTransform>().anchoredPosition = panel_OriginPosition;
        GameOverPanel.GetComponent<RectTransform>().localScale = panel_OriginSize;
        Text_GameOverTitle.GetComponent<RectTransform>().anchoredPosition = TitleText_OriginPosition;

        BackToHomePageButton.GetComponent<RectTransform>().anchoredPosition = button_OriginPosition;
    }

    void InitActive()
    {
        GameOverEmpty.gameObject.SetActive(true); // 提示 Box 的 RectTransform 
        GameOverCanvas.gameObject.SetActive(false); // 顯示此UI的Canvas  
        GameOverUI.gameObject.SetActive(false);
        BackToHomePageButton.gameObject.SetActive(false); // 跳回標題的按鈕

        PlayerProfile.gameObject.SetActive(false);
        GameOverTexts.gameObject.SetActive(false);

        Text_BackToHomePageButton.gameObject.SetActive(false);

        Text_TimeCount.gameObject.SetActive(false);
        Text_TimeNumber.gameObject.SetActive(false);

        Text_DefeatEnemyCount.gameObject.SetActive(false);
        Text_DefeatEnemyNumber.gameObject.SetActive(false);

        Text_FragmentsCount.gameObject.SetActive(false);
        Text_FragmentsNumber.gameObject.SetActive(false);

        Text_GameOverTitle.gameObject.SetActive(false);

        GameOverPanel.gameObject.SetActive(false);
    }

    void Init()
    {
        if (e_GM_HintBox == null)
            Debug.LogError("e_GM_GameOver 未設置。請在 Inspector 中分配相應的按鈕物件。");

        // 檢查引用是否為 null
        if (GameOverEmpty == null)
            Debug.LogError("GameOverPanel 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (GameOverCanvas == null)
            Debug.LogError("GameOverCanvas 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (GameOverUI == null)
            Debug.LogError("GameOverUI 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (PlayerProfile == null)
            Debug.LogError("PlayerProfile 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (BackToHomePageButton == null)
            Debug.LogError("BackToHomePageButton 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (Text_BackToHomePageButton == null)
            Debug.LogError("Text_BackToHomePageButton 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (GameOverPanel == null)
            Debug.LogError("GameOverPanel 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (GameOverTexts == null)
            Debug.LogError("GameOverTexts 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (Text_BackToHomePageButton == null)
            Debug.LogError("Text_BackToHomePageButton 未設置。請在 Inspector 中分配相應的按鈕物件。");

        InitText();

        isGameOverPanelOpen = false;
        InitActive();
        ResetUIPosition();
        SetButtonsInteractable(false);


    }

    void InitText()
    {
        if (Text_TimeCount == null)
            Debug.LogError("Text_TimeCount 未設置。請在 Inspector 中分配相應的文字物件。");

        if (Text_TimeNumber == null)
            Debug.LogError("Text_TimeNumber 未設置。請在 Inspector 中分配相應的文字物件。");


        if (Text_DefeatEnemyCount == null)
            Debug.LogError("Text_DefeatEnemyCount 未設置。請在 Inspector 中分配相應的文字物件。");

        if (Text_DefeatEnemyNumber == null)
            Debug.LogError("Text_DefeatEnemyNumber 未設置。請在 Inspector 中分配相應的文字物件。");


        if (Text_FragmentsCount == null)
            Debug.LogError("Text_MoneyCount 未設置。請在 Inspector 中分配相應的文字物件。");

        if (Text_FragmentsNumber == null)
            Debug.LogError("Text_MoneyNumber 未設置。請在 Inspector 中分配相應的文字物件。");

        if (Text_GameOverTitle == null)
            Debug.LogError("Text_GameOverTitle 未設置。請在 Inspector 中分配相應的按鈕物件。");


    }
}