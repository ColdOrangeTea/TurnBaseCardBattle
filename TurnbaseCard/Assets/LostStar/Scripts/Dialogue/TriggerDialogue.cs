using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// 對話 UI 的總控制器：綁定底部按鈕（快轉 / 自動 / 紀錄 / 跳過）的狀態與行為，
/// 處理點擊對話框推進對話，以及「跳過」按下後的淡出結束流程。
/// </summary>
public class TriggerDialogue : MonoBehaviour
{
    [SerializeField]
    private DialogueTypingEffect ContentTyper;  // 文字打字效果
    [SerializeField]
    private CanvasGroup DialogueGroup;          // 對話 UI 群組（跳過時整組淡出）
    [SerializeField]
    private GameObject DialogueBoxPanel;        // 對話框的 UI（包含 TMPText）

    [Header("對話紀錄")]
    [SerializeField]
    private GameObject LogUI;

    [Header("按鈕面板")]
    [Space(5)]
    [SerializeField]
    private GameObject FastForwardButton;       // 快轉（開關型：整段快速播放）
    [SerializeField]
    private GameObject AutoButton;              // 自動（開關型）
    [SerializeField]
    private GameObject LogButton;               // 紀錄（開關型）
    [SerializeField]
    private GameObject SkipButton;              // 跳過（單發型：淡出並結束對話）
    [SerializeField]
    private GameObject OpenButton;              // 開始對話

    [Header("跳過淡出設定")]
    [SerializeField][Min(0.05f)]
    private float fadeOutDuration = 0.6f;       // 跳過時對話 UI 的淡出秒數

    // 快取按鈕控制器，避免每次點擊都 GetComponent
    private BottomButtonController fastForwardButtonCtrl;
    private BottomButtonController autoButtonCtrl;
    private BottomButtonController logButtonCtrl;

    private Coroutine fadeRoutine;              // 淡出協程（非 null 表示正在淡出）

    /// <summary>對話 UI 淡出結束、完全收起後觸發（章節選擇畫面訂閱後可自動返回選單）。</summary>
    public event Action OnDialogueClosed;

    // Raycast 用的快取，避免每次點擊都配置新物件
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    private void Start()
    {
        CacheButtonControllers();
        ButtonBind();
        ContentTyper.OnDialogueFinished += EndDialogueWithFade; // 對白播畢再點擊 → 淡出結束
    }

    private void OnDestroy()
    {
        if (ContentTyper != null)
        {
            ContentTyper.OnDialogueFinished -= EndDialogueWithFade;
        }
    }

    private void Update()
    {
        TextChange();
    }

    private void CacheButtonControllers()
    {
        // 快轉/自動/紀錄按鈕皆為可選（舊版面可能沒有），null 防護避免 NPE
        if (FastForwardButton != null) fastForwardButtonCtrl = FastForwardButton.GetComponent<BottomButtonController>();
        if (AutoButton != null) autoButtonCtrl = AutoButton.GetComponent<BottomButtonController>();
        if (LogButton != null) logButtonCtrl = LogButton.GetComponent<BottomButtonController>();
    }

    /// <summary>把按鈕的狀態讀取 / 設定方法注入到 BottomButtonController（按鈕不存在則略過）。</summary>
    private void ButtonBind()
    {
        if (fastForwardButtonCtrl != null)
        {
            fastForwardButtonCtrl.getState = ContentTyper.GetCurrentlyFastForwarding;
            fastForwardButtonCtrl.setState = ContentTyper.ToFastForwardDialogue;
        }
        if (autoButtonCtrl != null)
        {
            autoButtonCtrl.getState = ContentTyper.GetCurrentlyAuto;
            autoButtonCtrl.setState = ContentTyper.ToAutoDialogue;
        }

        // TODO: LogButton 的狀態綁定（對話紀錄開關）
    }

    /// <summary>開啟對話按鈕（Inspector OnClick 綁定）。顯示對話 UI 並開始對話。</summary>
    public void Btn_Open()
    {
        if (fadeRoutine != null) return; // 淡出中不受理

        DialogueGroup.gameObject.SetActive(true);
        DialogueGroup.alpha = 1f;
        DialogueBoxPanel.SetActive(true);
        ContentTyper.ToStartDialogue();
    }

