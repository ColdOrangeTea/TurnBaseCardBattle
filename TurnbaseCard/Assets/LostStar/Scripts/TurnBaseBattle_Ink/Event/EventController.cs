using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.GlobalEnums; // Effect / EffectType

/// <summary>
/// 一般事件（Event）UI 控制器（仿 TreasureChest 的重構版，由 A_Good_Ink 使用 AI 生成）。
///
/// 流程：玩家走到「Event」事件格 → <see cref="MapEventService"/> 觸發
/// <see cref="MapEventService.GenericEventRequested"/> → 本元件讀取該格 <see cref="NodeEvent.eventData"/>
/// 指定的 <see cref="SO_Event"/>，開啟 EventEmpty UI，顯示標題／提問／三個選項；玩家點選後套用該選項的
/// 效果、顯示結果文字，按關閉結束。
///
/// 與舊 EventUIManager 差異：不再依賴 V2 重構已移除的 SO_EventManager / PlayerStatsManager。
/// 事件內容改由「每個 Event 節點自己持有的 SO_Event」提供（與節點連線/敵人同為「節點自帶資料」的作法）；
/// 選項效果改用事件 <see cref="EffectsChosen"/> 對外拋出，日後由玩家數值/背包系統訂閱實際套用，本元件本身不綁定數值系統。
/// </summary>
public class EventController : MonoBehaviour
{
    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private MapEventService mapEventService;
    [Tooltip("開事件時暫停地圖點擊、關閉後恢復；可留空。")]
    [SerializeField] private S001_PlayerController playerController;

    [Header("事件 UI")]
    [Tooltip("事件面板根（開/關）；對應 EventEmpty prefab 的根物件")]
    [SerializeField] private GameObject eventPanelRoot;
    [Tooltip("提問區（標題／提問／選項）的容器")]
    [SerializeField] private GameObject questionUI;
    [Tooltip("結果區（顯示選擇後的結果文字）的容器")]
    [SerializeField] private GameObject resultUI;

    [Header("提問區元件")]
    [SerializeField] private TMP_Text titleText;      // 事件標題
    [SerializeField] private TMP_Text questionText;   // 事件提問
    [SerializeField] private Button optionAButton;    // 選項 A
    [SerializeField] private Button optionBButton;    // 選項 B
    [SerializeField] private Button optionCButton;    // 選項 C

    [Header("結果區元件")]
    [SerializeField] private TMP_Text resultText;     // 結果文字
    [SerializeField] private Button closeButton;      // 關閉

    // 目前正在顯示的事件資料（執行期用；非序列化）
    private SO_Event currentEvent;

    // ── 對外事件（日後玩家數值/背包系統訂閱即可實際套用；本元件只顯示與拋出）──
    /// <summary>玩家選了某選項、該選項的效果清單（AddGold/ReduceHealth… 等）。</summary>
    public event Action<IReadOnlyList<Effect>> EffectsChosen;
    /// <summary>事件面板關閉。</summary>
    public event Action EventClosed;

    private void Awake()
    {
        if (mapEventService == null) mapEventService = FindAnyObjectByType<MapEventService>();
        if (playerController == null) playerController = FindAnyObjectByType<S001_PlayerController>();
        if (eventPanelRoot != null) eventPanelRoot.SetActive(false);
        if (closeButton != null) closeButton.onClick.AddListener(Close);
    }

    private void OnEnable()
    {
        if (mapEventService != null) mapEventService.GenericEventRequested += OnGenericEventRequested;
    }

    private void OnDisable()
    {
        if (mapEventService != null) mapEventService.GenericEventRequested -= OnGenericEventRequested;
    }

    private void OnGenericEventRequested(NodeEvent grid)
    {
        if (grid == null) return;
        if (grid.eventData == null)
        {
            Debug.LogWarning($"[EventController] 事件節點「{grid.name}」未指定 SO_Event（eventData），無法顯示事件內容。");
            return;
        }
        Open(grid.eventData);
    }

    /// <summary>開啟事件：暫停地圖點擊、以指定的 <see cref="SO_Event"/> 填入提問與選項。</summary>
    public void Open(SO_Event data)
    {
        if (data == null) return;
        currentEvent = data;

        if (eventPanelRoot != null) eventPanelRoot.SetActive(true);
        if (questionUI != null) questionUI.SetActive(true);
        if (resultUI != null) resultUI.SetActive(false);
        if (playerController != null) playerController.DisablePlayerInputForCheck(); // 看事件時先別讓玩家點格子

        if (titleText != null) titleText.text = data.Title;
        if (questionText != null) questionText.text = data.question;

        SetupOption(optionAButton, data.optionA, data.effectsA, data.resultA);
        SetupOption(optionBButton, data.optionB, data.effectsB, data.resultB);
        SetupOption(optionCButton, data.optionC, data.effectsC, data.resultC);

        BattleLog.Log($"[EventController] 開啟事件：{data.Title}");
    }

    /// <summary>設定單一選項按鈕的文字與點擊行為；選項文字為空則隱藏該按鈕。</summary>
    private void SetupOption(Button button, string optionLabel, List<Effect> effects, string resultLabel)
    {
        if (button == null) return;

        bool hasOption = !string.IsNullOrWhiteSpace(optionLabel);
        button.gameObject.SetActive(hasOption);
        if (!hasOption) return;

        var label = button.GetComponentInChildren<TMP_Text>();
        if (label != null) label.text = optionLabel;

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => ChooseOption(effects, resultLabel));
    }

    /// <summary>玩家點了某選項：拋出效果（供外部套用）、切到結果區顯示結果文字。</summary>
    private void ChooseOption(List<Effect> effects, string resultLabel)
    {
        if (effects != null && effects.Count > 0)
            EffectsChosen?.Invoke(effects);

        if (questionUI != null) questionUI.SetActive(false);
        if (resultUI != null) resultUI.SetActive(true);
        if (resultText != null) resultText.text = resultLabel;

        BattleLog.Log($"[EventController] 選擇結果：{resultLabel}");
    }

    /// <summary>關閉事件 UI、恢復地圖點擊。</summary>
    public void Close()
    {
        if (eventPanelRoot != null) eventPanelRoot.SetActive(false);
        if (playerController != null) playerController.EnablePlayerInput();
        currentEvent = null;
        EventClosed?.Invoke();
        BattleLog.Log("[EventController] 關閉事件");
    }
}
