using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 掛在「節點(Node)」上的資料：這個節點與哪些節點相鄰（供地圖尋路/移動）。
/// 由 EventGrid→NodeEvent、GridData→NodeData 的改名而來；欄位名沿用 connectedGrids 以保留序列化。
/// </summary>
public class NodeData : MonoBehaviour
{
    [Tooltip("相鄰的節點（走一步可到）")]
    public List<Transform> connectedGrids = new List<Transform>(); // 相鄰節點
}
