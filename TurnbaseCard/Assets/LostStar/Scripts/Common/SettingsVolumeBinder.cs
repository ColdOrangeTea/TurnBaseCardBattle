using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 設定面板音量拉條 → AudioDirector 的 <see cref="SimpleVolumeControl"/> 執行期綁定器
/// （由 A_Good_Ink 使用 AI 生成）。
///
/// 為什麼要這支：拉條在 UI prefab 內，而 SimpleVolumeControl 在場景上的 AudioDirector；
/// 若用 Inspector 直接把拉條 onValueChanged 接到場景物件，這種「prefab 實例 → 場景物件」的覆寫
/// 會在 prefab 重載時遺失。改由本元件在執行期用 <c>FindAnyObjectByType</c> 找到 SimpleVolumeControl
/// 並以 AddListener 綁定，不依賴任何序列化的跨物件引用，重載 prefab 也不會斷。
///
/// 用法：把本元件掛在設定面板上，於 Inspector 指定 <see cref="bgmSlider"/> / <see cref="sfxSlider"/>
/// （皆為 prefab 內部的拉條，序列化引用安全）。
/// </summary>
public class SettingsVolumeBinder : MonoBehaviour
{
    [Tooltip("背景音樂拉條（0~1）")]
    [SerializeField] private Slider bgmSlider;
    [Tooltip("音效拉條（0~1）")]
    [SerializeField] private Slider sfxSlider;

    private void Start()
    {
        var svc = FindAnyObjectByType<SimpleVolumeControl>();
        if (svc == null)
        {
            Debug.LogWarning("[SettingsVolumeBinder] 場上找不到 SimpleVolumeControl（通常在 AudioDirector 上），音量拉條未綁定。");
            return;
        }

        if (bgmSlider != null)
        {
            bgmSlider.minValue = 0f; bgmSlider.maxValue = 1f;
            bgmSlider.SetValueWithoutNotify(svc.BgmVolume); // 顯示目前音量
            bgmSlider.onValueChanged.AddListener(svc.SetBGMVolume);
        }
        if (sfxSlider != null)
        {
            sfxSlider.minValue = 0f; sfxSlider.maxValue = 1f;
            sfxSlider.SetValueWithoutNotify(svc.SfxVolume);
            sfxSlider.onValueChanged.AddListener(svc.SetSFXVolume);
        }
    }
}
