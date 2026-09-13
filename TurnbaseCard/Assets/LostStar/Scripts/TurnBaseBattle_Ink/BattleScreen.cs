using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

public class BattleScreen : MonoBehaviour
{
    // EnemyAction_Panel 相关声明
    public GameObject EnemyAction_Panel;
    private bool wasPanelVisible = false;

    public Button moveButton;  // 按钮
    public RectTransform PlayerOne;  // PlayerOne 图片的RectTransform
    public float PlayerOneMoveDistance = 10f;  // PlayerOne 可在Inspector调整的移动距离
    public float PlayerOneMoveDuration = 1f;  // PlayerOne 可在Inspector调整的移动持续时间
    public float PlayerOneDelay = 0f;  // PlayerOne 可在Inspector调整的延迟时间

    public RectTransform PlayerTwo;  // PlayerTwo 图片的RectTransform
    public float PlayerTwoMoveDistance = 10f;  // PlayerTwo 可在Inspector调整的移动距离
    public float PlayerTwoMoveDuration = 1f;  // PlayerTwo 可在Inspector调整的移动持续时间
    public float PlayerTwoDelay = 0f;  // PlayerTwo 可在Inspector调整的延迟时间

    public RectTransform PlayerOneHPBar;  // PlayerOne HP Bar 的RectTransform
    public float PlayerOneHPBarDistance = 10f;  // PlayerOne HP Bar 可在Inspector调整的移动距离
    public float PlayerOneHPBarDuration = 1f;  // PlayerOne HP Bar 可在Inspector调整的移动持续时间
    public float PlayerOneHPBarDelay = 0f;  // PlayerOne HP Bar 可在Inspector调整的延迟时间

    public RectTransform PlayerTwoHPBar;  // PlayerTwo HP Bar 的RectTransform
    public float PlayerTwoHPBarDistance = 10f;  // PlayerTwo HP Bar 可在Inspector调整的移动距离
    public float PlayerTwoHPBarDuration = 1f;  // PlayerTwo HP Bar 可在Inspector调整的移动持续时间
    public float PlayerTwoHPBarDelay = 0f;  // PlayerTwo HP Bar 可在Inspector调整的延迟时间

    public RectTransform PlayerOneName;  // PlayerOneName 的RectTransform
    public float PlayerOneNameDistance = 10f;  // PlayerOneName HP Bar 可在Inspector调整的移动距离
    public float PlayerOneNameDuration = 1f;  // PlayerOneName 可在Inspector调整的移动持续时间
    public float PlayerOneNameDelay = 0f;  // PlayerOneName 可在Inspector调整的延迟时间

    public RectTransform PlayerTwoName;  // PlayerTwoName 的RectTransform
    public float PlayerTwoNameDistance = 10f;  // PlayerTwoName 可在Inspector调整的移动距离
    public float PlayerTwoNameDuration = 1f;  // PlayerTwoName 可在Inspector调整的移动持续时间
    public float PlayerTwoNameDelay = 0f;  // PlayerTwoName 可在Inspector调整的延迟时间

    public RectTransform EndButton;  // EndButton 的RectTransform
    public float EndButtonDistance = 10f;  // EndButton 可在Inspector调整的移动距离
    public float EndButtonDuration = 1f; //EndButton 可在Inspector调整的移动持续时间
    public float EndButtonDelay = 0f;  // EndButton 可在Inspector调整的延迟时间

    public RectTransform Dice;  // Dice 的RectTransform
    public float DiceDistance = 10f;  // Dice 可在Inspector调整的移动距离
    public float DiceDuration = 1f; //Dice 可在Inspector调整的移动持续时间
    public float DiceDelay = 0f;  // Dice 可在Inspector调整的延迟时间

    // 新增的卡片变量
    public RectTransform Card1;  // Card1 的RectTransform
    public float Card1MoveDistance = 10f;  // Card1 可在Inspector调整的移动距离
    public float Card1MoveDuration = 1f;  // Card1 可在Inspector调整的持续时间
    public float Card1Delay = 0f;  // Card1 可在Inspector调整的延迟时间
    public float Card1FadeOutDuration = 0.5f; // Card1 透明度变化的时长
    public float Card1BackDelay = 0f;
    public float Card1RSDelay = 0f;

    public RectTransform Card2;  // Card2 的RectTransform
    public float Card2MoveDistance = 10f;  // Card2 可在Inspector调整的移动距离
    public float Card2MoveDuration = 1f;  // Card2 可在Inspector调整的持续时间
    public float Card2Delay = 0f;  // Card2 可在Inspector调整的延迟时间
    public float Card2FadeOutDuration = 0.5f; // Card2 透明度变化的时长
    public float Card2BackDelay = 0f;
    public float Card2RSDelay = 0f;

