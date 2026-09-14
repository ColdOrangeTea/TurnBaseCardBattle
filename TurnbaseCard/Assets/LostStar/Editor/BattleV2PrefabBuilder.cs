// 此工具由 A_Good_Ink 使用 AI 生成。
using System.Text;
using TurnBaseBattleV2;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 把「V2 戰鬥」烘成一個自包含的 prefab（由 A_Good_Ink 使用 AI 生成）。
///
/// 做什麼：用既有 <see cref="BattleV2SceneGenerator.BuildBattleV2"/> 的接線邏輯，組出一份
/// 「Canvas(根) → BattleEmpty(巢狀 prefab, 含 V2 單位元件) + BattleV2Brain(BattleController 等)」的階層，
/// 因為 BattleV2 所有引用（battleRoot / 單位 View / 骰子抽卡系統 / rootCanvasDirect→自帶 Canvas）都在此階層內，
/// 存成 prefab 後實例化即為完整可跑的常駐戰鬥，不再需要每次用程式重建接線。
///
/// 產出：Assets/LostStar/Prefabs/TurnBaseCardBattle/BattleV2Root.prefab
/// 使用方式：Unity 上方選單 Tools/TurnBaseBattle/生成 V2 戰鬥 Prefab (BattleV2Root Prefab)。
/// 可重複執行：覆蓋更新同路徑 prefab（GUID 不變、引用不會斷）；改了 V2 接線後再跑一次即可。
/// 註：BattleEmpty 以巢狀 prefab 方式收入，之後編輯 BattleEmpty 仍會傳遞進來。
/// </summary>
public static class BattleV2PrefabBuilder
{
    const string PrefabGuid = "";                 // 首次生成後可填入以鎖定；空字串則用預設路徑
    const string DefaultPrefabPath = "Assets/LostStar/Prefabs/TurnBaseCardBattle/BattleV2Root.prefab";

    static string PrefabPath
    {
        get
        {
            if (!string.IsNullOrEmpty(PrefabGuid))
            {
                string p = AssetDatabase.GUIDToAssetPath(PrefabGuid);
                if (!string.IsNullOrEmpty(p)) return p;
            }
            return DefaultPrefabPath;
        }
    }

    [MenuItem("Tools/TurnBaseBattle/生成 V2 戰鬥 Prefab (BattleV2Root Prefab)")]
    public static void Menu()
    {
        string report;
        bool ok = Build(out report);
        EditorUtility.DisplayDialog("BattleV2Root Prefab",
            (ok ? $"已生成：\n{PrefabPath}\n\n接線報告：\n" : "生成失敗，詳見 Console。\n\n") + report, "好");
    }

    public static bool Build(out string report)
    {
        var log = new StringBuilder();
        GameObject canvasGO = null;
        try
        {
            // prefab 的根：自帶一個 ScreenSpaceOverlay Canvas（BattleEmpty 是 UI，需要 Canvas 才能顯示）
            canvasGO = new GameObject("BattleV2Root", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasGO.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 蓋在地圖之上
            var scaler = canvasGO.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);

            BattleController controller;
            GameObject battleUI;
            var brain = BattleV2SceneGenerator.BuildBattleV2(canvasGO, canvas, log, out controller, out battleUI);
            if (brain == null) { report = log.ToString(); return false; }

            brain.name = "BattleV2Brain";                 // 與 prefab 根(BattleV2Root)區分
            brain.transform.SetParent(canvasGO.transform, false); // 收進 prefab 根，讓引用全在階層內

            BattleV2SceneGenerator.EnsureFolder(System.IO.Path.GetDirectoryName(DefaultPrefabPath).Replace('\\', '/'));
            var saved = PrefabUtility.SaveAsPrefabAsset(canvasGO, PrefabPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            log.AppendLine(saved != null ? $"✓ 已存成 prefab：{PrefabPath}" : "✗ SaveAsPrefabAsset 失敗");
            report = log.ToString();
            Debug.Log($"[BattleV2PrefabBuilder] 完成：{PrefabPath}\n{report}");
            return saved != null;
        }
        catch (System.Exception ex)
        {
            report = log.ToString() + "\n例外：" + ex.Message;
            Debug.LogError($"[BattleV2PrefabBuilder] 失敗：{ex}");
            return false;
        }
        finally
        {
            if (canvasGO != null) Object.DestroyImmediate(canvasGO); // 清掉場景中的暫存階層
        }
    }
}
