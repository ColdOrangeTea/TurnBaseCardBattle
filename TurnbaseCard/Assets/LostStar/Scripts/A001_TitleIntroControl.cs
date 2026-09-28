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
    [SerializeField] private List<Animation> introList; // 依播放順序：0=TeamLogo(TeamList) 1=TitleLogoForStart 2=BackGroundFadeIn(UIs)
    [SerializeField] private GameObject introGroup;     // 開場容器（播放時開啟並擋住 UI，結束後關閉）

    [Tooltip("開場 TitleLogo 播完（或 skip）時觸發一次，例如接 SceneBGM.Play 播本場景 BGM。")]
    [SerializeField] private UnityEvent onIntroFinished;

    [Tooltip("Skip 時的快速淡入/淡出秒數（logo 淡出、背景淡入，從當前 alpha 平滑補到目標，不會閃爍）。")]
    [SerializeField] private float skipFadeDuration = 0.4f;

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
        SetClipAndPlay(2, "A_BackGroundFadeIn");    // UIs 背景淡入
        yield return WaitPlaying(2);

        Finish();
    }

    // skip 版：程式驅動快速淡入淡出（logo 淡出→0、背景淡入→1，從當前 alpha 平滑補到目標，無閃爍）。
    // 背景 = introList 最後一個（UIs），其餘 = logo。
    private IEnumerator PlaySkip()
    {
        Signal();
        StopAll(); // 停掉開場動畫，改由程式接手 alpha，避免 clip 從 time 0 重播造成倒退閃爍

        int last = (introList != null) ? introList.Count - 1 : -1;
        // 記錄各索引的起始 alpha 與目標（logo→0、背景→1）
        var starts = new float[introList != null ? introList.Count : 0];
        for (int i = 0; i < starts.Length; i++) starts[i] = GetAlpha(i);

        float dur = Mathf.Max(0.0001f, skipFadeDuration);
        float t = 0f;
        while (t < dur)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            for (int i = 0; i < starts.Length; i++)
                SetAlpha(i, Mathf.Lerp(starts[i], i == last ? 1f : 0f, k));
            yield return null;
        }
        for (int i = 0; i < starts.Length; i++) SetAlpha(i, i == last ? 1f : 0f);

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

    // 等待指定的多個 Animation 全部播完（不限數量；沒帶索引則立即返回）
    private IEnumerator WaitPlaying(params int[] indices)
    {
        if (indices == null || indices.Length == 0) yield break;
        bool anyPlaying;
        do
        {
            anyPlaying = false;
            foreach (var i in indices) if (IsPlaying(i)) { anyPlaying = true; break; }
            if (anyPlaying) yield return null;
        } while (anyPlaying);
    }

    private bool IsPlaying(int i) =>
        introList != null && i >= 0 && i < introList.Count && introList[i] != null && introList[i].isPlaying;

    private CanvasGroup GetCanvasGroup(int i)
    {
        if (introList == null || i < 0 || i >= introList.Count || introList[i] == null) return null;
        return introList[i].GetComponent<CanvasGroup>();
    }

    private float GetAlpha(int i) { var cg = GetCanvasGroup(i); return cg != null ? cg.alpha : 1f; }
    private void SetAlpha(int i, float a) { var cg = GetCanvasGroup(i); if (cg != null) cg.alpha = a; }

    private void StopAll()
    {
        if (introList == null) return;
        foreach (var a in introList) if (a != null) a.Stop();
    }
}