    public RectTransform Card3;  // Card3 的RectTransform
    public float Card3MoveDistance = 10f;  // Card3 可在Inspector调整的移动距离
    public float Card3MoveDuration = 1f;  // Card3 可在Inspector调整的持续时间
    public float Card3Delay = 0f;  // Card3 可在Inspector调整的延迟时间
    public float Card3FadeOutDuration = 0.5f; // Card3 透明度变化的时长
    public float Card3BackDelay = 0f;
    public float Card3RSDelay = 0f;

    public RectTransform Card4;  // Card4 的RectTransform
    public float Card4MoveDistance = 10f;  // Card4 可在Inspector调整的移动距离
    public float Card4MoveDuration = 1f;  // Card4 可在Inspector调整的持续时间
    public float Card4Delay = 0f;  // Card4 可在Inspector调整的延迟时间
    public float Card4FadeOutDuration = 0.5f; // Card4 透明度变化的时长
    public float Card4BackDelay = 0f;
    public float Card4RSDelay = 0f;
    public float Card4RSDistance = 10f;  // Card4 特殊的 RoundStar 移动距离

    public GameObject BattleCanvas;  // BattleCanvas 的引用

    // 初始位置的變數
    private Vector2 initialPlayerOnePos;
    private Vector2 initialPlayerTwoPos;
    private Vector2 initialPlayerOneHPBarPos;
    private Vector2 initialPlayerTwoHPBarPos;
    private Vector2 initialPlayerOneNamePos;
    private Vector2 initialPlayerTwoNamePos;
    private Vector2 initialEndButtonPos;
    private Vector2 initialDicePos;
    private Vector2 initialCard1Pos;
    private Vector2 initialCard2Pos;
    private Vector2 initialCard3Pos;
    private Vector2 initialCard4Pos;


    // 为每个卡片创建 CanvasGroup 变量
    private CanvasGroup canvasGroup1;
    private CanvasGroup canvasGroup2;
    private CanvasGroup canvasGroup3;
    private CanvasGroup canvasGroup4;

    void Start()
    {
        // 初始化时记录面板的状态
        wasPanelVisible = EnemyAction_Panel.activeSelf;

        // 保存初始位置
        initialPlayerOnePos = PlayerOne.anchoredPosition;
        initialPlayerTwoPos = PlayerTwo.anchoredPosition;
        initialPlayerOneHPBarPos = PlayerOneHPBar.anchoredPosition;
        initialPlayerTwoHPBarPos = PlayerTwoHPBar.anchoredPosition;
        initialPlayerOneNamePos = PlayerOneName.anchoredPosition;
        initialPlayerTwoNamePos = PlayerTwoName.anchoredPosition;
        initialEndButtonPos = EndButton.anchoredPosition;
        initialDicePos = Dice.anchoredPosition;
        initialCard1Pos = Card1.anchoredPosition;
        initialCard2Pos = Card2.anchoredPosition;
        initialCard3Pos = Card3.anchoredPosition;
        initialCard4Pos = Card4.anchoredPosition;



        // 为按钮添加点击事件
        moveButton.onClick.AddListener(MovePlayers);

        // 获取每个卡片的 CanvasGroup 组件
        canvasGroup1 = Card1.GetComponent<CanvasGroup>();
        canvasGroup2 = Card2.GetComponent<CanvasGroup>();
        canvasGroup3 = Card3.GetComponent<CanvasGroup>();
        canvasGroup4 = Card4.GetComponent<CanvasGroup>();

        // 设置卡片初始透明度为 0
        canvasGroup1.alpha = 0;
        canvasGroup2.alpha = 0;
        canvasGroup3.alpha = 0;
        canvasGroup4.alpha = 0;
    }

    void Update()
    {
        // 檢測 EnemyAction_Panel 的活動狀態變化
        bool isPanelVisible = EnemyAction_Panel.activeSelf;

        // 原本：面板從可見變不可見時自動 CardMove() → RoundStar()，但 RoundStar 是
        // card.position.x + moveDistance（每次往右加、又不歸位），導致每次切回合卡片都往右飄。
        // 卡片每回合本來就會重抽刷新，不需要這個自動移動，故停用以修正飄移 bug。
        // if (wasPanelVisible && !isPanelVisible)
        // {
        //     CardMove();
        // }

        // 更新面板的狀態記錄
        wasPanelVisible = isPanelVisible;
    }

