using UnityEngine;
using UnityEngine.UI;

public class UI_SettingPanel : MonoBehaviour
{
    [Header("UI References")]
    public GameObject Setting; // 引用設定面板
    public Button toggleButton; // 按鈕用來切換面板狀態

    void Start()
    {
        // 確保 Setting 面板初始狀態為不可視
        if (Setting != null)
        {
            Setting.SetActive(false);
        }

        // 設定按鈕點擊事件
        if (toggleButton != null)
        {
            toggleButton.onClick.AddListener(ToggleSettingPanel);
        }
    }

    // 切換 Setting 面板的顯示狀態
    void ToggleSettingPanel()
    {
        if (Setting != null)
        {
            Setting.SetActive(!Setting.activeSelf);
        }
    }
}
