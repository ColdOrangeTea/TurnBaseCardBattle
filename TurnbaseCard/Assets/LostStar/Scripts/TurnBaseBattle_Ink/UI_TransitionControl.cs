using UnityEngine;
using UnityEngine.UI; // 用於控制UI元素
using DG.Tweening; // DOTween命名空間
using UnityEngine.SceneManagement; // 用於場景管理

public class UI_TransitionControl : MonoBehaviour
{
    [Header("UI Elements")]
    [SerializeField] private CanvasGroup canvasGroup; // 控制畫面透明度的CanvasGroup
    [SerializeField] private Button fadeButton; // 觸發漸暗效果的按鈕
    [SerializeField] private GameObject HomePage; // 需要隱藏的Panel
    [SerializeField] private CanvasGroup L1_Transition; // 控制L1_Transition的CanvasGroup
    [SerializeField] private GameObject Settlement; // 新增的Settlement Panel
    [SerializeField] private CanvasGroup L9_Transition;
    [SerializeField] private CanvasGroup L0_Transition;


    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 1f; // 漸暗效果的持續時間

    [Header("Settlement Settings")]
    [SerializeField] private float moveDistance = 1250f; // Y軸上移的距離
    [SerializeField] private float moveDuration = 1f; // 上移的時間
    [SerializeField] private float returnDelay = 5f; // 自動歸位的延遲時間

    private void Start()
    {
        // 確保按鈕和CanvasGroup已正確設定
        if (fadeButton != null)
        {
            fadeButton.onClick.AddListener(OnFadeButtonPressed);
        }

        // 檢查是否有設置L1_Transition
        if (L1_Transition != null && SceneManager.GetActiveScene().name == "L1_Ifir")
        {
            StartL1Transition();
        }

        // 檢查是否有設置L1_Transition
        if (L9_Transition != null && SceneManager.GetActiveScene().name == "L9_SpaceCraft")
        {
            StartL9Transition();
        }

        // 檢查是否有設置L1_Transition
        if (L0_Transition != null && SceneManager.GetActiveScene().name == "EndlessModeRoom")
        {
            StartL0Transition();
        }
    }

    private void OnFadeButtonPressed()
    {
        FadeToDark();
        HideHomePage();
    }

    private void FadeToDark()
    {
        if (canvasGroup != null)
        {
            // 使用DOTween淡出效果，將透明度變為1（完全黑）
            canvasGroup.DOFade(1f, fadeDuration).SetEase(Ease.Linear);
        }
    }

    private void HideHomePage()
    {
        if (HomePage != null)
        {
            // 隱藏 HomePage Panel
            HomePage.SetActive(false);
        }
    }

    private void StartL1Transition()
    {
        if (L1_Transition != null)
        {
            // 設置初始透明度為1（完全黑）
            L1_Transition.alpha = 1f;

            // 使用DOTween淡入效果，將透明度逐漸變為0（完全透明）
            L1_Transition.DOFade(0f, fadeDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 動畫完成後將L1_Transition改為不可視
                    L1_Transition.gameObject.SetActive(false);
                });
        }
    }

    public void TriggerSettlementMovement()
    {
        if (Settlement != null)
        {
            // 获取当前位置
            Vector3 originalPosition = Settlement.transform.localPosition;

            // 上移Settlement Panel
            Settlement.transform.DOLocalMoveY(originalPosition.y + moveDistance, moveDuration)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 移动完成后将Settlement改为不可见
                    Settlement.SetActive(false);

                    // 延迟指定时间后返回原位，但保持不可见
                    DOVirtual.DelayedCall(returnDelay, () =>
                    {
                        // 仍保持不可见状态并归位
                        Settlement.transform.DOLocalMoveY(originalPosition.y, moveDuration)
                            .SetEase(Ease.InQuad);
                    });
                });
        }
    }

    private void StartL9Transition()
    {
        if (L9_Transition != null)
        {
            Invoke(nameof(ExecuteL9Transition), 3f);
        }
    }

    private void ExecuteL9Transition()
    {
        // 設置初始透明度為1（完全黑）
        L9_Transition.alpha = 1f;

        // 使用DOTween淡入效果，將透明度逐漸變為0（完全透明）
        L9_Transition.DOFade(0f, fadeDuration)
            .SetEase(Ease.Linear)
            .OnComplete(() =>
            {
                // 動畫完成後將L1_Transition改為不可視
                L9_Transition.gameObject.SetActive(false);
            });
    }

    private void StartL0Transition()
    {
        if (L0_Transition != null)
        {
            // 設置初始透明度為1（完全黑）
            L0_Transition.alpha = 1f;

            // 使用DOTween淡入效果，將透明度逐漸變為0（完全透明）
            L0_Transition.DOFade(0f, fadeDuration)
                .SetEase(Ease.Linear)
                .OnComplete(() =>
                {
                    // 動畫完成後將L1_Transition改為不可視
                    L0_Transition.gameObject.SetActive(false);
                });
        }
    }
}



