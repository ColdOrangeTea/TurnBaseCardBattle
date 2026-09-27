using UnityEngine;

/// <summary>
/// 本場景的背景音樂宣告（由 A_Good_Ink 使用 AI 生成）。
///
/// 只負責「告訴跨場景常駐的 <see cref="AudioDirector"/>：這個場景的基底 BGM 是哪一首」，
/// 實際播放交給 AudioDirector、音量交給 SimpleVolumeControl。取代原本每個場景各自持有 AudioSource 播放的做法。
///
///   - <see cref="playOnStart"/>=true：進場景即播（一般場景）。
///   - =false：等外部觸發（例如 HomePage 由開場動畫播完後呼叫 <see cref="Play"/>）。
/// </summary>
public class SceneBGM : MonoBehaviour
{
    [Tooltip("這個場景的基底 BGM。")]
    [SerializeField] private AudioClip bgm;
    [Tooltip("是否循環播放。")]
    [SerializeField] private bool loop = true;
    [Tooltip("進場景就播；false＝等外部呼叫 Play（例如開場動畫結束）。")]
    [SerializeField] private bool playOnStart = true;

    private void Start()
    {
        if (playOnStart) Play();
    }

    /// <summary>叫跨場景常駐的 AudioDirector 播放本場景的基底 BGM。</summary>
    public void Play()
    {
        if (bgm == null) { Debug.LogWarning($"[SceneBGM]「{name}」未指定 BGM。"); return; }
        if (AudioDirector.Instance != null) AudioDirector.Instance.PlayBaseBGM(bgm, loop);
        else Debug.LogWarning("[SceneBGM] 場上沒有 AudioDirector，BGM 未播放。");
    }
}
