using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最小可用的音量控制（由 A_Good_Ink 使用 AI 生成）。
///
/// 直接把 0~1 音量套到指定的 AudioSource.volume 上，不依賴 AudioMixer / 舊設定管理器。
/// 分三段：主音量(master) × 背景音樂(bgm) / 音效(sfx)——實際音量 = master × 各自。
///
/// 兩種調法：
///   - **開發者**：直接拉 Inspector 上的三條 [Range] 拉桿（主音量／BGM／音效預設值），
///     編輯或執行中拖動都會即時套用（OnValidate / 對應 Setter）。
///   - **玩家**：暫停選單的 BGM／音效拉條 → 綁 <see cref="SetBGMVolume"/> / <see cref="SetSFXVolume"/>。
/// 與 <see cref="AudioDirector"/> 共用同一組 source：本元件只改 volume，AudioDirector 只換 clip，互不干擾。
/// </summary>
public class SimpleVolumeControl : MonoBehaviour
{
    [Header("預設音量（開發者用；玩家的暫停選單拉條會覆蓋 BGM/音效）")]
    [Tooltip("主音量：同時影響 BGM 與音效（實際 = 主音量 × 各自音量）")]
    [Range(0f, 1f)][SerializeField] private float masterVolume = 0.5f;
    [Tooltip("背景音樂音量")]
    [Range(0f, 1f)][SerializeField] private float bgmVolume = 0.4f;
    [Tooltip("音效音量")]
    [Range(0f, 1f)][SerializeField] private float sfxVolume = 0.3f;

    [Header("音源")]
    [Tooltip("背景音樂來源（通常是 AudioDirector 的 BGM 頻道）")]
    [SerializeField] private AudioSource bgmSource;
    [Tooltip("音效來源清單（移動音效、AudioDirector 的一次性音效…）")]
    [SerializeField] private List<AudioSource> sfxSources = new List<AudioSource>();

    /// <summary>目前背景音樂音量（0~1）。</summary>
    public float BgmVolume => bgmVolume;
    /// <summary>目前音效音量（0~1）。</summary>
    public float SfxVolume => sfxVolume;

    /// <summary>全域唯一實例（隨 AudioDirector 一起跨場景常駐）。</summary>
    public static SimpleVolumeControl Instance { get; private set; }

    // 玩家音量設定的存檔鍵（跨遊戲重啟沿用）
    private const string PP_Master = "vol_master", PP_Bgm = "vol_bgm", PP_Sfx = "vol_sfx";

    private void Awake()
    {
        if (Instance != null && Instance != this) return; // 重複的隨 AudioDirector 物件一起被銷毀
        Instance = this;
        // 載入玩家上次的音量設定（沒存過就用 Inspector 預設）
        masterVolume = PlayerPrefs.GetFloat(PP_Master, masterVolume);
        bgmVolume = PlayerPrefs.GetFloat(PP_Bgm, bgmVolume);
        sfxVolume = PlayerPrefs.GetFloat(PP_Sfx, sfxVolume);
        ApplyAll();
    }

    private void OnDestroy() { if (Instance == this) Instance = null; }

    /// <summary>主音量（0~1）：同時縮放 BGM 與音效。</summary>
    public void SetMasterVolume(float value01)
    {
        masterVolume = Mathf.Clamp01(value01); ApplyAll();
        PlayerPrefs.SetFloat(PP_Master, masterVolume);
    }

    /// <summary>背景音樂音量（0~1）。</summary>
    public void SetBGMVolume(float value01)
    {
        bgmVolume = Mathf.Clamp01(value01); ApplyBgm();
        PlayerPrefs.SetFloat(PP_Bgm, bgmVolume);
    }

    /// <summary>音效音量（0~1），套用到所有指定的音效來源。</summary>
    public void SetSFXVolume(float value01)
    {
        sfxVolume = Mathf.Clamp01(value01); ApplySfx();
        PlayerPrefs.SetFloat(PP_Sfx, sfxVolume);
    }

    /// <summary>把目前三段音量重新套到所有來源。</summary>
    public void ApplyAll() { ApplyBgm(); ApplySfx(); }

    private void ApplyBgm()
    {
        if (bgmSource != null) bgmSource.volume = masterVolume * bgmVolume;
    }

    private void ApplySfx()
    {
        float v = masterVolume * sfxVolume;
        foreach (var s in sfxSources)
            if (s != null) s.volume = v;
    }

#if UNITY_EDITOR
    // 編輯器中拖動上面的拉桿時即時預覽（編輯與執行皆可）。
    private void OnValidate() => ApplyAll();
#endif
}
