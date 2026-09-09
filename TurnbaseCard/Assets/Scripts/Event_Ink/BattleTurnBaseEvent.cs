using System;
using Assets.Scripts.GlobalEnums.BattleEnum;

/// <summary>
/// 回合制戰鬥的事件收發：負責把「戰鬥設定、行動順序、單位資料」等訊息，
/// 從送出端廣播給訂閱端（例如 TurnBaseBattleManager）。
/// 送出的樣板統一交給 <see cref="EventDispatch"/> 處理。
/// </summary>
public class BattleTurnBaseEvent
{
    /// <summary>傳遞戰鬥的設定至 TurnBaseBattleManager。</summary>
    public static Action<SetBattleSetting> OnBattleSettingSent;
    public void SendBattleSetting(SetBattleSetting battleSetting)
        => EventDispatch.Raise(OnBattleSettingSent, battleSetting, nameof(OnBattleSettingSent));

    /// <summary>傳遞戰鬥時行動的順序。unitTag：使用者的 Tag。</summary>
    public static Action<TurnBaseBattleOrderType, string> OnTurnOrderSent;
    public void SendTurnOrder(TurnBaseBattleOrderType order, string unitTag)
        => EventDispatch.Raise(OnTurnOrderSent, order, unitTag, nameof(OnTurnOrderSent));

    /// <summary>準備取得資料，觸發後由訂閱端回送 <see cref="OnUnitsInfoSent"/>。</summary>
    public static Action OnReadyForInfo;
    public void GetReadyForInfo()
        => EventDispatch.Raise(OnReadyForInfo, nameof(OnReadyForInfo));

    /// <summary>傳遞這回合行動者的資料。</summary>
    public static Action<TurnBaseBattleUnitDisplayData> OnUnitsInfoSent;
    public void SendUnitInfo(TurnBaseBattleUnitDisplayData unit)
        => EventDispatch.Raise(OnUnitsInfoSent, unit, nameof(OnUnitsInfoSent));

    /// <summary>由管理器把行動者(player1)、對象(player2)與當前順序一起傳回單位端。</summary>
    public static Action<TurnBaseBattleUnitDisplayData, TurnBaseBattleUnitDisplayData, TurnBaseBattleOrderType> OnUnitsInfoFromManagerSent;
    public void SendUnitsInfoFromManager(TurnBaseBattleUnitDisplayData player1, TurnBaseBattleUnitDisplayData player2, TurnBaseBattleOrderType order)
        => EventDispatch.Raise(OnUnitsInfoFromManagerSent, player1, player2, order, nameof(OnUnitsInfoFromManagerSent));
}
