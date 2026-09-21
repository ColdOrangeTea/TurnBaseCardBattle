using TMPro; // 改tmp_Text 需要用到這個命名空間
using UnityEngine;

public class Temp_VFX002_ButtonTextChangeColor : MonoBehaviour // 掛在TMP文字上 (不知為何滑鼠射線的方式掃不到UI 寫失敗了)
{
    private TMP_Text tmp_Text;
    public Color originColor;
    public Color changeColor;
    void Start()
    {
        tmp_Text = this.GetComponent<TMP_Text>();
        originColor = tmp_Text.color;
    }

    void Update()
    {
        // Hover();
    }

    public void ChangeColor()
    {
        tmp_Text.color = changeColor;
        Debug.Log("變色");
    }
    public void OriginColor()
    {
        tmp_Text.color = originColor;
        Debug.Log("變回原色");
    }

    // void OnDrawGizmos()
    // {
    //     Vector3 s = new(0, 0, 1);
    //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //     Vector3 vector3 = ray.origin + s;
    //     Gizmos.DrawRay(ray.origin, vector3);
    // }

    // void Hover()
    // {
    //     Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
    //     int buttonLayer = LayerMask.GetMask("UI_Button");

    //     Debug.Log("ray:" + ray);
    //     if (Physics.Raycast(ray, out RaycastHit hitinfo, 500, buttonLayer))
    //     {
    //         tmp_Text.color = changeColor;
    //         Debug.Log("變色");
    //     }
    //     else
    //     {
    //         tmp_Text.color = originColor;
    //         Debug.Log("不便");
    //     }
    // }
}
