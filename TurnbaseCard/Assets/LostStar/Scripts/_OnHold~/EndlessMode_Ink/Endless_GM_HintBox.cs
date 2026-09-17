using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Unity.VisualScripting;
using TMPro;

public class Endless_GM_HintBox : MonoBehaviour
{
    private UIBarMove uIBarMove = new UIBarMove();

    [Header("UI物件")]
    [SerializeField] private Button HintBoxButton; // 收回UI的按鈕
    [SerializeField] private RectTransform HintBoxUI; // 提示 Box 的 RectTransform
    [SerializeField] private RectTransform IllustrateEmptyUI;
    [SerializeField] private Button SwitchButton_Back; // 翻頁
    [SerializeField] private Button SwitchButton_Next; // 翻頁

    [Header("面板數量")]
    [SerializeField] private int IllustrateTotalCount = 0;
    [SerializeField] private int IllustrateCurPageCount = 0;

    [SerializeField] private List<GameObject> illustratePages; // 會顯示的頁數
    [SerializeField] private bool isHintBoxOpen = false; // 開啟狀態

    [Header("面板移動動畫設定")]
    [SerializeField] private Vector2 closedPosition = new Vector2(-1800, 0); // 收回的位置（在螢幕左側）
    [SerializeField] private Vector2 openedPosition = new Vector2(0, 0); // 展開的位置（螢幕內）
    [SerializeField] private float animationDuration = 0.5f; // 動畫持續時間
    [SerializeField] private Ease easeType = Ease.OutCubic;

    public Button GetHintBoxButton() => HintBoxButton;
    void Start()
    {
        // 綁定按鈕事件
        Init();
    }
    void Init()
    {
        // 檢查引用是否為 null
        if (HintBoxButton == null)
            Debug.LogError("HintBoxButton 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (HintBoxUI == null)
            Debug.LogError("HintBoxUI 未設置。請在 Inspector 中分配相應的 RectTransform 物件。");

        if (IllustrateEmptyUI == null)
            Debug.LogError("IllustrateEmptyUI 未設置。請在 Inspector 中分配相應的 RectTransform 物件。");

        if (SwitchButton_Back == null)
            Debug.LogError("SwitchButton_Back 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (SwitchButton_Next == null)
            Debug.LogError("SwitchButton_Next 未設置。請在 Inspector 中分配相應的按鈕物件。");

        if (illustratePages == null || illustratePages.Count == 0)
            Debug.LogError("illustratePages 未設置或沒有分配任何頁面。請在 Inspector 中分配頁面物件。");

        // 綁定按鈕事件（前提是引用已經設置）
        if (HintBoxButton != null)
            HintBoxButton.onClick.AddListener(() => OpenHintBox());

        if (SwitchButton_Back != null)
            SwitchButton_Back.onClick.AddListener(() => PageBack());

        if (SwitchButton_Next != null)
            SwitchButton_Next.onClick.AddListener(() => PageNext());

        // 初始化頁面數量
        IllustrateTotalCount = illustratePages?.Count ?? 0;
    }
    void PageBack()
    {
        if (IllustrateCurPageCount - 1 >= 0)
            IllustrateCurPageCount -= 1;
        DisplayContent();
    }

    void PageNext()
    {
        if (IllustrateCurPageCount + 1 < IllustrateTotalCount)
        {
            IllustrateCurPageCount += 1;
        }
        else
        {
            isHintBoxOpen = false;
            ToggleHintBox(isHintBoxOpen); // UI收回去
        }

        DisplayContent();
    }
    void DisplayContent()
    {
        DisplayPageBackAsEnd();
        DisplayPageNextAsNone();
        DisplayIllustrate();
    }
    void DisplayPageBackAsEnd()
    {
        if (IllustrateCurPageCount == 0)
        {
            SwitchButton_Back.transform.GetChild(0).GetComponent<TMP_Text>().text = "";
        }
        else
        {
            SwitchButton_Back.transform.GetChild(0).GetComponent<TMP_Text>().text = "<";
        }

    }
    void DisplayPageNextAsNone()
    {
        if (IllustrateCurPageCount + 1 == IllustrateTotalCount)
        {
            SwitchButton_Next.transform.GetChild(0).GetComponent<TMP_Text>().text = "結束";
        }
        else
        {
            SwitchButton_Next.transform.GetChild(0).GetComponent<TMP_Text>().text = ">";
        }
    }
    void DisplayIllustrate()
    {
        foreach (GameObject page in illustratePages)
        {
            if (page == illustratePages[IllustrateCurPageCount])
            {
                illustratePages[IllustrateCurPageCount].SetActive(true);
            }
            else
            {
                page.SetActive(false);
            }
        }
    }

    void ResetIllustrateOrder()
    {
        IllustrateCurPageCount = 0;
        foreach (GameObject page in illustratePages)
        {
            page.SetActive(false);
        }
    }

    void OpenHintBox()
    {
        if (isHintBoxOpen)
        {
            isHintBoxOpen = false;
        }
        else
        {
            isHintBoxOpen = true;
            ResetIllustrateOrder();
            DisplayContent();
        }

        ToggleHintBox(isHintBoxOpen);
    }

    void ToggleHintBox(bool open)
    {
        SetButtonsInteractable(false); // 禁用按鈕   
        Vector2 targetPosition = open ? openedPosition : closedPosition; // 設定目標位置

        // 使用 DOAnchorPos 進行動畫，並在完成時執行回調
        HintBoxUI.DOAnchorPos(targetPosition, animationDuration)
            .SetEase(easeType)
            .OnComplete(() =>
            {
                // 動畫完成後重新啟用按鈕
                SetButtonsInteractable(true);
            });
    }

    void SetButtonsInteractable(bool state) // 方法：設置按鈕的可用狀態
    {
        HintBoxButton.interactable = state;
        SwitchButton_Back.interactable = state;
        SwitchButton_Next.interactable = state;
    }

}
