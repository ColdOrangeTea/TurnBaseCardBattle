using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 最小可用的音量控制（由 A_Good_Ink 使用 AI 生成）。
///
/// 直接把 UI Slider（0~1）的數值套到指定的 AudioSource.volume 上，
/// 不依賴 AudioMixer / 舊設定管理器。給暫停選單的「背景音樂 / 音效」拉條使用：
/// 把 Slider.onValueChanged 綁到 <see cref="SetBGMVolume"/> / <see cref="SetSFXVolume"/> 即可。
/// </summary>
public class SimpleVolumeControl : MonoBehaviour
{
    [Tooltip("背景音樂來源（BGM 拉條控制其音量）")]
    [SerializeField] private AudioSource bgmSource;

    [Tooltip("音效來源清單（音效拉條一起控制其音量）")]
    [SerializeField] private List<AudioSource> sfxSources = new List<AudioSource>();

    /// <summary>背景音樂音量（0~1）。</summary>
    public void SetBGMVolume(float value01)
    {
        if (bgmSource != null) bgmSource.volume = Mathf.Clamp01(value01);
    }

    /// <summary>音效音量（0~1），套用到所有指定的音效來源。</summary>
    public void SetSFXVolume(float value01)
    {
        float v = Mathf.Clamp01(value01);
        foreach (var s in sfxSources)
            if (s != null) s.volume = v;
    }
}
