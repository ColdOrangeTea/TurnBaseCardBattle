using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// L1 Boss 劇情演出（由 A_Good_Ink 使用 AI 生成；改寫自 LS2 舊版 JokerSecret）。
///
/// 流程：
///   1. 第一次進入 Boss Stage（<see cref="bossStage"/>）→ 播 Boss 登場對話（例：Text_014 L1-BOSS_Main_1，奧蘭娜）。
///   2. 打倒 L1 Boss（敵人型別＝<see cref="bossEnemyType"/>）並按結算確定回到地圖後 →
///      播 Boss 戰後對話（例：Text_015 L1-BOSS_Main_2）；播到第 <see cref="shakeStartLine"/> 句（地面開始震動）起鏡頭持續震動。
///   3. 接著 Joker 演出：Joker 物件出現＋上下黑邊滑入 → 畫面淡成白（超新星爆炸音效）→ 再淡成黑、停止震動、
///      切 Joker BGM → 稍候播 Joker 對話（例：Text_016 L1-Joker_Main_1）。
///   4. Joker 對話結束 → 收起 Joker、還原 BGM、（可選）黑幕淡出 → 觸發 <see cref="onSequenceFinished"/>
///      （接大關卡結算/結束畫面等，舊版在這裡開 GameOver 結果面板）。
///
/// 與舊版差異：改用新對話系統（<see cref="TriggerDialogue"/> + DialogueData）與地圖流程掛點
/// （<see cref="MapFlowHookBase"/>：戰後回地圖、任務領獎面板之前播放），移除已刪的 DialogueOpenClose /
/// GameManager.DialogueManagerInstance 依賴；聖女立繪淡入淡出交給對話系統的逐句立繪（風格表/Spine 表情）處理。
/// 音訊走 <see cref="AudioDirector"/>（沒有時退回本物件上的 AudioSource）。
/// 黑邊/淡出圖未指定時，會在執行期自動建一個全螢幕演出用 Canvas（蓋過地圖與戰鬥、低於對話 UI）。
/// </summary>
[DefaultExecutionOrder(100)] // 晚於 CameraController.LateUpdate，震動偏移才不會被相機跟隨蓋掉
public class JokerSecret : MapFlowHookBase
{
    [Header("接線（留空會在場上自動尋找）")]
    [SerializeField] private TriggerDialogue dialogue;
    [SerializeField] private LevelMapManager levelMap;
    [SerializeField] private S001_PlayerController player;
    [Tooltip("要震動的相機；留空用 Camera.main")]
    [SerializeField] private Camera mainCamera;

    [Header("1. Boss 登場（第一次進入 Boss Stage）")]
    [Tooltip("L1 的 Boss Stage（場景中的 StageInfo）")]
    [SerializeField] private StageInfo bossStage;
    [SerializeField] private LevelMapTutorialDirector.DialogueSequence bossIntroDialogue = new LevelMapTutorialDirector.DialogueSequence();
    [Tooltip("進入 Boss Stage 後等多久再開始對話")]
    [SerializeField][Min(0f)] private float bossIntroDelay = 0.6f;

    [Header("2. 打倒 Boss 後")]
    [Tooltip("L1 Boss 的敵人型別（被擊敗的地圖敵人型別相符才算打倒 Boss）")]
    [SerializeField] private Assets.Scripts.GlobalEnums.BattleEnum.EnemyType bossEnemyType = Assets.Scripts.GlobalEnums.BattleEnum.EnemyType.Godness;
    [SerializeField] private LevelMapTutorialDirector.DialogueSequence bossDefeatedDialogue = new LevelMapTutorialDirector.DialogueSequence();
    [Tooltip("戰後對話播到第幾句（從 1 起算）開始震動鏡頭；0 = 不震動。以戰後對話的第一段為準。")]
    [SerializeField][Min(0)] private int shakeStartLine = 5;

    [Header("3. Joker 演出")]
    [Tooltip("Joker 動畫物件（可空）：演出開始時顯示、結束時隱藏")]
    [SerializeField] private GameObject joker;
    [Tooltip("Joker 對話期間顯示的背景（可空）")]
    [SerializeField] private GameObject jokerBlackBG;
    [SerializeField] private LevelMapTutorialDirector.DialogueSequence jokerDialogue = new LevelMapTutorialDirector.DialogueSequence();

    [Header("演出 UI（留空＝執行期自動建立）")]
    [SerializeField] private Image topBlackBar;     // 上方黑邊
    [SerializeField] private Image bottomBlackBar;  // 下方黑邊
    [SerializeField] private Image fadeImage;       // 全螢幕淡白/淡黑用
    [Tooltip("自動建立演出 Canvas 的排序（需高於戰鬥 100、低於對話 UI 250，對話才能顯示在黑幕上）")]
    [SerializeField] private int overlaySortingOrder = 245;

