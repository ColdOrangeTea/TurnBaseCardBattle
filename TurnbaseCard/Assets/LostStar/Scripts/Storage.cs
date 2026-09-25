using UnityEngine;
using UnityEngine.UI;
using DG.Tweening; // 確保已匯入 DOTween 的命名空間

public class Storage : MonoBehaviour
{
    public RectTransform Hint; // Panel 的 RectTransform
    public Button Hint_button; // 初始按鈕
    public Button Hint_button2; // 第二個按鈕

    private Vector3 originalPosition; // 保存初始位置
    private bool isMoved = false; // 判斷是否已經移動

    void Start()
    {
        // 保存 Hint 的初始位置
        if (Hint != null)
        {
            originalPosition = Hint.localPosition;
        }

        // 為按鈕加上點擊事件
        if (Hint_button != null)
        {
            Hint_button.onClick.AddListener(MoveHintToLeft);
        }

        if (Hint_button2 != null)
        {
            Hint_button2.onClick.AddListener(MoveHintToOriginalPosition);
            Hint_button2.gameObject.SetActive(false); // 初始隱藏 Hint_button2
        }
    }

    void MoveHintToLeft()
    {
        if (Hint == null || isMoved) return;

        // 向左移動 369 像素
        Hint.DOLocalMoveX(originalPosition.x - 369, 0.5f).OnComplete(() =>
        {
            // 切換按鈕顯示
            Hint_button.gameObject.SetActive(false);
            if (Hint_button2 != null)
            {
                Hint_button2.gameObject.SetActive(true);
            }
        });

        isMoved = true;
    }

    void MoveHintToOriginalPosition()
    {
        if (Hint == null || !isMoved) return;

        // 回到原位
        Hint.DOLocalMoveX(originalPosition.x, 0.5f).OnComplete(() =>
        {
            // 切換按鈕顯示
            if (Hint_button2 != null)
            {
                Hint_button2.gameObject.SetActive(false);
            }
            Hint_button.gameObject.SetActive(true);
        });

        isMoved = false;
    }
}
