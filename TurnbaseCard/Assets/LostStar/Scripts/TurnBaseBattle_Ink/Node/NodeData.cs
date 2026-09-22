using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 掛在「節點(Node)」上的資料：這個節點與哪些節點相鄰（供地圖尋路/移動）。
/// 由 EventGrid→NodeEvent、GridData→NodeData 的改名而來。
/// 欄位 connectedGrids 改名為 connectedNodes，用 FormerlySerializedAs 保留舊 prefab/場景已存的相鄰資料。
/// </summary>
public class NodeData : MonoBehaviour
{
    [FormerlySerializedAs("connectedGrids")]
    [Tooltip("相鄰的節點（走一步可到）")]
    public List<Transform> connectedNodes = new List<Transform>();
}
