using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 按鈕點擊音效（重構：改由全域 <see cref="AudioDirector"/> 播放，不再用本地 AudioSource）。
///
/// 在 Inspector 指定 <see cref="myButton"/> 與 <see cref="buttonClickSound"/>；按鈕被點擊時把音效
/// 交給 AudioDirector.PlaySFX 播放（音量由 SimpleVolumeControl 統一控制）。
/// 由 A_Good_Ink 使用 AI 生成/重構。
/// </summary>
public class V002_Button_SoundEffect : MonoBehaviour
{
    [Tooltip("要監聽點擊的按鈕（在 Inspector 指定）。")]
    public Button myButton;

    [Tooltip("點擊時播放的音效（在 Inspector 指定）。")]
    public AudioClip buttonClickSound;

    void Start()
    {
        if (myButton != null)
            myButton.onClick.AddListener(OnButtonClick);
        else
            Debug.LogError("[V002_Button_SoundEffect] 未在 Inspector 指定 myButton。");

        if (buttonClickSound == null)
            Debug.LogWarning("[V002_Button_SoundEffect] 未在 Inspector 指定 buttonClickSound。");
    }

    // 按鈕被點擊時：交給 AudioDirector 播放（吃全域 SFX 音量）
    void OnButtonClick()
    {
        if (buttonClickSound == null) return;
        if (AudioDirector.Instance != null)
            AudioDirector.Instance.PlaySFX(buttonClickSound);
        else
            Debug.LogWarning("[V002_Button_SoundEffect] 場上沒有 AudioDirector，按鈕音效未播放。");
    }
}
