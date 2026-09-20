using System.Collections;
using UnityEngine;

/// <summary>
/// 商店音訊掛件：所有商店聲音都集中在這裡、統一走 <see cref="AudioDirector"/>，ShopSystem 本身不再持有任何音源。
///   - 進店前：播進店音效並把 BGM 切成商店 BGM（PushBGM，記住地圖 BGM 位置）。
///   - 離店後：還原地圖 BGM（PopBGM）。
///   - 購買成功／失敗：訂閱 ShopSystem 的事件，用 AudioDirector.PlaySFX 播對應音效。
///
/// 用法：掛到場上任一 GameObject（通常與其他 MapFlowHook 同物件），指定各 AudioClip。
/// 由 A_Good_Ink 使用 AI 生成。
/// </summary>
public class ShopAudioHook : MapFlowHookBase
{
    [Header("商店 BGM / 進店音效")]
    [Tooltip("商店背景音樂")]
    [SerializeField] private AudioClip storeBGM;
    [Tooltip("進入商店的音效")]
    [SerializeField] private AudioClip toStoreSFX;

    [Header("購買音效")]
    [Tooltip("購買成功音效")]
    [SerializeField] private AudioClip buySFX;
    [Tooltip("購買失敗（金幣不足）音效")]
    [SerializeField] private AudioClip buyFailedSFX;

    [Tooltip("商店系統（留空會自動尋找）")]
    [SerializeField] private ShopSystem shop;

    private void Awake()
    {
        if (shop == null) shop = FindAnyObjectByType<ShopSystem>();
        if (shop != null)
        {
            shop.ItemPurchased += OnItemPurchased;
            shop.PurchaseFailed += OnPurchaseFailed;
        }
    }

    private void OnDestroy()
    {
        if (shop != null)
        {
            shop.ItemPurchased -= OnItemPurchased;
            shop.PurchaseFailed -= OnPurchaseFailed;
        }
    }

    private void OnItemPurchased(ShopSystem.ShopItem item) => PlaySfx(buySFX);
    private void OnPurchaseFailed() => PlaySfx(buyFailedSFX);

    private static void PlaySfx(AudioClip clip)
    {
        if (clip != null && AudioDirector.Instance != null) AudioDirector.Instance.PlaySFX(clip);
    }

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