    [Header("黑邊")]
    [SerializeField][Min(0.01f)] private float blackBarSpeed = 1.0f;   // 黑邊滑入秒數
    [SerializeField][Range(0f, 0.5f)] private float blackBarHeight = 0.05f; // 黑邊佔畫面高度比例

    [Header("鏡頭震動")]
    [SerializeField][Min(0f)] private float shakeIntensity = 0.3f;     // 震動幅度（世界單位）

    [Header("淡出")]
    [SerializeField][Min(0f)] private float waitBeforeFade = 2f;       // 黑邊滑入後到開始淡白的等待
    [SerializeField][Min(0.01f)] private float fadeDuration = 2.0f;    // 淡成白的秒數
    [SerializeField][Min(0f)] private float waitAfterFade = 1f;        // 全白停留
    [SerializeField][Min(0.01f)] private float blackFadeDuration = 1.0f; // 白→黑的秒數
    [SerializeField][Min(0f)] private float jokerDialogueDelay = 3f;   // 全黑後到 Joker 對話的等待
    [Tooltip("Joker 對話結束後把黑幕淡出回到畫面；不勾則保持全黑（交給結算畫面接手）")]
    [SerializeField] private bool fadeBackAfterSequence = true;

    [Header("音訊（走 AudioDirector）")]
    [SerializeField] private AudioClip jokerBgm;           // Joker 對話 BGM（BGM_JokerSecret）
    [SerializeField] private AudioClip shockSfx;           // 震動開始（地震）
    [SerializeField] private AudioClip spaceExplosionSfx;  // 淡白時（超新星爆炸）
    [SerializeField] private AudioClip spaceShipSfx;       // 淡白時（逃回母艦）

    [Header("演出結束")]
    [Tooltip("整段 Boss 戰後演出（含 Joker 對話）結束時觸發：可接大關卡結算/結束畫面")]
    public UnityEvent onSequenceFinished = new UnityEvent();
    public event Action SequenceFinished;

    private DialogueTypingEffect typer;
    private bool introPlayed;
    private bool bossDefeatedPending; // 本場戰鬥打倒了 Boss，等回到地圖播戰後演出
    private bool sequencePlayed;
    private bool bgmPushed;

    // 震動：LateUpdate 套偏移、下一影格 Update 先扣回，不污染 CameraController 的跟隨
    private bool shaking;
    private Vector3 appliedShakeOffset;

    private void Awake()
    {
        if (dialogue == null) dialogue = FindAnyObjectByType<TriggerDialogue>(FindObjectsInactive.Include);
        if (levelMap == null) levelMap = FindAnyObjectByType<LevelMapManager>();
        if (player == null) player = FindAnyObjectByType<S001_PlayerController>();
        typer = FindAnyObjectByType<DialogueTypingEffect>(FindObjectsInactive.Include);
        if (dialogue == null) Debug.LogWarning($"[{name}] JokerSecret 找不到 TriggerDialogue（對話 UI），Boss 劇情對話將不會播放。");

        if (joker != null) joker.SetActive(false);
        if (jokerBlackBG != null) jokerBlackBG.SetActive(false);
    }

    private void OnEnable()
    {
        if (levelMap != null) levelMap.StageEntered += OnStageEntered;
        MapTurnBaseManager.EnemyDefeated += OnEnemyDefeated;
    }

    private void OnDisable()
    {
        if (levelMap != null) levelMap.StageEntered -= OnStageEntered;
        MapTurnBaseManager.EnemyDefeated -= OnEnemyDefeated;
        StopShake();
    }

    private void Start()
    {
        if (mainCamera == null) mainCamera = Camera.main;
        EnsureOverlay();
        SetBlackBarsActive(false);
        if (fadeImage != null) fadeImage.color = new Color(1f, 1f, 1f, 0f);

        // 起始就在 Boss Stage（直接測 Boss 關）時補判
        if (levelMap != null && levelMap.CurrentStage != null) OnStageEntered(levelMap.CurrentStage);
    }

    // ────────────────────────────────────────────────────────────────
    // 觸發
    // ────────────────────────────────────────────────────────────────

    private void OnStageEntered(StageInfo stage)
    {
        if (introPlayed || bossStage == null || stage != bossStage) return;
        introPlayed = true;
        StartCoroutine(PlayIntro());
    }

    private IEnumerator PlayIntro()
    {
        if (player != null) player.PushInputHold();
        if (bossIntroDelay > 0f) yield return new WaitForSeconds(bossIntroDelay);
        yield return PlaySequence(bossIntroDialogue);
        if (player != null) player.PopInputHold();
    }

