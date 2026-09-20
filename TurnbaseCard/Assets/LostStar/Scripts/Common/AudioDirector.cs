using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 全域音訊總管（單一權威）。用「單一 BGM 頻道 + 堆疊」確保同一時間只有一首 BGM，
/// 從根本杜絕地圖 BGM 與商店/戰鬥 BGM 疊在一起：
///   - <see cref="PlayBaseBGM"/>：設定基底 BGM（地圖），清空堆疊；
///   - <see cref="PushBGM"/>：暫時切到另一首（商店/戰鬥），記住原本那首的播放位置；
///   - <see cref="PopBGM"/>：還原上一首（含播放位置），離開商店/戰鬥後接回地圖 BGM。
///   - <see cref="PlaySFX"/>：一次性音效（PlayOneShot）。
///
/// 音量：本元件不碰 volume，交給 <see cref="SimpleVolumeControl"/> 直接套在 bgmSource / sfxSource 上
/// （暫停選單拉條 0~1），維持全專案單一音量模型。由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class AudioDirector : MonoBehaviour
{
    public static AudioDirector Instance { get; private set; }

    [Header("音源（留空會在自身建立）")]
    [Tooltip("單一 BGM 頻道；同時也給 SimpleVolumeControl 當 bgmSource")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("一次性音效來源（PlayOneShot）")]
    [SerializeField] private AudioSource sfxSource;

    // BGM 堆疊：記住被暫時覆蓋的 BGM（含播放位置），Pop 時精準還原
    private struct BgmEntry { public AudioClip clip; public float time; public bool loop; }
    private readonly Stack<BgmEntry> bgmStack = new Stack<BgmEntry>();

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[AudioDirector] 場上已有另一個實例，保留先出現的：{Instance.name}");
            return;
        }
        Instance = this;

        if (bgmSource == null)
        {
            bgmSource = gameObject.AddComponent<AudioSource>();
            bgmSource.playOnAwake = false;
            bgmSource.loop = true;
        }
        if (sfxSource == null)
        {
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;
            sfxSource.loop = false;
        }
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>設定基底 BGM（如地圖），清空堆疊並從頭播放。</summary>
    public void PlayBaseBGM(AudioClip clip, bool loop = true)
    {
        bgmStack.Clear();
        SwitchBgm(clip, 0f, loop);
    }

    /// <summary>暫時切到另一首 BGM（商店/戰鬥），記住目前這首的位置以便 <see cref="PopBGM"/> 還原。</summary>
    public void PushBGM(AudioClip clip, bool loop = true)
    {
        if (bgmSource == null) return;
        if (bgmSource.clip != null)
            bgmStack.Push(new BgmEntry { clip = bgmSource.clip, time = bgmSource.time, loop = bgmSource.loop });
        SwitchBgm(clip, 0f, loop);
    }

    /// <summary>還原上一首 BGM（含播放位置）；堆疊空了就停止。</summary>
    public void PopBGM()
    {
        if (bgmStack.Count > 0)
        {
            var e = bgmStack.Pop();
            SwitchBgm(e.clip, e.time, e.loop);
        }
        else if (bgmSource != null)
        {
            bgmSource.Stop();
            bgmSource.clip = null;
        }
    }

    /// <summary>播放一次性音效（音量吃 sfxSource.volume × volumeScale）。</summary>
    public void PlaySFX(AudioClip clip, float volumeScale = 1f)
    {
        if (sfxSource != null && clip != null) sfxSource.PlayOneShot(clip, volumeScale);
    }

    private void SwitchBgm(AudioClip clip, float time, bool loop)
    {
        if (bgmSource == null) return;
        bgmSource.Stop();
        bgmSource.clip = clip;
        bgmSource.loop = loop;
        if (clip != null)
        {
            bgmSource.time = Mathf.Clamp(time, 0f, Mathf.Max(0f, clip.length - 0.01f));
            bgmSource.Play();
        }
    }
}
