using UnityEngine;
using UnityEngine.UI;

public class UI_ButtonControl : MonoBehaviour
{
    // 公共按鈕變量，允許在 Unity 編輯器中將按鈕拖入
    public Button RollTheButton;
    public Button DrawCardsButton;

    void Start()
    {
        // 為按鈕點擊事件添加不同的監聽器
        RollTheButton.onClick.AddListener(() => OnButtonClick(RollTheButton));
        DrawCardsButton.onClick.AddListener(() => OnButtonClick(DrawCardsButton));
    }

    // 接受 Button 參數來控制按鈕的隱藏
    void OnButtonClick(Button button)
    {
        // 將按下的按鈕設為不可視
        button.gameObject.SetActive(false);
    }
}