    public void SceenAni()
    {
        MovePlayers();
    }

    void MovePlayers()
    {
        // PlayerOne 向右移动，并应用延迟
        PlayerOne.DOAnchorPosX(PlayerOne.anchoredPosition.x + PlayerOneMoveDistance, PlayerOneMoveDuration)
                 .SetDelay(PlayerOneDelay);

        // PlayerOneHPBar 向右移动，并应用延迟
        PlayerOneHPBar.DOAnchorPosX(PlayerOneHPBar.anchoredPosition.x + PlayerOneHPBarDistance, PlayerOneHPBarDuration)
                 .SetDelay(PlayerOneHPBarDelay);

        // PlayerTwo 向左移动，并应用延迟
        PlayerTwo.DOAnchorPosX(PlayerTwo.anchoredPosition.x - PlayerTwoMoveDistance, PlayerTwoMoveDuration)
                 .SetDelay(PlayerTwoDelay);

        // PlayerTwoHPBar 向下移动，并应用延迟
        PlayerTwoHPBar.DOAnchorPosY(PlayerTwoHPBar.anchoredPosition.y - PlayerTwoHPBarDistance, PlayerTwoHPBarDuration)
                 .SetDelay(PlayerTwoHPBarDelay);

        // PlayerOneName 向右移动，并应用延迟
        PlayerOneName.DOAnchorPosX(PlayerOneName.anchoredPosition.x + PlayerOneNameDistance, PlayerOneNameDuration)
                 .SetDelay(PlayerOneNameDelay);

        // PlayerTwoName 向下移动，并应用延迟
        PlayerTwoName.DOAnchorPosY(PlayerTwoName.anchoredPosition.y - PlayerTwoNameDistance, PlayerTwoNameDuration)
                 .SetDelay(PlayerTwoNameDelay);

        // EndButton 向上移动，并应用延迟
        EndButton.DOAnchorPosY(EndButton.anchoredPosition.y + EndButtonDistance, EndButtonDuration)
                 .SetDelay(EndButtonDelay);

        
        Dice.DOAnchorPosY(Dice.anchoredPosition.y + DiceDistance, DiceDuration)
                .SetDelay(DiceDelay);

        // 移动和改变透明度的逻辑
        MoveAndFadeIn(canvasGroup1, Card1, Card1MoveDistance, Card1MoveDuration, Card1Delay, Card1FadeOutDuration);
        MoveAndFadeIn(canvasGroup2, Card2, Card2MoveDistance, Card2MoveDuration, Card2Delay, Card2FadeOutDuration);
        MoveAndFadeIn(canvasGroup3, Card3, Card3MoveDistance, Card3MoveDuration, Card3Delay, Card3FadeOutDuration);
        MoveAndFadeIn(canvasGroup4, Card4, Card4MoveDistance, Card4MoveDuration, Card4Delay, Card4FadeOutDuration);
    }

    void MoveAndFadeIn(CanvasGroup canvasGroup, RectTransform card, float moveDistance, float duration, float delay, float fadeInDuration)
    {
        card.DOAnchorPosX(card.anchoredPosition.x + moveDistance, duration)
            .SetDelay(delay);
        canvasGroup.DOFade(1, fadeInDuration)  // 从 0 到 1
            .SetDelay(delay);
    }

    void MoveAndFadeOut(CanvasGroup canvasGroup, RectTransform card, float moveDistance, float duration, float delay, float fadeInDuration)
    {
        card.DOAnchorPosX(card.anchoredPosition.x - moveDistance, duration)
            .SetDelay(delay);
        canvasGroup.DOFade(0, fadeInDuration)  // 从 0 到 1
            .SetDelay(delay);
    }

    public void ToggleEnemyActionPanel(bool isVisible)
    {
        // 设置面板的显示状态
        EnemyAction_Panel.SetActive(isVisible);
    }

    

