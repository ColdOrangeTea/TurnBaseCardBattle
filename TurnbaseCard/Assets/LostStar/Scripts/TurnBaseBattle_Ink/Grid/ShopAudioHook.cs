using System.Collections;
using UnityEngine;

/// <summary>
/// 商店音訊掛件：踏到商店、真正開店前先播進店音效並把 BGM 切成商店 BGM；離開商店後還原地圖 BGM。
/// 透過 <see cref="AudioDirector"/> 的單一 BGM 頻道＋堆疊來做，確保地圖 BGM 與商店 BGM 不會疊在一起。
///
/// 用法：把本元件掛到場上任一 GameObject（通常與其他 MapFlowHook 同物件），
/// 指定 <see cref="storeBGM"/> / <see cref="toStoreSFX"/>；<see cref="MapFlowController"/> 會自動抓到並在對應時機呼叫。
/// 由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class ShopAudioHook : MapFlowHookBase
{
    [Tooltip("商店背景音樂")]
    [SerializeField] private AudioClip storeBGM;
    [Tooltip("進入商店的音效")]
    [SerializeField] private AudioClip toStoreSFX;

    public override IEnumerator OnBeforeEvent(GridEventType type, EventGrid grid)
    {
        if (type != GridEventType.Shop) yield break;
        var audio = AudioDirector.Instance;
        if (audio != null)
        {
            if (toStoreSFX != null) audio.PlaySFX(toStoreSFX);
            if (storeBGM != null) audio.PushBGM(storeBGM);   // 切到商店 BGM（記住地圖 BGM 位置）
        }
    }

    public override IEnumerator OnAfterEvent(GridEventType type, EventGrid grid)
    {
        if (type != GridEventType.Shop) yield break;
        var audio = AudioDirector.Instance;
        if (audio != null) audio.PopBGM();               // 還原地圖 BGM
    }
}
