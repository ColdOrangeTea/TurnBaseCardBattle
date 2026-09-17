using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新手教學觸發鈕（由 A_Good_Ink 使用 AI 生成）。
///
/// 掛在一顆「?」按鈕上：點擊 → 開啟指定的教學面板 <see cref="teachPanel"/>；
/// 若 <see cref="onlyOnce"/> 為真，觸發一次後把按鈕設為不可點（避免重複彈教學）。
///
/// 已脫離 V2 重構移除的舊對話系統（DialogueManager/DialogueOpenClose）——直接開教學面板即可。
/// 各情境（商店/背包/戰鬥/設定…）用同一顆 GuideButton，指到各自的 <see cref="TeachPanelBase"/> 即可。
/// </summary>
[RequireComponent(typeof(Button))]
public class GuideButton : MonoBehaviour
{
    [Tooltip("要開啟的教學面板")]
    [SerializeField] private TeachPanelBase teachPanel;

    [Tooltip("只觸發一次後停用按鈕")]
    [SerializeField] private bool onlyOnce = true;

    private Button button;

    protected virtual void Awake()
    {
        button = GetComponent<Button>();
        if (button != null) button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (teachPanel != null) teachPanel.showUI();
        else Debug.LogWarning($"[{name}] GuideButton 未指派 teachPanel。");

        if (onlyOnce && button != null) button.interactable = false;
    }
}