    // 重置所有物件的位置和透明度
    public void ResetPositions()
    {
        // 使用保存的初始位置來重置位置
        PlayerOne.anchoredPosition = initialPlayerOnePos;
        PlayerTwo.anchoredPosition = initialPlayerTwoPos;
        PlayerOneHPBar.anchoredPosition = initialPlayerOneHPBarPos;
        PlayerTwoHPBar.anchoredPosition = initialPlayerTwoHPBarPos;
        PlayerOneName.anchoredPosition = initialPlayerOneNamePos;
        PlayerTwoName.anchoredPosition = initialPlayerTwoNamePos;
        EndButton.anchoredPosition = initialEndButtonPos;
        //Dice.anchoredPosition = initialDicePos;
        Card1.anchoredPosition = initialCard1Pos;
        Card2.anchoredPosition = initialCard2Pos;
        Card3.anchoredPosition = initialCard3Pos;
        Card4.anchoredPosition = initialCard4Pos;

        canvasGroup1.alpha = 0;  // 重置 Card1 透明度
        canvasGroup2.alpha = 0;  // 重置 Card2 透明度
        canvasGroup3.alpha = 0;  // 重置 Card3 透明度
        canvasGroup4.alpha = 0;  // 重置 Card4 透明度

        // 延迟重置 Dice 的位置
        StartCoroutine(ResetDicePositionWithDelay());
    }

    public void CardMove()
    {
        // 移动和改变透明度的逻辑
        RoundStar(canvasGroup1, Card1, Card1MoveDistance, Card1MoveDuration, Card1Delay, Card1FadeOutDuration);
        RoundStar(canvasGroup2, Card2, Card2MoveDistance, Card2MoveDuration, Card2Delay, Card2FadeOutDuration);
        RoundStar(canvasGroup3, Card3, Card3MoveDistance, Card3MoveDuration, Card3Delay, Card3FadeOutDuration);
        RoundStar(canvasGroup4, Card4, Card4MoveDistance, Card4MoveDuration, Card4Delay, Card4FadeOutDuration);
    }

    public void CardBack()
    {
        HandleCardBack(canvasGroup1, Card1, Card1MoveDistance, Card1MoveDuration, Card1FadeOutDuration, Card1BackDelay);
        HandleCardBack(canvasGroup2, Card2, Card2MoveDistance, Card2MoveDuration, Card2FadeOutDuration, Card2BackDelay);
        HandleCardBack(canvasGroup3, Card3, Card3MoveDistance, Card3MoveDuration, Card3FadeOutDuration, Card3BackDelay);
        HandleCardBack(canvasGroup4, Card4, Card4MoveDistance, Card4MoveDuration, Card4FadeOutDuration, Card4BackDelay);
    }

    private void HandleCardBack(CanvasGroup canvasGroup, RectTransform card, float moveDistance, float duration, float fadeOutDuration, float backDelay)
    {
        // 如果透明度为 0，直接跳过该物件
        if (Mathf.Approximately(canvasGroup.alpha, 0f))
        {
            return;
        }

        // 移动物件回原位，并应用自定义延迟
        card.DOMoveX(card.position.x - moveDistance, duration)
            .SetDelay(backDelay)
            .OnKill(() => StartCoroutine(CheckEnemyActionPanelState(canvasGroup, card, moveDistance, duration, fadeOutDuration, backDelay))); // 动画完成后启动协程

        // 设置透明度为 0，并应用自定义延迟
        canvasGroup.DOFade(0, fadeOutDuration)
            .SetDelay(backDelay);
    }

    private IEnumerator CheckEnemyActionPanelState(CanvasGroup canvasGroup, RectTransform card, float moveDistance, float duration, float fadeOutDuration, float backDelay)
    {
        // 等待 0.3 秒
        yield return new WaitForSeconds(0.3f);

        // 检查 EnemyAction_Panel 是否变为可视
        if (!EnemyAction_Panel.activeSelf)
        {
            // 如果 EnemyAction_Panel 没有变为可视，则触发 RoundStar
            RoundStar(canvasGroup, card, moveDistance, duration, fadeOutDuration, backDelay); // 不再偏移200单位
        }
    }


    private void RoundStar(CanvasGroup canvasGroup, RectTransform card, float moveDistance, float duration, float fadeOutDuration, float RSDelay)
    {
        // 如果是 Card4，使用 Card4RSDistance
        if (card == Card4)
        {
            moveDistance = Card4RSDistance;
        }

        card.DOMoveX(card.position.x + moveDistance, duration)
            .SetDelay(RSDelay);
        canvasGroup.DOFade(1, fadeOutDuration)  // 从 0 到 1
            .SetDelay(RSDelay);
    }

    public void DelayedResetPositions()
    {
        StartCoroutine(ResetPositionsWithDelay());
    }

    private IEnumerator ResetPositionsWithDelay()
    {
        // 等待3秒
        yield return new WaitForSeconds(3f);

        // 调用 ResetPositions 方法
        ResetPositions();
    }

    private IEnumerator ResetDicePositionWithDelay()
    {
        yield return new WaitForSeconds(1); // 延迟 10 秒
        Dice.anchoredPosition = initialDicePos;
    }
}
