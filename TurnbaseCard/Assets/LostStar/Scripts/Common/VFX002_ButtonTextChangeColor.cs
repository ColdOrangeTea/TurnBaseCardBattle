using TMPro;
using UnityEngine;

public class VFX002_ButtonTextChangeColor : MonoBehaviour
{
    private TMP_Text tmp_Text;
    public Color originColor;
    public Color changeColor;
    void Start()
    {
        tmp_Text = this.GetComponent<TMP_Text>();
        originColor = tmp_Text.color;
    }

    public void ChangeColor()
    {
        tmp_Text.color = changeColor;
    }
    public void OriginColor()
    {
        tmp_Text.color = originColor;
    }
}
