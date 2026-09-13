using UnityEngine;
using TMPro;

public class Settlement_MUSIC : MonoBehaviour
{
    public GameObject SettlementEmpty;  // SettlementEmpty物件
    public AudioSource VT;              // VT音效
    public AudioSource LS;              // LS音效
    public TMP_Text Loss;               // Loss文字

    void Update()
    {
        // 判斷SettlementEmpty是否可視並播放對應音效
        if (SettlementEmpty.activeSelf)
        {
            // 如果SettlementEmpty可視且Loss文字也可視，播放LS音效
            if (Loss.gameObject.activeSelf)
            {
                if (!LS.isPlaying)
                {
                    VT.Stop();  // 停止VT音效
                    LS.Play();  // 播放LS音效
                }
            }
            else
            {
                // 如果只有SettlementEmpty可視，播放VT音效
                if (!VT.isPlaying)
                {
                    LS.Stop();  // 停止LS音效
                    VT.Play();  // 播放VT音效
                }
            }
        }
    }
}
