using UnityEngine.EventSystems;
using UnityEngine;

/// <summary>
/// 繼承 Unity EventSystem 的 UI 互動管理器，
/// 以單例形式跨場景追蹤當前選取的 UI 物件。
/// </summary>
public class InteractionOfUI : EventSystem
{
    private static InteractionOfUI instance;

    [SerializeField]
    private GameObject clickTarget; // 當前選取的 UI 物件

    protected override void Awake()
    {
        base.Awake();
        SetupSingleton();
    }

    protected override void Update()
    {
        base.Update();
        TrackSelectedTarget();
    }

    /// <summary>
    /// 將 EventSystem 的選取目標設回快取的 clickTarget（供外部或後續功能使用）。
    /// </summary>
    public void ApplyClickTarget()
    {
        SetSelectedGameObject(clickTarget);
    }

    /// <summary>
    /// 追蹤 EventSystem 當前選取的 UI 物件並快取。
    /// </summary>
    private void TrackSelectedTarget()
    {
        if (currentSelectedGameObject == null) return;        // 沒有選取任何 UI 物件
        if (clickTarget == currentSelectedGameObject) return; // 點擊目標未改變

        clickTarget = currentSelectedGameObject;
        Debug.Log("當前選取的 UI 物件: " + clickTarget);
    }

    private void SetupSingleton()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }
}
