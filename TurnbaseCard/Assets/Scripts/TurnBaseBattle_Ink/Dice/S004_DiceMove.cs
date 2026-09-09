using System;
using Assets.Scripts.GlobalEnums;
using UnityEngine;
using UnityEngine.EventSystems;

public class S004_DiceMove : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    #region 變數宣告區
    public DiceEvent diceEvent = new DiceEvent();
    [Header("拖曳控制")]
    [SerializeField] private bool isDragging = false; // 是否正在拖曳
    [SerializeField] private RectTransform rectTransform; // 圖片的 RectTransform
    [SerializeField] private Vector3 initialPosition; // 存储初始位置

    [Header("目標面板")]
    [SerializeField] private RectTransform[] panels; // 四個面板的 RectTransform 數組，DicePrefab已經有4個Element了

    [Header("外圍圓環")]
    [SerializeField] private GameObject outerRing; // 新增的 Outer_Ring 物件

    // 計數變數
    private int successDragCount = 0; // 成功拖曳次數


    #endregion

    #region 初始化

    void Awake()
    {
        InitializeComponents();
    }

    void Start()
    {
        // StoreInitialPosition(rectTransform.anchoredPosition); // 存储初始位置
        SetOuterRingVisible(false); // 初始时将 Outer_Ring 隐藏
    }

    #region "墨水新增區"
    public void SetPanels(GameObject group_Cards) // DicePrefab已經有4個Element了，可以直接[i]=資料
    {
        for (int i = 0; i < group_Cards.transform.childCount; i++)
        {
            panels[i] = group_Cards.transform.GetChild(i).GetComponent<RectTransform>();
        }
    }
    #endregion

    // 初始化 RectTransform
    private void InitializeComponents()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    // 存起色子起始位置
    public void StoreInitialPosition(Vector2 pos, Canvas canvas)
    {
        initialPosition = pos;
        battleCanvas = canvas;
    }
    #endregion

    #region 更新邏輯
    void Update()
    {
        HandleDragging(); // 處理拖曳邏輯
    }
    public Canvas battleCanvas;
    // 處理拖曳邏輯
    private void HandleDragging()
    {
        if (isDragging)
        {
            Vector3 mousePos = Input.mousePosition; // 獲取鼠標位置
            rectTransform.position = (Vector2)mousePos; // 將物件移動到鼠標位置
            SetOuterRingVisible(true); // 开始拖曳时显示 Outer_Ring
        }
        else
        {
            SetOuterRingVisible(false); // 停止拖曳时隐藏 Outer_Ring
        }
    }
    #endregion

    #region 拖曳事件處理
    public void OnPointerDown(PointerEventData eventData)
    {
        StartDragging(); // 開始拖曳
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        StopDragging(); // 停止拖曳
    }

    // 開始拖曳
    private void StartDragging()
    {
        isDragging = true;
    }

    // 停止拖曳
    private void StopDragging()
    {
        isDragging = false;
        CheckIfInsidePanel();
    }

    // 檢查是否在任何面板內
    public void CheckIfInsidePanel()
    {
        bool isInsidePanel = false;

        foreach (RectTransform panel in panels)
        {
            if (RectTransformUtility.RectangleContainsScreenPoint(panel, rectTransform.position, null))
            {
                diceEvent.SendDiceInfo(this.gameObject, this.GetComponent<DiceData>().GetDiceValue(), panel);
                isInsidePanel = true;
                // 成功拖曳次數（原本只在非無盡模式下計數並用來觸發教學對話，
                // 對話系統移除後改為單純計數）
                successDragCount++;
                break;
            }
        }

        if (!isInsidePanel)
        {
            ReturnToInitialPosition();
        }
    }

    #endregion
    #region 公共方法
    // 返回初始位置
    public void ReturnToInitialPosition()
    {
        try
        {
            // rectTransform.anchoredPosition = new Vector3(initialPosition.x, initialPosition.y, battleCanvas.planeDistance);
            rectTransform.anchoredPosition = (Vector2)initialPosition;
            SetOuterRingVisible(false); // 返回初始位置时隐藏 Outer_Ring
        }
        catch (Exception ex)
        {
            UnityEngine.Debug.LogWarning("ReturnToInitialPosition 建立骰子時會有段時間出這個問題 " + ex.Message);
        }
    }

    // 控制 Outer_Ring 的显示与隐藏
    private void SetOuterRingVisible(bool isVisible)
    {
        if (outerRing != null)
        {
            outerRing.SetActive(isVisible);
        }
    }
    #endregion
}