    private void OnEnemyDefeated(Enemy enemy)
    {
        if (sequencePlayed || enemy == null || enemy.enemyType != bossEnemyType) return;
        bossDefeatedPending = true;
    }

    /// <summary>戰鬥結算確定、回到地圖後（MapFlowController 會 yield 等這裡全部播完才還給玩家）。</summary>
    public override IEnumerator OnAfterBattleReturned(bool playerWin)
    {
        if (!playerWin || !bossDefeatedPending || sequencePlayed) yield break;
        bossDefeatedPending = false;
        sequencePlayed = true;
        yield return PlayBossDefeatedSequence();
    }

    /// <summary>手動觸發整段 Boss 戰後演出（測試或其他流程用）。</summary>
    [ContextMenu("播放 Boss 戰後演出（測試）")]
    public void StartJokerAni()
    {
        sequencePlayed = true;
        StartCoroutine(PlayBossDefeatedSequence());
    }

    // ────────────────────────────────────────────────────────────────
    // Boss 戰後演出
    // ────────────────────────────────────────────────────────────────

    private IEnumerator PlayBossDefeatedSequence()
    {
        if (player != null) player.PushInputHold();

        // 2. 戰後對話；播到指定句開始震動
        Coroutine watcher = shakeStartLine > 0 ? StartCoroutine(StartShakeAtLine()) : null;
        yield return PlaySequence(bossDefeatedDialogue);
        if (watcher != null) StopCoroutine(watcher);

        // 3. Joker 演出
        if (joker != null) joker.SetActive(true);
        if (!shaking) { PlaySfx(shockSfx); StartShake(); }

        SetBlackBarsActive(true);
        yield return SlideBlackBars();
        if (waitBeforeFade > 0f) yield return new WaitForSeconds(waitBeforeFade);

        yield return FadeToWhiteThenBlack();
        StopShake();

        if (jokerBgm != null && AudioDirector.Instance != null) { AudioDirector.Instance.PushBGM(jokerBgm); bgmPushed = true; }

        if (jokerDialogueDelay > 0f) yield return new WaitForSeconds(jokerDialogueDelay);
        if (jokerBlackBG != null) jokerBlackBG.SetActive(true);
        yield return PlaySequence(jokerDialogue);

        // 4. 收尾
        CloseJokerAni();
        if (fadeBackAfterSequence) yield return FadeImageTo(new Color(0f, 0f, 0f, 0f), blackFadeDuration);

        if (player != null) player.PopInputHold();
        onSequenceFinished?.Invoke();
        SequenceFinished?.Invoke();
    }

    /// <summary>收起 Joker 演出（隱藏 Joker/背景、黑邊，還原 BGM）。</summary>
    public void CloseJokerAni()
    {
        if (joker != null) joker.SetActive(false);
        if (jokerBlackBG != null) jokerBlackBG.SetActive(false);
        SetBlackBarsActive(false);
        StopShake();
        if (bgmPushed && AudioDirector.Instance != null) AudioDirector.Instance.PopBGM();
        bgmPushed = false;
    }

    private IEnumerator StartShakeAtLine()
    {
        DialogueData first = FirstValid(bossDefeatedDialogue);
        if (typer == null || first == null) yield break;
        // DialogueTypingEffect.currentLineIndex 在某句顯示時＝該句的序號（從 1 起算）
        while (!(typer.CurrentData == first && typer.currentLineIndex >= shakeStartLine)) yield return null;
        PlaySfx(shockSfx);
        StartShake();
    }

    private IEnumerator SlideBlackBars()
    {
        if (topBlackBar == null || bottomBlackBar == null) yield break;
        float t = 0f;
        while (t < blackBarSpeed)
        {
            float p = t / blackBarSpeed;
            topBlackBar.rectTransform.anchorMin = new Vector2(0f, Mathf.Lerp(1f, 1f - blackBarHeight, p));
            bottomBlackBar.rectTransform.anchorMax = new Vector2(1f, Mathf.Lerp(0f, blackBarHeight, p));
            t += Time.deltaTime;
            yield return null;
        }
        topBlackBar.rectTransform.anchorMin = new Vector2(0f, 1f - blackBarHeight);
        bottomBlackBar.rectTransform.anchorMax = new Vector2(1f, blackBarHeight);
    }

    private IEnumerator FadeToWhiteThenBlack()
    {
        if (fadeImage == null) yield break;
        PlaySfx(spaceExplosionSfx);
        PlaySfx(spaceShipSfx);

        fadeImage.color = new Color(1f, 1f, 1f, 0f);
        yield return FadeImageTo(Color.white, fadeDuration);
        if (waitAfterFade > 0f) yield return new WaitForSeconds(waitAfterFade);
        yield return FadeImageTo(Color.black, blackFadeDuration);
    }

