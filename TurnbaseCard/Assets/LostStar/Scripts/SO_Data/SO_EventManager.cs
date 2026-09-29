using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "EventManager", menuName = "Event/Create Event Manager")]
public class SO_EventManager : ScriptableObject
{
    public List<SO_Event> events; // 儲存事件的列表

    public SO_Event GetRandomEvent()
    {
        if (events.Count == 0) return null;
        int randomIndex = Random.Range(0, events.Count);
        return events[randomIndex]; // 隨機返回一個事件
    }
}