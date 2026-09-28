using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 首頁開場（Team Logo → Title Logo → 淡入）動畫控制（由 A_Good_Ink 使用 AI 生成/重構）。
///
/// 行為：
///   - 啥都不操作：完整播放開場動畫，播完才關閉 Intro → 玩家才能操作 UI（Intro 期間 introGroup 擋住 UI Raycast）。
///   - Skip：開場播放中，玩家「滑鼠點一下」或「鍵盤任意鍵按一下」→ 立即改播 skip 版快速淡出並結束。
///   - 這場遊戲已看過開場（<see cref="isIntroFinished"/>）：直接走 skip 版（不重播完整開場）。
///
/// 本腳本只管開場動畫時序；BGM 等交由 <see cref="onIntroFinished"/>（例如接 SceneBGM.Play），只觸發一次。
/// </summary>
public class A001_TitleIntroControl : MonoBehaviour
{
    [SerializeField] private List<Animation> introList; // 依播放順序：0=TeamLogo 1=TitleLogoForStart 2=Fade 3=BackGroundFadeIn
    [SerializeField] private GameObject introGroup;     // 開場容器（播放時開啟並擋住 UI，結束後關閉）

    [Tooltip("開場 TitleLogo 播完（或 skip）時觸發一次，例如接 SceneBGM.Play 播本場景 BGM。")]
    [SerializeField] private UnityEvent onIntroFinished;

    /// <summary>這場遊戲是否已看過開場（跨場景保留；再次進首頁直接走 skip 版）。</summary>
    public static bool isIntroFinished = false;

    private bool ended = false;    // 是否已進入結束流程（避免重複 skip/finish）
    private bool signaled = false; // onIntroFinished 是否已觸發
    private Coroutine routine;

    void Start()
    {
        if (introGroup != null) introGroup.SetActive(true); // 開場：開啟 Intro（同時擋住 UI，播完/skip 才放行）
        routine = StartCoroutine(isIntroFinished ? PlaySkip() : PlayFull());
    }

    void Update()
    {
        // 開場播放中：滑鼠點一下或按任意鍵 → 直接 skip
        if (!ended && (Input.GetMouseButtonDown(0) || Input.anyKeyDown))
            Skip();
    }

    // 完整開場：TeamLogo + TitleLogo → 淡入
    private IEnumerator PlayFull()
    {
        SetClipAndPlay(0, "A_TeamLogo");
        SetClipAndPlay(1, "A_TitleLogoForStart");
        yield return WaitPlaying(0, 1);
        Signal(); // TitleLogo 播完 → 通知（BGM）

        SetClipAndPlay(2, "A_FadeAnimation");
        SetClipAndPlay(3, "A_BackGroundFadeIn");
        yield return WaitPlaying(2, 3);

        Finish();
    }

    // skip 版：快速淡出後結束
    private IEnumerator PlaySkip()
    {
        Signal();
        StopAll();
        SetClipAndPlay(2, "A_FadeAnimation_SkipIntro");
        SetClipAndPlay(3, "A_BackGroundFadeIn_SkipIntro");
        yield return WaitPlaying(2, 3);
        Finish();
    }

    // 玩家在開場中要求 skip
    private void Skip()
    {
        if (ended) return;
        ended = true;
        if (routine != null) StopCoroutine(routine);
        routine = StartCoroutine(PlaySkip());
    }

    private void Finish()
    {
        ended = true;
        isIntroFinished = true;
        Signal();
        if (introGroup != null) introGroup.SetActive(false); // 關閉 Intro → 玩家可操作 UI
    }

    private void Signal()
    {
        if (signaled) return;
        signaled = true;
        onIntroFinished?.Invoke();
    }

    // ── 輔助 ──
    private void SetClipAndPlay(int i, string clipName)
    {
        if (introList == null || i >= introList.Count || introList[i] == null) return;
        var clip = introList[i].GetClip(clipName);
        if (clip != null) introList[i].clip = clip;
        introList[i].Play();
    }

    private IEnumerator WaitPlaying(int a, int b)
    {
        while (IsPlaying(a) || IsPlaying(b)) yield return null;
    }

    private bool IsPlaying(int i) =>
        introList != null && i >= 0 && i < introList.Count && introList[i] != null && introList[i].isPlaying;

    private void StopAll()
    {
        if (introList == null) return;
        foreach (var a in introList) if (a != null) a.Stop();
    }
}
