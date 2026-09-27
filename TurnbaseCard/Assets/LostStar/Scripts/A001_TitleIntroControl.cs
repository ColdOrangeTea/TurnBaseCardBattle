using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class A001_TitleIntroControl : MonoBehaviour
{
    [SerializeField] private List<Animation> introList; // 手動指定物件，按撥放順序放 從0開始排
    [SerializeField] private GameObject introGroup; // 手動指定物件

    [Tooltip("開場 TitleLogo 播完時觸發（例如接 SceneBGM.Play 播本場景 BGM）。本腳本只管動畫、不碰聲音。")]
    [SerializeField] private UnityEvent onIntroFinished;
    bool playHomePageIntro = true;
    bool isTitleLogoForStartFinished = false;

    // bool isIntroFinished = false;
    static public bool isIntroFinished = false;

    Coroutine PlayIntro;
    void Update()
    {
        InEditorSkipIntro();
        if (playHomePageIntro)
        {
            playHomePageIntro = false;
            if (PlayIntro != null)
            {
                StopCoroutine(PlayIntro);
            }
            PlayIntro = StartCoroutine(PlayHomePageLogoIntro()); // 使用協程播放動畫

        }
        SignalIntroFinishedOnce();
    }

    void InEditorSkipIntro()
    {
        // 原本依賴已擱置的 GameManager.instance.IsInEditor，改用 Unity 內建 Application.isEditor
        // （編輯器內＝開發中，直接跳過開場動畫）。
        if (Application.isEditor)
        {
            isIntroFinished = true;
            CloseIntro();
            // playHomePageIntro = false;
        }

    }

    // 開場 TitleLogo(introList[1]) 播完的那一刻，觸發 onIntroFinished（只觸發一次）。
    // 本腳本只負責動畫時序，不碰聲音——BGM 由外部接（例如 SceneBGM.Play）到 onIntroFinished。
    private void SignalIntroFinishedOnce()
    {
        if (isTitleLogoForStartFinished) return;
        if (introList == null || introList.Count < 2 || introList[1] == null) return;
        if (!introList[1].isPlaying)
        {
            isTitleLogoForStartFinished = true;
            onIntroFinished?.Invoke();
        }
    }

    private IEnumerator PlayHomePageLogoIntro()
    {
        if (isIntroFinished)
        {
            CloseIntro();
            introList[2].GetComponent<Animation>().clip = introList[2].GetComponent<Animation>().GetClip("A_FadeAnimation_SkipIntro");
            introList[2].GetComponent<Animation>().Play(); // 撥放 "TitleLogoForStart" introList[2]

            introList[3].GetComponent<Animation>().clip = introList[3].GetComponent<Animation>().GetClip("A_BackGroundFadeIn_SkipIntro");
            introList[3].GetComponent<Animation>().Play();
            yield return null;
        }
        else
        {
            // 撥放 introList[0] 和 introList[1]
            introList[0].clip = introList[0].GetClip("A_TeamLogo");
            introList[1].clip = introList[1].GetClip("A_TitleLogoForStart");

            introList[0].Play();
            introList[1].Play();

            // 等待 introList[0] 和 introList[1] 撥放完
            while (introList[0].isPlaying || introList[1].isPlaying)
            {
                yield return null;
            }

            // Debug.Log("Intro Part 1 Finished.");

            // 撥放 introList[2] 和 introList[3]
            introList[2].clip = introList[2].GetClip("A_FadeAnimation");
            introList[3].clip = introList[3].GetClip("A_BackGroundFadeIn");

            introList[2].Play();
            introList[3].Play();

            // 等待 introList[2] 和 introList[3] 撥放完
            while (introList[2].isPlaying || introList[3].isPlaying)
            {
                yield return null;
            }

            // Debug.Log("Intro Part 2 Finished.");
            isIntroFinished = true;
            CloseIntro();
        }

    }

    // 關閉Intro的Function 即使動畫撥完圖像透明度至0，還是會遮擋到UI的Raycast導致按鈕沒反應 所以要關起來
    void CloseIntro()
    {
        if (isIntroFinished && introGroup.activeInHierarchy)
        {
            introGroup.SetActive(false);
        }
    }
}
