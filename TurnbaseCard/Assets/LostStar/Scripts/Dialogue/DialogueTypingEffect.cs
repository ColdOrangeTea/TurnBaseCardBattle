using System;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using Assets.Scripts.Dialogue;

/// <summary>
/// 對話內容的打字機效果：由 DialogueData（對話資訊表）讀取對白，逐字顯示文字。
/// 支援 TMP 富文本標籤、|pause| / |pause=秒數| 停頓標記、快轉 / 自動模式，
/// 並依行資料指定的立繪與名稱顏色（來自人物風格資訊表或自定義）更新畫面。
/// 對白全部播畢後玩家再點擊時，發出 OnDialogueFinished 事件（由 TriggerDialogue 接手淡出）。
/// </summary>
public class DialogueTypingEffect : MonoBehaviour
{
    [Header("對話資料")]
    [SerializeField] private DialogueData dialogueData; // 對話資訊表（ScriptableObject）

    [Header("對話框的組件")]
    public TMP_Text charaNameBox;                       // 顯示角色名稱的框
    public TMP_Text dialogueContentBox;                 // 顯示對話內容的框
    [SerializeField]
    private CharacterPortraitController portraitController; // 人物立繪控制器（可為空）
    [SerializeField]
    private DialogueBackgroundController backgroundController; // 對話背景控制器（可為空）
    [SerializeField]
    private DialogueLogController logController;        // 對話紀錄控制器（可為空）
    public string charaNameText;                        // 當前行的角色名稱（除錯顯示用）
    public DialogueUnitType charaUnitType;              // 當前行的說話者
    public Sprite charaPortrait;                        // 當前行的人物立繪
    public Color charaNameColor = Color.white;          // 當前行的人物名稱顯示顏色
    public string dialogueText;                         // 當前行的對白文本
    public int curVisibleCharCount = 0;                 // 當前可見字元數
    public int MaxLineCount = 0;                        // 對白總行數
    public int currentLineIndex = 0;                    // 當前正在顯示的對白索引

    [Header("對話特效設定")]
    public float specialCharDelay = 0.45f;              // |pause| 的預設停頓秒數（可被行資料或 |pause=秒數| 覆寫）
    public float charactersPerSecond = 50;              // 每秒顯示字元數

    [Header("快轉 / 自動功能設定")]
    public float pagePerSecond = 1;                     // 自動模式下的翻頁間隔（秒）
    [SerializeField] private bool CurrentlyAuto;           // 自動播放中
    [SerializeField] private bool CurrentlyCompletingLine; // 點擊後快速完成「當前句」中
    [SerializeField] private bool CurrentlyFastForwarding; // 快轉模式中（整段快速播放）
    [SerializeField][Min(1)] private float fastSpeedup = 5; // 完成當前句的加速倍率

    [Header("完整的顯示文字")]
    [SerializeField] private string cleanText;          // 去除標籤後的純文字（除錯用）

    /// <summary>對白播畢後玩家再點擊時觸發（TriggerDialogue 訂閱後執行淡出結束）。</summary>
    public event Action OnDialogueFinished;

    // 停頓標記語法：|pause| 或 |pause=1.5|
    private static readonly Regex PauseRegex =
        new Regex(@"\|pause(=(?<sec>\d+(\.\d+)?))?\|", RegexOptions.Compiled);

    // HTML 標籤的起訖字元
    private const char HTMLTagOpen = '<';
    private const char HTMLTagClose = '>';

    private bool readyForNewText = true;                // 是否可接收下一句
    private bool isFoundSPString = false;               // 是否碰到停頓標記
    private float currentPauseDuration;                 // 當前行 |pause| 的預設秒數

    // 對話框原本的字型（DialogueData 未指定字型時還原用）
    private TMP_FontAsset defaultNameBoxFont;
    private TMP_FontAsset defaultContentBoxFont;

    private IEnumerator typingProcess;                  // 打字效果協程
    private IEnumerator nextPage;                       // 翻頁協程

    // 快取的延遲物件（避免每字元 new WaitForSeconds 產生 GC）
    private WaitForSeconds WFS_currentPause;
    private WaitForSeconds WFS_simpleDelay;
    private WaitForSeconds WFS_completeLine;
    private WaitForSeconds WFS_fastForwardMode;

    #region 狀態存取（TriggerDialogue 以 Func / Action 綁定，勿改名）

