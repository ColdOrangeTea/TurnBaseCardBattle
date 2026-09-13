using System;
using Assets.Scripts.GlobalEnums;

/// <summary>
/// 地圖回合制的事件收發：切換回合、回送目前回合。
/// 送出的樣板統一交給 <see cref="EventDispatch"/> 處理。
/// </summary>
public class MapTurnBaseEvent
{
    /// <summary>切換到指定回合，speed 為切換時的移動速度。</summary>
    public static event Action<MapTurnBaseType, float> OnTurnChanged;
    public void ChangeTurn(MapTurnBaseType nextTurn, float speed)
        => EventDispatch.Raise(OnTurnChanged, nextTurn, speed, nameof(OnTurnChanged));

    /// <summary>回送管理器目前的回合。</summary>
    public static event Action<MapTurnBaseType> OnTurnInfoSent;
    public void SendManagerTurn(MapTurnBaseType curTurn)
        => EventDispatch.Raise(OnTurnInfoSent, curTurn, nameof(OnTurnInfoSent));
}
