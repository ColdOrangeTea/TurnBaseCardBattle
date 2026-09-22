using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// 節點連線視覺（由 A_Good_Ink 使用 AI 生成）。掛在 Stage 根物件上，
/// 依「各節點自己編寫的 <see cref="NodeData.connectedNodes"/>」（誰連誰＝玩家可走路徑）
/// 產生 <see cref="LineRenderer"/> 實體線當視覺——不再用距離自動判斷相鄰。
///
/// 節點清單優先取自同物件的 <see cref="StageInfo.Nodes"/>；沒有 StageInfo 時退回抓子物件所有 NodeData。
/// connectedNodes 由開發者在各節點的 Inspector 自行編寫，本元件只「讀取並畫線」，不會改動它。
///
/// 用法：編好各節點的 connectedNodes 後，在此元件右鍵選「重建連線視覺 (依已編寫的相鄰)」；
/// 或勾 <see cref="rebuildOnAwake"/> 讓進遊戲時自動依當前相鄰重畫。重建為冪等：每次先清空舊線再重畫。
/// </summary>
public class NodeLinkRenderer : MonoBehaviour
{
    [Header("線視覺")]
    public float lineWidth = 0.08f;
    public Color lineColor = new Color(1f, 1f, 1f, 0.6f);
    [Tooltip("線材質；留空自動用 Sprites/Default")]
    public Material lineMaterial;

    [Header("執行期")]
    [Tooltip("進遊戲(Awake)時自動重畫一次；否則只靠編輯器右鍵重畫、烘進 prefab")]
    public bool rebuildOnAwake = false;

    const string LinesContainerName = "AutoLinks";

    private void Awake()
    {
        if (rebuildOnAwake) Rebuild();
    }

    [ContextMenu("重建連線視覺 (依已編寫的相鄰)")]
    public void RebuildFromMenu()
    {
        Rebuild();
#if UNITY_EDITOR
        UnityEditor.EditorUtility.SetDirty(this);
        if (!Application.isPlaying)
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(gameObject.scene);
#endif
    }

    /// <summary>依各節點已編寫的 connectedNodes 重畫線（去重雙向、不改動 connectedNodes）。</summary>
    public void Rebuild()
    {
        var nodes = CollectNodes();

        // 由各節點的相鄰蒐集「無向邊」，以節點對去重（A-B 與 B-A 只畫一條）
        var seen = new HashSet<long>();
        var edges = new List<(Transform a, Transform b)>();
        foreach (var nd in nodes)
        {
            if (nd == null || nd.connectedNodes == null) continue;
            foreach (var c in nd.connectedNodes)
            {
                if (c == null) continue;
                int ia = nd.transform.GetInstanceID(), ib = c.GetInstanceID();
                long key = ia < ib ? ((long)ia << 32) ^ (uint)ib : ((long)ib << 32) ^ (uint)ia;
                if (seen.Add(key)) edges.Add((nd.transform, c));
            }
        }

        ClearLines();
        BuildLines(edges);

        BattleLog.Log($"[NodeLinkRenderer] {name}：{nodes.Count} 個節點 → 依編寫相鄰畫出 {edges.Count} 條線。");
    }

    /// <summary>節點來源：優先 StageInfo.Nodes，否則子物件所有 NodeData。</summary>
    private List<NodeData> CollectNodes()
    {
        var si = GetComponent<StageInfo>();
        if (si != null && si.Nodes != null && si.Nodes.Count > 0)
            return si.Nodes.Where(n => n != null).ToList();
        return GetComponentsInChildren<NodeData>(true).ToList();
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

    private void BuildLines(List<(Transform a, Transform b)> edges)
    {
        if (edges.Count == 0) return;
        var container = GetLinesContainer(true);
        var mat = lineMaterial != null ? lineMaterial : new Material(Shader.Find("Sprites/Default"));

        foreach (var (a, b) in edges)
        {
            var go = new GameObject($"Link_{a.name}_{b.name}");
            go.transform.SetParent(container, false);
            var lr = go.AddComponent<LineRenderer>();
            // 用區域座標（相對 stage 根）而非世界座標：否則烘進 prefab 的是絕對世界座標，
            // 放進場景後同一 prefab 的每個實例都會把線畫在烘製當下的位置（原點附近）而不跟著實例走。
            lr.useWorldSpace = false;
            lr.positionCount = 2;
            lr.SetPosition(0, transform.InverseTransformPoint(a.position));
            lr.SetPosition(1, transform.InverseTransformPoint(b.position));
            lr.startWidth = lr.endWidth = lineWidth;
            lr.numCapVertices = 2;
            lr.material = mat;
            lr.startColor = lr.endColor = lineColor;
            lr.sortingOrder = -1; // 壓在節點 icon 之下
        }
    }
}
