using System;
using UnityEngine;

/// <summary>
/// 骰子相關的事件收發：卡片＋骰數資料、骰子從卡片移除、骰子拖放資訊等。
/// 送出的樣板統一交給 <see cref="EventDispatch"/> 處理。
/// </summary>
public class DiceEvent
{
    /// <summary>遞送卡片與骰子點數的資料。</summary>
    public static Action<CardData, int> OnInfoOfCardAndDicesSent;
    public void SendInfoOfCardAndDices(CardData card, int value)
        => EventDispatch.Raise(OnInfoOfCardAndDicesSent, card, value, nameof(OnInfoOfCardAndDicesSent));

    /// <summary>把某顆骰子從卡片上移除。</summary>
    public static Action<GameObject> OnDiceRemoved;
    public void RemoveDiceOnCard(GameObject dice)
        => EventDispatch.Raise(OnDiceRemoved, dice, nameof(OnDiceRemoved));

    /// <summary>遞送骰子物件、點數與所在面板。</summary>
    public static Action<GameObject, int, RectTransform> OnDiceInfoSent;
    public void SendDiceInfo(GameObject dice, int diceValue, RectTransform panel)
        => EventDispatch.Raise(OnDiceInfoSent, dice, diceValue, panel, nameof(OnDiceInfoSent));
}
