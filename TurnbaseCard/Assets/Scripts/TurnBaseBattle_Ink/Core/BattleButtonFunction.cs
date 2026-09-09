using UnityEngine;
using Assets.Scripts.GlobalEnums;
using Assets.Scripts.Dialogue;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums.BattleEnum;
using Spine.Unity;
using System.Collections;

public class BattleButtonFunction : MonoBehaviour
{
    public S001_PlayerController playerController;

    [SerializeField] List<bool> gameModes;


    [Header("正比副團長動畫設定")]
    public GameObject Jephthah;
    public SkeletonGraphic JephthahAni;

    [Header("正比聖女動畫設定")]
    public SkeletonGraphic GodnessAni;
    public float fadeDuration = 0.5f; // 淡出的時間
    public float fadeInDuration = 0.2f; // 淡入的時間

    public TurnBaseBattleManager battleManager;
    public TurnBaseBattleUI battleUI;
    public GridManager gridManager; // 引用 GridManager 來訪問 currentLevelIndex 



    [SerializeField]
    public bool BattleIsEnd = false;

    private int currentDialogueIndex = 0; // 用來追蹤當前的對話索引
    private List<DialogueUnitType> types; // 儲存對話類型的列表
    private bool canExecute = true; // 控制是否可以执行的方法(YB)
    private float cooldownTime = 3f; // 冷却时间（秒）(YB)

    [SerializeField] private DialogueTypingEffect dialogueTypingEffect; // 引用 DialogueTypingEffect


    public void SetTurnBaseBattleManager(TurnBaseBattleManager manager) => this.battleManager = manager;


    [Header("測試")]
    [SerializeField] private CharacterType test_CharacterType;
    [SerializeField] private EnemyType test_EnemyType;

    [SerializeField][Range(1, 6)] private int diceValue = 1;
    [SerializeField][Range(0, 12)] private int bgIndex = 1;
    [SerializeField][Range(0, 12)] private int bgmIndex = 1;
    public List<bool> gMode = new() { true, false, false };


    private static bool hasTriggeredGuideDialogue = false;
    private static bool hasTriggeredGuideDialogue2 = false;
    private static bool hasTriggeredGuideDialogue3 = false;

    void Start()
    {
        // 確保取得 DialogueTypingEffect 實例
        dialogueTypingEffect = FindAnyObjectByType<DialogueTypingEffect>();
    }


    #region Han's Functions








    // 假設這是要在 Level 6 時觸發的功能
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

    IEnumerator FadeInForJephthah()
    {
        Color startColor = new Color(JephthahAni.color.r, JephthahAni.color.g, JephthahAni.color.b, 0f); // 完全透明
        Color endColor = new Color(startColor.r, startColor.g, startColor.b, 1f); // 完全不透明

        float elapsedTime = 0f;
        while (elapsedTime < fadeInDuration)
        {
            JephthahAni.color = Color.Lerp(startColor, endColor, elapsedTime / fadeInDuration);
            elapsedTime += Time.deltaTime;
            yield return null;
        }

        JephthahAni.color = endColor; // 確保最終顏色為不透明
    }

    #endregion

    #region Cheat
    public void PlayerHpToZero()
    {
        battleManager.BattleAction.ReducePlayerHpToZero();
    }
    public void RivalHpToZero()
    {
        battleManager.BattleAction.ReduceRivalHpToZero();
    }
    public void ToggleDialogueCheat()
    {
        if (battleUI.IsBattleCheat)
        {
            battleUI.IsBattleCheat = false;
        }
        else
        {
            battleUI.IsBattleCheat = true;
        }
        battleUI.CheatButtonsControl();
    }

    public void Test_EnemyAct()
    {
        TurnBaseBattleUnitDisplayData unit;
        if (battleManager.currentTurn == battleManager.GetPlayerOrder())
        {
            unit = battleManager.GetTemp_Target();
            battleManager.Test_ExchangeCardUserForPeriod();
            battleManager.BattleAction.TestEnemyBehavior(unit, diceValue);
            battleManager.Test_ExchangeCardUserForPeriod();

        }
        if (battleManager.currentTurn == battleManager.GetRivalOrder())
        {
            unit = battleManager.GetTemp_CardUser();
            battleManager.BattleAction.TestEnemyBehavior(unit, diceValue);
        }

    }

    #endregion


    #region Ink's Battle Start-Up Function

    public void IsReadyToNextTurn()
    {
        if (!canExecute) return; // 如果冷却中，直接返回

        canExecute = false; // 设置为不可执行
        battleManager.ToNextTurn();

        // 开始冷却计时
        StartCoroutine(ResetCooldown());
    }

    private IEnumerator ResetCooldown()
    {
        yield return new WaitForSeconds(cooldownTime); // 等待冷却时间
        canExecute = true; // 冷却结束，允许再次执行
    }

    /// <summary>
    /// 結束戰鬥並收拾戰鬥場面：關閉戰鬥 UI、標記戰鬥已結束，戰敗時重置暫存資料以便再戰。
    /// </summary>
    public void ExitBattle()
    {
        if (playerController != null)
        {
            playerController.firstbattleInStory = false;
            playerController.DisableBlocking();
        }

        if (battleUI == null || battleManager == null)
        {
            Debug.LogWarning($"[{name}] ExitBattle：battleUI 或 battleManager 未指派，無法正常結束戰鬥。");
            return;
        }

        // 關閉戰鬥畫面（原本只在無盡模式的結束流程中處理，無盡模式移除後改由此統一收尾）
        battleUI.CloseBattleUI();
        BattleIsEnd = true;

        if (battleManager.IsplayerWin == false)
        {
            battleManager.ResetTurnBaseBattleTempData();

            if (playerController != null)
            {
                playerController.AgainbattleInStory = true; // 允許再次挑戰
            }
        }
    }

    /// <summary>初始化劇情模式戰鬥的資料 參數：玩家是否優先、玩家的資料、可遊玩角色種類Type、敵人種類Type </summary>
    public void OpenBattle_EndlessMode(bool isPlayer1First, TurnBaseBattlePlayerData playerData,
   CharacterType playerOneType, EnemyType enemyType, int storyBattleOrder)
    {
        battleUI.OpenBattleUI();
        BattleIsEnd = false; // 開戰時重置結束旗標

        battleManager.GetTurnBaseBattleSetUp().Temp_InitEndlessModeTBBSetUp
        (
            true, playerData, playerData.UnitType, enemyType
        );
    }

    /// <summary>初始化劇情模式戰鬥的資料 參數：玩家是否優先、玩家的資料、可遊玩角色種類Type、敵人種類Type </summary>
    public void OpenBattle_StoryMode(bool isPlayer1First, TurnBaseBattlePlayerData playerData,
   CharacterType playerOneType, EnemyType enemyType, int storyBattleOrder)
    {
        playerController.EnableBlocking();

        battleUI.OpenBattleUI();
        BattleIsEnd = false; // 開戰時重置結束旗標

        battleManager.GetTurnBaseBattleSetUp().Temp_InitStoryModeTBBSetUp
        (
            true, playerData, playerData.UnitType, enemyType
        );
    }

    public void Test_OpenBattle()
    {
        TurnBaseBattlePlayerData battleUnitPlayerData = new TurnBaseBattlePlayerData();
        TurnBaseBattlePlayerData playerData = battleUnitPlayerData.InitPlayerInfo(test_CharacterType);

        OpenBattle_EndlessMode(true, playerData, playerData.UnitType, test_EnemyType, 0);
        Debug.Log($"Test_OpenBattle 測試戰鬥 將 {test_EnemyType} 傳入");
    }
    #endregion

}
