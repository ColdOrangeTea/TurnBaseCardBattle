using System;
using UnityEngine;

/// <summary>
/// 事件派送的共用小工具（此工具由 A_Good_Ink 使用 AI 生成）。
/// 統一各事件類別「有人訂閱才 Invoke、沒人訂閱就記一筆 Log」的重複樣板，
/// 讓每個事件的送出方法縮成一行，行為與原本一致。
/// </summary>
internal static class EventDispatch
{
    /// <summary>送出無參數事件；沒有任何訂閱者時輸出提示 Log。</summary>
    public static void Raise(Action action, string eventName)
    {
        if (action != null) action.Invoke();
        else LogNoSubscriber(eventName);
    }

    /// <summary>送出單一參數事件；沒有任何訂閱者時輸出提示 Log。</summary>
    public static void Raise<T>(Action<T> action, T arg, string eventName)
    {
        if (action != null) action.Invoke(arg);
        else LogNoSubscriber(eventName);
    }

    /// <summary>送出兩個參數事件；沒有任何訂閱者時輸出提示 Log。</summary>
    public static void Raise<T1, T2>(Action<T1, T2> action, T1 arg1, T2 arg2, string eventName)
    {
        if (action != null) action.Invoke(arg1, arg2);
        else LogNoSubscriber(eventName);
    }

    /// <summary>送出三個參數事件；沒有任何訂閱者時輸出提示 Log。</summary>
    public static void Raise<T1, T2, T3>(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3, string eventName)
    {
        if (action != null) action.Invoke(arg1, arg2, arg3);
        else LogNoSubscriber(eventName);
    }

    static void LogNoSubscriber(string eventName)
    {
        Debug.Log($"[事件] {eventName} 目前沒有人訂閱");
    }
}
