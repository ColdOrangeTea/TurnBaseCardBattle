using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 新手教學面板共用基底（由 A_Good_Ink 使用 AI 生成）。
///
/// 一個「翻頁式圖片教學」：一張張教學圖，左/右鈕切換，第一張隱藏左鈕、最後一張隱藏右鈕。
/// <see cref="showUI"/> / <see cref="HideUI"/> 開關面板；若指派了 <see cref="playerController"/>，
/// 開啟時會暫停地圖點擊、關閉時恢復（給地圖上的教學用，戰鬥/設定教學可留空）。
///
/// 原本 TeachForBag / TeachForBattle / TeachForSetting 三支幾乎一模一樣，統一成本基底，
/// 三者改為薄殼子類別（保留各自類別名與 GUID，prefab 綁定不受影響）。
/// </summary>
public abstract class TeachPanelBase : MonoBehaviour
{
    [Header("UI 元件")]
    public GameObject TeachUI;   // 教學面板根（開/關）
    public Image displayImage;   // 顯示教學圖的 Image
    public Button leftButton;    // 上一張
    public Button rightButton;   // 下一張

    [Header("教學圖片")]
    public Sprite[] images;

    [Header("可選：開啟時暫停地圖點擊（地圖上的教學用；戰鬥/設定可留空）")]
    public S001_PlayerController playerController;

    private int currentIndex = 0;

    protected virtual void Start()
    {
        UpdateImage();
        UpdateButtons();
        if (leftButton != null) leftButton.onClick.AddListener(ShowPreviousImage);
        if (rightButton != null) rightButton.onClick.AddListener(ShowNextImage);
    }

    public void ShowPreviousImage()
    {
        if (currentIndex > 0) { currentIndex--; UpdateImage(); UpdateButtons(); }
    }

    public void ShowNextImage()
    {
        if (images != null && currentIndex < images.Length - 1) { currentIndex++; UpdateImage(); UpdateButtons(); }
    }

    private void UpdateImage()
    {
        if (displayImage != null && images != null && images.Length > 0)
            displayImage.sprite = images[Mathf.Clamp(currentIndex, 0, images.Length - 1)];
    }

    private void UpdateButtons()
    {
        int last = (images != null ? images.Length : 0) - 1;
        if (leftButton != null) leftButton.gameObject.SetActive(currentIndex > 0);
        if (rightButton != null) rightButton.gameObject.SetActive(currentIndex < last);
    }

    /// <summary>開啟教學面板（回到第一張），必要時暫停地圖點擊。</summary>
    public virtual void showUI()
    {
        currentIndex = 0;
        UpdateImage();
        UpdateButtons();
        if (TeachUI != null) TeachUI.SetActive(true);
        if (playerController != null) playerController.EnableBlocking();
    }

    /// <summary>關閉教學面板，恢復地圖點擊。</summary>
    public virtual void HideUI()
    {
        if (TeachUI != null) TeachUI.SetActive(false);
        if (playerController != null) playerController.DisableBlocking();
    }
}