    public void SetCurrentlyAuto(bool isAuto) => CurrentlyAuto = isAuto;
    public bool GetCurrentlyAuto() => CurrentlyAuto;
    public void SetCurrentlyCompletingLine(bool isCompleting) => CurrentlyCompletingLine = isCompleting;
    public bool GetCurrentlyCompletingLine() => CurrentlyCompletingLine;
    public void SetCurrentlyFastForwarding(bool isFastForwarding) => CurrentlyFastForwarding = isFastForwarding;
    public bool GetCurrentlyFastForwarding() => CurrentlyFastForwarding;
    public void SetReadyForNewText(bool isReady) => readyForNewText = isReady;
    public bool GetReadyForNewText() => readyForNewText;

    private void SetFoundSpString(bool isFound) => isFoundSPString = isFound;
    private bool GetFoundSpString() => isFoundSPString;

    #endregion

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        // 記下對話框原本的字型，供未指定字型的對話還原使用
        if (charaNameBox != null) defaultNameBoxFont = charaNameBox.font;
        if (dialogueContentBox != null) defaultContentBoxFont = dialogueContentBox.font;

        currentPauseDuration = specialCharDelay;
        WFS_currentPause = new WaitForSeconds(specialCharDelay);
        WFS_simpleDelay = new WaitForSeconds(1 / charactersPerSecond);
        WFS_completeLine = new WaitForSeconds(1 / (charactersPerSecond * fastSpeedup));
        WFS_fastForwardMode = new WaitForSeconds(0);
    }

    #region 外部觸發入口

    /// <summary>更換對話資訊表（例如切換到另一段劇情對話）。</summary>
    public void SetDialogueData(DialogueData data)
    {
        dialogueData = data;
    }

    /// <summary>快轉模式開關（由 BottomButtonController 的 setState 觸發）。</summary>
    public void ToFastForwardDialogue(bool status)
    {
        SetCurrentlyFastForwarding(status);
        // 開啟快轉時若目前閒置（等待點擊中），立刻啟動推進，讓快轉鏈開始跑
        if (status && GetReadyForNewText())
        {
            ToNextDialogue();
        }
    }

    /// <summary>自動模式開關（由 BottomButtonController 的 setState 觸發）。</summary>
    public void ToAutoDialogue(bool status)
    {
        SetCurrentlyAuto(status);
        // 開啟 auto 時若目前閒置，立刻推進下一句（關閉時不觸發，避免誤翻頁）
        if (status && GetReadyForNewText())
        {
            ToNextDialogue();
        }
    }

    /// <summary>從對話資訊表的第一行開始對話。</summary>
    public void ToStartDialogue()
    {
        if (dialogueData == null || dialogueData.LineCount == 0)
        {
            Debug.LogWarning("尚未設定對話資訊表 (DialogueData)，無法開始對話");
            return;
        }

        currentLineIndex = 0;
        MaxLineCount = dialogueData.LineCount;
        curVisibleCharCount = 0;
        SetReadyForNewText(true);
        if (logController != null) logController.Clear(); // 新對話開始，清空紀錄
        // 套用此段對話的背景圖（未設定則自動隱藏背景）
        if (backgroundController != null) backgroundController.SetBackground(dialogueData.backgroundImage);
        // 套用此段對話指定的字型（未指定則還原對話框原本的字型）
        ApplyDialogueFont();
        TriggerTyping();
    }

    /// <summary>推進對話：閒置時翻下一頁；打字中則快速完成當前句。</summary>
    public void ToNextDialogue()
    {
        if (GetReadyForNewText())
        {
            if (nextPage != null)
            {
                StopCoroutine(nextPage);
            }
            nextPage = ToNextPage();
            StartCoroutine(nextPage);
        }
        else if (!GetCurrentlyCompletingLine()) // 打字中 → 快速完成當前句
        {
            SetCurrentlyCompletingLine(true);
        }
    }

    /// <summary>
    /// 立即結束對話：停止所有協程並重置狀態（由 TriggerDialogue 的跳過淡出流程呼叫）。
    /// </summary>
    public void ToEndDialogue()
    {
        if (typingProcess != null) StopCoroutine(typingProcess);
        if (nextPage != null) StopCoroutine(nextPage);
        SetCurrentlyAuto(false);
        ResetDialogueTypingSetting();
    }

    #endregion

    private IEnumerator ToNextPage()
    {
        if (currentLineIndex == MaxLineCount)
        {
            // 自動 / 快轉模式在最後一頁時直接停止
            if (GetCurrentlyAuto() || GetCurrentlyFastForwarding())
            {
                yield break;
            }
            // 玩家在對白播畢後再點擊 → 通知外部結束對話（淡出）
            OnDialogueFinished?.Invoke();
            yield break;
        }
        TriggerTyping();
        yield return new WaitForSeconds(0.1f);
    }

    #region 行資料套用與重置

    /// <summary>從對話資訊表讀取當前行的文本、說話者、立繪、名稱顏色與停頓設定。</summary>
    private void ApplyCurrentLine()
    {
        var line = dialogueData != null ? dialogueData.GetLine(currentLineIndex) : null;
        if (line == null) return;

        dialogueText = line.text;
        charaNameText = line.DisplayName;
        charaUnitType = line.Speaker;
        charaPortrait = line.Portrait;
        charaNameColor = line.NameColor;
        // 行資料的 pauseDuration > 0 時覆寫全域預設
        currentPauseDuration = line.pauseDuration > 0f ? line.pauseDuration : specialCharDelay;
        WFS_currentPause = new WaitForSeconds(currentPauseDuration);

        UpdatePortrait();

        // 寫入對話紀錄（名稱 + 去除停頓標記的對白，保留富文本顏色等標籤）
        if (logController != null)
        {
            logController.AddEntry(charaNameText, RemoveSpecialString(dialogueText), charaNameColor);
        }
    }

    /// <summary>
    /// 套用對話資訊表指定的字型到名稱框、內容框與對話紀錄；
    /// 未指定（null）時還原對話框原本的字型。
    /// </summary>
    private void ApplyDialogueFont()
    {
        TMP_FontAsset font = dialogueData != null ? dialogueData.dialogueFont : null;

        if (charaNameBox != null)
        {
            var target = font != null ? font : defaultNameBoxFont;
            if (target != null) charaNameBox.font = target;
        }
        if (dialogueContentBox != null)
        {
            var target = font != null ? font : defaultContentBoxFont;
            if (target != null) dialogueContentBox.font = target;
        }
        if (logController != null) logController.SetEntryFont(font); // null = 用紀錄 Prefab 原字型
    }

    /// <summary>依當前行指定的立繪更新人物立繪；旁白或空文本時隱藏立繪。</summary>
    private void UpdatePortrait()
    {
        if (portraitController == null) return;

        if (string.IsNullOrEmpty(dialogueText) || charaUnitType == DialogueUnitType.Narration)
        {
            portraitController.Hide();
        }
        else
        {
            portraitController.SetPortrait(charaPortrait);
            // 立繪位置一律還原 Prefab 預設位置（自訂立繪位置功能已移除）
            portraitController.ResetPosition();
        }
    }

    private void ResetDialogueTypingSetting()
    {
        dialogueText = null;
        charaNameText = null;
        charaUnitType = DialogueUnitType.Unknown;
        charaPortrait = null;
        charaNameColor = Color.white;
        curVisibleCharCount = 0;
        currentLineIndex = 0;
        MaxLineCount = 0;

        charaNameBox.text = null;
        dialogueContentBox.text = null;

        SetReadyForNewText(true);
        SetFoundSpString(false);
        SetCurrentlyCompletingLine(false);
        SetCurrentlyFastForwarding(false);
        if (portraitController != null) portraitController.Hide();
        if (backgroundController != null) backgroundController.Hide();
    }

    #endregion

    private void TriggerTyping()
    {
        if (MaxLineCount == 0)
        {
            Debug.LogWarning("沒有對白文本");
            return;
        }

        if (currentLineIndex < MaxLineCount)
        {
            if (!GetReadyForNewText()) return;

            ApplyCurrentLine();

            SetCurrentlyCompletingLine(false);
            SetReadyForNewText(false);

            // 如果有正在執行的打字效果，則停止
            if (typingProcess != null)
            {
                StopCoroutine(typingProcess);
            }
            typingProcess = TextTyping();
            StartCoroutine(typingProcess);
        }
        else
        {
            Debug.LogWarning("對白最後一頁");
        }
    }

    #region Typewriter Effect 打字效果部分

    private WaitForSeconds GetCurrentTypingDelay()
    {
        if (GetCurrentlyFastForwarding()) return WFS_fastForwardMode; // 快轉模式：不等待
        if (GetCurrentlyCompletingLine()) return WFS_completeLine;    // 快速完成當前句（停頓也退化）
        if (GetFoundSpString()) return WFS_currentPause;
        return WFS_simpleDelay;
    }

    private void ShowCharacterName()
    {
        charaNameBox.text = charaNameText;
        charaNameBox.color = charaNameColor; // 套用人物主題色（或自定義顏色）
    }

    /// <summary>去除文本中所有停頓標記（|pause| 與 |pause=秒數|）。</summary>
    private string RemoveSpecialString(string text)
    {
        return PauseRegex.Replace(text, "");
    }

    /// <summary>去除文本中所有 HTML 標籤。</summary>
    private string RemoveHTMLTags(string text)
    {
        int startTag = text.IndexOf(HTMLTagOpen);
        while (startTag != -1)
        {
            int endTag = text.IndexOf(HTMLTagClose, startTag);
            if (endTag == -1) break;
            text = text.Remove(startTag, endTag - startTag + 1);
            startTag = text.IndexOf(HTMLTagOpen);
        }
        return text;
    }

    /// <summary>判斷 index 位置是否正好是一個停頓標記的起點。</summary>
    private bool IsPauseTagAt(string text, int index)
    {
        if (index >= text.Length || text[index] != '|') return false;
        var match = PauseRegex.Match(text, index);
        return match.Success && match.Index == index;
    }

    /// <summary>
    /// 若當前索引處為停頓標記則移除之，設定停頓秒數並標記觸發停頓。
    /// |pause=秒數| 使用指定秒數；|pause| 使用當前行的預設秒數。
    /// </summary>
    private string TryConsumePauseTag(int curIndex, string currentText)
    {
        if (curIndex >= currentText.Length || currentText[curIndex] != '|') return currentText;

        var match = PauseRegex.Match(currentText, curIndex);
        if (!match.Success || match.Index != curIndex) return currentText;

        currentText = currentText.Remove(curIndex, match.Length);

        float seconds = currentPauseDuration;
        if (match.Groups["sec"].Success)
        {
            seconds = float.Parse(match.Groups["sec"].Value, CultureInfo.InvariantCulture);
        }
        WFS_currentPause = new WaitForSeconds(seconds);
        SetFoundSpString(true);

        return currentText;
    }

    /// <summary>若當前索引處為 HTML 標籤（含連續標籤），回傳應跳過的字元數。</summary>
    private int AddHTMLLengthToCurIndex(int curIndex, string currentText)
    {
        int tagCharCount = 0;
        bool insideTag = curIndex < currentText.Length && currentText[curIndex] == HTMLTagOpen;

        if (insideTag)
        {
            tagCharCount = 1;
        }

        while (insideTag && curIndex < currentText.Length)
        {
            if (currentText[curIndex] == HTMLTagClose)
            {
                insideTag = false;
                // 如果有連續的 HTML 標籤也一起跳過
                if (curIndex + 1 < currentText.Length && currentText[curIndex + 1] == HTMLTagOpen)
                {
                    insideTag = true;
                    curIndex++;
                    tagCharCount++;
                }
            }
            else
            {
                curIndex++;
                tagCharCount++;
            }
        }
        return tagCharCount;
    }

    private IEnumerator TextTyping()
    {
        dialogueContentBox.text = dialogueText;
        ShowCharacterName();

        // 生成剔除停頓標記與 HTML 標籤的純文本，用以計算實際可見字元數
        cleanText = RemoveHTMLTags(RemoveSpecialString(dialogueContentBox.text));
        int visibleCharCount = cleanText.Length;
        int curCharIndex = 0;

        dialogueContentBox.maxVisibleCharacters = 0;

        while (curCharIndex <= dialogueContentBox.text.Length)
        {
            // 判斷一句話打字結束
            if (curCharIndex >= visibleCharCount && dialogueContentBox.maxVisibleCharacters >= visibleCharCount)
            {
                SetReadyForNewText(true);
                SetCurrentlyCompletingLine(false);
                if (currentLineIndex < MaxLineCount)
                {
                    ++currentLineIndex;
                }
                if (GetCurrentlyFastForwarding())
                {
                    ToNextDialogue();
                }
                if (GetCurrentlyAuto())
                {
                    yield return new WaitForSeconds(pagePerSecond);
                    ToNextDialogue();
                }
                break;
            }

            curCharIndex += AddHTMLLengthToCurIndex(curCharIndex, dialogueContentBox.text);
            dialogueContentBox.text = TryConsumePauseTag(curCharIndex, dialogueContentBox.text);

            yield return GetCurrentTypingDelay();

            if (GetFoundSpString())
            {
                SetFoundSpString(false);
            }

            // 顯示下一個字元（若停在尚未消耗的停頓標記起點，留到下一輪處理）
            if (curCharIndex < dialogueContentBox.text.Length &&
                !IsPauseTagAt(dialogueContentBox.text, curCharIndex))
            {
                curVisibleCharCount = dialogueContentBox.maxVisibleCharacters;
                dialogueContentBox.maxVisibleCharacters++;
                curCharIndex++;
            }
        }
    }

    #endregion
}