    /// <summary>
    /// 指定一份對話資訊表並立刻開始播放（章節選擇畫面跳轉劇情用的入口）。
    /// 傳入 null 時不做任何事，避免播放到上一段殘留的對話。
    /// </summary>
    public void PlayDialogue(DialogueData data)
    {
        if (data == null)
        {
            Debug.LogWarning($"{name} 的 PlayDialogue 收到空的對話資訊表 (DialogueData)，已忽略。");
            return;
        }
        if (ContentTyper == null)
        {
            Debug.LogWarning($"{name} 的 TriggerDialogue 缺少 ContentTyper 引用，無法播放對話。");
            return;
        }

        ContentTyper.SetDialogueData(data);
        Btn_Open();
    }

    /// <summary>快轉按鈕（Inspector OnClick 綁定）。整段快速播放，與自動互斥。</summary>
    public void Btn_FastForward()
    {
        if (fastForwardButtonCtrl == null) return;
        fastForwardButtonCtrl.OnButtonPressed();
        if (ContentTyper.GetCurrentlyAuto() && autoButtonCtrl != null)
        {
            autoButtonCtrl.OnButtonPressed();
        }
    }

    /// <summary>自動按鈕（Inspector OnClick 綁定）。自動與快轉互斥。</summary>
    public void Btn_Auto()
    {
        if (autoButtonCtrl == null) return;
        autoButtonCtrl.OnButtonPressed();
        if (ContentTyper.GetCurrentlyFastForwarding() && fastForwardButtonCtrl != null)
        {
            fastForwardButtonCtrl.OnButtonPressed();
        }
    }

    /// <summary>紀錄按鈕（Inspector OnClick 綁定）。切換對話紀錄面板。</summary>
    public void Btn_Log()
    {
        if (logButtonCtrl != null) logButtonCtrl.OnButtonPressed();
        if (LogUI != null) LogUI.SetActive(!LogUI.activeInHierarchy);
    }

    /// <summary>跳過按鈕（Inspector OnClick 綁定）。對話 UI 淡出並結束對話。</summary>
    public void Btn_Skip()
    {
        EndDialogueWithFade();
    }

    #region 淡出結束流程

    private void EndDialogueWithFade()
    {
        if (fadeRoutine != null) return; // 已在淡出中
        if (DialogueGroup == null || !DialogueGroup.gameObject.activeSelf) return;

        // 關閉進行中的模式（連同按鈕開關動畫一起還原）
        if (ContentTyper.GetCurrentlyFastForwarding() && fastForwardButtonCtrl != null)
        {
            fastForwardButtonCtrl.OnButtonPressed();
        }
        if (ContentTyper.GetCurrentlyAuto() && autoButtonCtrl != null)
        {
            autoButtonCtrl.OnButtonPressed();
        }
        if (LogUI != null && LogUI.activeInHierarchy)
        {
            LogUI.SetActive(false);
        }

        fadeRoutine = StartCoroutine(FadeOutAndEnd());
    }

    private IEnumerator FadeOutAndEnd()
    {
        float elapsed = 0f;
        while (elapsed < fadeOutDuration)
        {
            elapsed += Time.deltaTime;
            DialogueGroup.alpha = Mathf.Clamp01(1f - elapsed / fadeOutDuration);
            yield return null;
        }

        ContentTyper.ToEndDialogue();               // 停止打字並重置狀態
        DialogueGroup.gameObject.SetActive(false);  // 收起對話 UI
        DialogueGroup.alpha = 1f;                   // 還原透明度給下次開啟
        fadeRoutine = null;

        OnDialogueClosed?.Invoke();                 // 通知外部（例：章節選擇畫面）對話已結束
    }

    #endregion

    #region Turn To Next Page

    /// <summary>判斷滑鼠位置是否落在指定 UI 元素上。</summary>
    private bool IsPointerOverUIElement(GameObject target)
    {
        PointerEventData eventData = new PointerEventData(EventSystem.current)
        {
            position = Input.mousePosition
        };

        raycastResults.Clear();
        EventSystem.current.RaycastAll(eventData, raycastResults);

        foreach (RaycastResult result in raycastResults)
        {
            if (result.gameObject == target)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>點擊對話框時推進對話（快轉 / 自動 / 淡出 / 紀錄開啟時不接受手動點擊）。</summary>
    private void TextChange()
    {
        if (fadeRoutine != null) return;
        if (ContentTyper.GetCurrentlyFastForwarding()) return;
        if (ContentTyper.GetCurrentlyAuto()) return;
        if (LogUI != null && LogUI.activeInHierarchy) return; // 對話紀錄開啟時不推進對話

        // 先檢查點擊，再做 Raycast，避免每一幀都執行 UI Raycast
        if (Input.GetMouseButtonDown(0) && IsPointerOverUIElement(DialogueBoxPanel))
        {
            ContentTyper.ToNextDialogue();
        }
    }

    #endregion
}
