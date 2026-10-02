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

    [Header("劇情觸發用（地圖/戰鬥中由程式呼叫播放時）")]
    [Tooltip("開場先收起對話 UI（由程式觸發播放的場景勾選；章節選擇等原本就由外部開關的可不勾）。")]
    [SerializeField]
    private bool hideOnStart = false;
    [Tooltip("跳過按鈕在對話 UI 之外時（如 OLD_DialogueEmpty），勾選後跳過鈕隨對話一起顯示/隱藏。")]
    [SerializeField]
    private bool toggleSkipButtonWithDialogue = false;

    // 快取按鈕控制器，避免每次點擊都 GetComponent
    private BottomButtonController fastForwardButtonCtrl;
    private BottomButtonController autoButtonCtrl;
    private BottomButtonController logButtonCtrl;

    private Coroutine fadeRoutine;              // 淡出協程（非 null 表示正在淡出）

    /// <summary>對話 UI 淡出結束、完全收起後觸發（章節選擇畫面訂閱後可自動返回選單）。</summary>
    public event Action OnDialogueClosed;

    // Raycast 用的快取，避免每次點擊都配置新物件
    private readonly List<RaycastResult> raycastResults = new List<RaycastResult>();

    /// <summary>對話是否正在顯示中（開啟後、淡出收起前）。</summary>
    public bool IsPlaying { get; private set; }

    private void Start()
    {
        CacheButtonControllers();
        ButtonBind();
        ContentTyper.OnDialogueFinished += OnContentFinished; // 對白播畢再點擊 → 接續下一段或淡出結束

        // 若 Start 前已有人呼叫 PlayDialogue（IsPlaying=true），不要把它收起來
        if (hideOnStart && !IsPlaying)
        {
            if (DialogueGroup != null) DialogueGroup.gameObject.SetActive(false);
            SetSkipButtonVisible(false);
        }
    }

    private void SetSkipButtonVisible(bool visible)
    {
        if (toggleSkipButtonWithDialogue && SkipButton != null) SkipButton.SetActive(visible);
    }

    private void OnDestroy()
    {
        if (ContentTyper != null)
        {
            ContentTyper.OnDialogueFinished -= OnContentFinished;
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

        IsPlaying = true;
        DialogueGroup.gameObject.SetActive(true);
        DialogueGroup.alpha = 1f;
        DialogueBoxPanel.SetActive(true);
        SetSkipButtonVisible(true);
        ContentTyper.ToStartDialogue();
    }

    /// <summary>
    /// 播放一段對話並等它結束（淡出收起）才返回，供劇情/教學流程以 yield return 串接。
    /// 若上一段還在淡出，會先等它收完再開始；data 為 null 直接略過。
    /// </summary>
    public IEnumerator PlayDialogueRoutine(DialogueData data)
    {
        if (data == null || data.LineCount == 0) yield break;
        while (fadeRoutine != null) yield return null; // 等上一段淡出完

        PlayDialogue(data);
        while (IsPlaying) yield return null;
    }

    // 連播佇列：目前這段播完（玩家再點一下）時，不收起 UI 直接接著播的後續段落
    private readonly Queue<DialogueData> continuation = new Queue<DialogueData>();

    /// <summary>
    /// 依序播放多段對話並等全部結束才返回。
    ///   keepUIOpenBetween = true ：段與段之間不收起 UI，一段播完直接接下一段，像一段長對話（跳過＝跳過剩下全部）；
    ///   keepUIOpenBetween = false：每段播完先淡出收起 UI，再開下一段（跳過＝只跳過目前這段）。
    /// null 或空的段落會自動略過。
    /// </summary>
    public IEnumerator PlayDialogueSequenceRoutine(IList<DialogueData> list, bool keepUIOpenBetween)
    {
        if (list == null) yield break;

        if (!keepUIOpenBetween)
        {
            foreach (DialogueData d in list) yield return PlayDialogueRoutine(d);
            yield break;
        }

        var valid = new List<DialogueData>();
        foreach (DialogueData d in list) if (d != null && d.LineCount > 0) valid.Add(d);
        if (valid.Count == 0) yield break;

        while (fadeRoutine != null) yield return null; // 等上一段淡出完

        continuation.Clear();
        for (int i = 1; i < valid.Count; i++) continuation.Enqueue(valid[i]);

        PlayDialogue(valid[0]);
        while (IsPlaying) yield return null;
        continuation.Clear(); // 保險：被跳過時清掉殘留
    }

    /// <summary>目前這段對白播畢後玩家再點擊：有連播段落就直接接下一段，否則淡出結束。</summary>
    private void OnContentFinished()
    {
        while (continuation.Count > 0)
        {
            DialogueData next = continuation.Dequeue();
            if (next == null || next.LineCount == 0) continue;
            ContentTyper.ContinueWithDialogue(next);
            return;
        }
        EndDialogueWithFade();
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
        continuation.Clear(); // 連播中跳過＝跳過剩下全部
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
        SetSkipButtonVisible(false);
        fadeRoutine = null;
        IsPlaying = false;

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
