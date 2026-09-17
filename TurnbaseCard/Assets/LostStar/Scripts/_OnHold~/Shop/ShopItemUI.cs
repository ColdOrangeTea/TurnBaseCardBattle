using UnityEngine;
using TMPro;

public class ShopItemUI : MonoBehaviour
{
    public string objectName; // 物件名稱
    [TextArea]
    public string description; // 物件敘述

    public GameObject tooltipUI; // 指向顯示名稱和敘述的UI
    public TextMeshProUGUI nameText; // UI中的名稱文字
    public TextMeshProUGUI descriptionText; // UI中的敘述文字

    private void Start()
    {
        // 初始隱藏UI
        if (tooltipUI != null)
        {
            tooltipUI.SetActive(false);
        }
    }

    // 當滑鼠移到物件上時觸發
    private void OnMouseEnter()
    {
        if (tooltipUI != null)
        {
            tooltipUI.SetActive(true); // 顯示UI
            nameText.text = objectName; // 更新名稱
            descriptionText.text = description; // 更新敘述
        }
        if (tooltipUI.activeSelf)
        {
            // 讓UI跟隨滑鼠位置
            Vector3 mousePosition = Input.mousePosition;
            tooltipUI.transform.position = mousePosition + new Vector3(10, 10, 0); // 調整偏移量
        }
    }

    // 當滑鼠移出物件時觸發
    private void OnMouseExit()
    {
        if (tooltipUI != null)
        {
            tooltipUI.SetActive(false); // 隱藏UI
        }
    }
}
