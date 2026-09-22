using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 自動連接關卡節點（由 A_Good_Ink 使用 AI 生成）。取代手排 Wire 物件：
/// 掛在 Stage 根物件上，蒐集底下所有具 <see cref="NodeData"/> 的可移動節點，
/// 依「距離閘值」判斷相鄰、雙向寫入 <see cref="NodeData.connectedNodes"/>（供 BFS 尋路），
/// 並同時產生 <see cref="LineRenderer"/> 實體線當視覺（取代 Wire）。
///
/// 用法：排好節點後，在此元件右鍵選「重建連線 (連接＋產生線)」；或勾 <see cref="rebuildOnAwake"/> 讓進遊戲時自動重建。
/// 重建為冪等：每次先清空既有 connectedNodes 與舊線，再依目前節點位置重算。
/// </summary>
public class NodeAutoLinker : MonoBehaviour
{
    [Header("連接規則")]
    [Tooltip("兩節點距離 ≤ 此值就相連")]
    public float linkRadius = 3f;
    [Tooltip("每個節點最多幾條連線（0 = 不限）；超過時保留較近的")]
    public int maxLinksPerNode = 0;
    [Tooltip("只看 XY 平面距離（正面 2D 地圖用；關掉則用 3D 距離）")]
    public bool useXYOnly = true;

    [Header("線視覺")]
    [Tooltip("是否產生 LineRenderer 實體線")]
    public bool generateLines = true;
    public float lineWidth = 0.08f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    [Tooltip("線材質；留空自動用 Sprites/Default")]
    public Material lineMaterial;

    [Header("執行期")]
    [Tooltip("進遊戲(Awake)時自動重建一次；否則只靠編輯器右鍵重建、烘進 prefab")]
    public bool rebuildOnAwake = false;

    const string LinesContainerName = "AutoLinks";

    private void Awake()
    {
        if (rebuildOnAwake) Rebuild();
    }

    [ContextMenu("重建連線 (連接＋產生線)")]
    public void RebuildFromMenu()
    {
        Rebuild();
#if UNITY_EDITOR
        // 讓編輯器把 connectedNodes 與新產生的線持久化
        foreach (var nd in GetComponentsInChildren<NodeData>(true)) UnityEditor.EditorUtility.SetDirty(nd);
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    /// <summary>依目前節點位置重算相鄰與線。</summary>
    public void Rebuild()
    {
        var nodes = GetComponentsInChildren<NodeData>(true).ToList();

        // 冪等：先清空所有節點的相鄰
        foreach (var nd in nodes)
        {
            if (nd.connectedNodes == null) nd.connectedNodes = new List<Transform>();
            nd.connectedNodes.Clear();
        }

        // 蒐集半徑內的候選邊，依距離由近到遠
        var edges = new List<(float dist, NodeData a, NodeData b)>();
        float radiusSqr = linkRadius * linkRadius;
        for (int i = 0; i < nodes.Count; i++)
            for (int j = i + 1; j < nodes.Count; j++)
            {
                float d2 = SqrDist(nodes[i].transform.position, nodes[j].transform.position);
                if (d2 <= radiusSqr) edges.Add((d2, nodes[i], nodes[j]));
            }
        edges.Sort((x, y) => x.dist.CompareTo(y.dist));

        // 貪婪加邊，遵守每節點上限
        var degree = new Dictionary<NodeData, int>();
        foreach (var nd in nodes) degree[nd] = 0;
        var accepted = new List<(NodeData a, NodeData b)>();
        foreach (var e in edges)
        {
            if (maxLinksPerNode > 0 && (degree[e.a] >= maxLinksPerNode || degree[e.b] >= maxLinksPerNode))
                continue;
            e.a.connectedNodes.Add(e.b.transform);
            e.b.connectedNodes.Add(e.a.transform);
            degree[e.a]++; degree[e.b]++;
            accepted.Add((e.a, e.b));
        }

        // 線視覺
        ClearLines();
        if (generateLines) BuildLines(accepted);

        BattleLog.Log($"[NodeAutoLinker] {name}：{nodes.Count} 個節點、半徑 {linkRadius} → {accepted.Count} 條連線。");
    }

    private float SqrDist(Vector3 a, Vector3 b)
    {
        if (useXYOnly) { float dx = a.x - b.x, dy = a.y - b.y; return dx * dx + dy * dy; }
        return (a - b).sqrMagnitude;
    }

    private Transform GetLinesContainer(bool createIfMissing)
    {
        var t = transform.Find(LinesContainerName);
        if (t == null && createIfMissing)
        {
            var go = new GameObject(LinesContainerName);
            t = go.transform;
            t.SetParent(transform, false);
        }
        return t;
    }

    private void ClearLines()
    {
        var container = GetLinesContainer(false);
        if (container == null) return;
        // 由後往前刪，編輯器/執行期皆可
        for (int i = container.childCount - 1; i >= 0; i--)
        {
            var child = container.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(child);
            else DestroyImmediate(child);
        }
    }

    private void BuildLines(List<(NodeData a, NodeData b)> accepted)
    {
        if (accepted.Count == 0) return;
        var container = GetLinesContainer(true);
        var mat = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));

        foreach (var (a, b) in accepted)
        {
            var go = new GameObject($"Link_{a.name}_{b.name}");
            go.transform.SetParent(container, false);
            var lr = go.AddComponent<LineRenderer>();
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            lr.SetPosition(0, a.transform.position);
            lr.SetPosition(1, b.transform.position);
            lr.startWidth = lr.endWidth = lineWidth;
            lr.numCapVertices = 2;
            lr.material = mat;
            lr.startColor = lr.endColor = lineColor;
            lr.sortingOrder = -1; // 壓在節點 icon 之下
        }
    }
}