    private IEnumerator FadeImageTo(Color target, float duration)
    {
        if (fadeImage == null) yield break;
        Color from = fadeImage.color;
        float t = 0f;
        while (t < duration)
        {
            fadeImage.color = Color.Lerp(from, target, t / duration);
            t += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = target;
    }

    // ────────────────────────────────────────────────────────────────
    // 鏡頭震動
    // ────────────────────────────────────────────────────────────────

    /// <summary>開始持續震動鏡頭（直到 <see cref="StopShake"/>）。</summary>
    public void StartShake() => shaking = mainCamera != null;

    /// <summary>停止震動並把鏡頭偏移歸零。</summary>
    public void StopShake()
    {
        shaking = false;
        RemoveShakeOffset();
    }

    private void Update() => RemoveShakeOffset(); // 早於所有 LateUpdate：先扣掉上一影格的偏移

    private void LateUpdate()
    {
        if (!shaking || mainCamera == null) return;
        appliedShakeOffset = UnityEngine.Random.insideUnitSphere * shakeIntensity;
        appliedShakeOffset.z = 0f; // 正面相機只在畫面平面上晃
        mainCamera.transform.position += appliedShakeOffset;
    }

    private void RemoveShakeOffset()
    {
        if (appliedShakeOffset == Vector3.zero || mainCamera == null) return;
        mainCamera.transform.position -= appliedShakeOffset;
        appliedShakeOffset = Vector3.zero;
    }

    // ────────────────────────────────────────────────────────────────
    // 工具
    // ────────────────────────────────────────────────────────────────

    private IEnumerator PlaySequence(LevelMapTutorialDirector.DialogueSequence seq)
    {
        if (dialogue == null || seq == null || !seq.HasAny) yield break;
        while (dialogue.IsPlaying) yield return null; // 別蓋掉其他正在播的對話（如教學）
        yield return dialogue.PlayDialogueSequenceRoutine(seq.dialogues, seq.keepUIOpenBetween);
    }

    private static DialogueData FirstValid(LevelMapTutorialDirector.DialogueSequence seq)
    {
        if (seq == null || seq.dialogues == null) return null;
        foreach (var d in seq.dialogues) if (d != null && d.LineCount > 0) return d;
        return null;
    }

    private void PlaySfx(AudioClip clip)
    {
        if (clip == null) return;
        if (AudioDirector.Instance != null) AudioDirector.Instance.PlaySFX(clip);
        else AudioSource.PlayClipAtPoint(clip, mainCamera != null ? mainCamera.transform.position : Vector3.zero);
    }

    private void SetBlackBarsActive(bool active)
    {
        if (topBlackBar != null) topBlackBar.gameObject.SetActive(active);
        if (bottomBlackBar != null) bottomBlackBar.gameObject.SetActive(active);
        if (active)
        {
            // 從畫面外開始滑入
            if (topBlackBar != null) topBlackBar.rectTransform.anchorMin = new Vector2(0f, 1f);
            if (bottomBlackBar != null) bottomBlackBar.rectTransform.anchorMax = new Vector2(1f, 0f);
        }
    }

    /// <summary>黑邊/淡出圖未指定時，建一個全螢幕演出 Canvas（不擋點擊）。</summary>
    private void EnsureOverlay()
    {
        if (topBlackBar != null && bottomBlackBar != null && fadeImage != null) return;

        var canvasGO = new GameObject("JokerSecret_Overlay", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGO.transform.SetParent(transform, false);
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = overlaySortingOrder;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);

        if (fadeImage == null) fadeImage = CreateImage(canvasGO.transform, "Fade", new Vector2(0, 0), new Vector2(1, 1), new Color(1, 1, 1, 0));
        if (topBlackBar == null) topBlackBar = CreateImage(canvasGO.transform, "TopBlackBar", new Vector2(0, 1), new Vector2(1, 1), Color.black);
        if (bottomBlackBar == null) bottomBlackBar = CreateImage(canvasGO.transform, "BottomBlackBar", new Vector2(0, 0), new Vector2(1, 0), Color.black);
    }

    private static Image CreateImage(Transform parent, string objName, Vector2 anchorMin, Vector2 anchorMax, Color color)
    {
        var go = new GameObject(objName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var rt = (RectTransform)go.transform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
        var img = go.GetComponent<Image>();
        img.color = color;
        img.raycastTarget = false; // 演出圖不擋點擊（對話框要能點）
        return img;
    }
}
