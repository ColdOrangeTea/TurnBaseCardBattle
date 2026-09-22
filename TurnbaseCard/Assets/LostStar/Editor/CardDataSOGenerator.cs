/// <summary>
/// 卡片資料 SO 生成工具。
///
/// 做什麼：把原本寫死在 BattleCard.BuildDefaultCardInfo() 的每張卡行為資料，一鍵匯出成每卡一個
///         SO_CardData 資產，並彙整成一張 SO_CardDataTable 供 BattleDataProvider / BattleCard 讀取。
///         使用音效(useSfx)於「新建」時自動從對應的舊卡片 prefab（Cards/<CardType>.prefab 的 CardData.Use_SFX）帶入。
/// 產出位置：
///   - Assets/LostStar/Resources/SO_Battle/Cards/Card_<CardType>.asset（每卡一個）
///   - Assets/LostStar/Resources/SO_Battle/BattleCards.asset（總表）
/// 使用方式：Unity 上方選單「Tools/TurnBaseBattle/生成卡片資料 SO (Generate Card Data SO)」。
/// 可重複執行：既有的 SO 資產「不覆蓋」（尊重你在 Inspector 上的編修，含音效/敘述/數值）；只補齊缺少的卡，
///         並重建總表 BattleCards（總表只是引用清單，重建安全，GUID 不變）。若要用程式碼預設值重寫既有卡，
///         勾選視窗中的「覆寫既有卡資料」。
///
/// 此工具由 A_Good_Ink 使用 AI 生成。
/// </summary>
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEditor;
using UnityEngine;

public static class CardDataSOGenerator
{
    private const string TargetFolder = "Assets/LostStar/Resources/SO_Battle";
    private const string CardsFolder = TargetFolder + "/Cards";
    private const string TablePath = TargetFolder + "/BattleCards.asset";
    private const string CardPrefabFolder = "Assets/LostStar/Prefabs/TurnBaseCardBattle/Cards";

    [MenuItem("Tools/TurnBaseBattle/生成卡片資料 SO (Generate Card Data SO)")]
    public static void Generate()
    {
        string report = Run(overwriteExisting: false);
        EditorUtility.DisplayDialog("卡片資料 SO 生成完成", report, "OK");
    }

    /// <summary>不跳對話框的生成入口（供自動化/腳本呼叫），回傳結果摘要。</summary>
    public static string GenerateSilent(bool overwriteExisting = false) => Run(overwriteExisting);

    [MenuItem("Tools/TurnBaseBattle/生成卡片資料 SO：覆寫既有卡資料")]
    public static void GenerateOverwrite()
    {
        if (EditorUtility.DisplayDialog("覆寫既有卡資料？",
            "會用程式碼預設值(BuildDefaultCardInfo)重寫既有 SO_CardData 的行為欄位，並重新從 prefab 帶入音效。\n" +
            "你在 Inspector 手動改過的卡資料會被蓋掉。確定要覆寫嗎？", "覆寫", "取消"))
        {
            string report = Run(overwriteExisting: true);
            EditorUtility.DisplayDialog("卡片資料 SO 生成完成", report, "OK");
        }
    }

    private static string Run(bool overwriteExisting)
    {
        EnsureFolder(CardsFolder);

        var sb = new StringBuilder();
        var tableCards = new List<SO_CardData>();
        int created = 0, updated = 0, kept = 0;

        foreach (CardType type in Enum.GetValues(typeof(CardType)))
        {
            if (type == CardType.Undefined) continue;

            string path = $"{CardsFolder}/Card_{type}.asset";
            var so = LoadOrCreate<SO_CardData>(path, out bool isNew);

            if (isNew)
            {
                PopulateBehavior(so, type);
                so.useSfx = LoadCardSfx(type);
                EditorUtility.SetDirty(so);
                created++;
                sb.AppendLine($"[新建] Card_{type}（音效：{(so.useSfx != null ? so.useSfx.name : "無")}）");
            }
            else if (overwriteExisting)
            {
                PopulateBehavior(so, type);
                so.useSfx = LoadCardSfx(type);
                EditorUtility.SetDirty(so);
                updated++;
                sb.AppendLine($"[覆寫] Card_{type}");
            }
            else
            {
                kept++;
            }

            tableCards.Add(so);
        }

        // 重建總表（引用清單，安全）
        var table = LoadOrCreate<SO_CardDataTable>(TablePath, out bool tableNew);
        table.cards = tableCards;
        EditorUtility.SetDirty(table);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BattleDataProvider.ReloadAll();

        sb.Insert(0, $"新建 {created}、覆寫 {updated}、保留 {kept}；總表 {(tableNew ? "新建" : "更新")} {table.cards.Count} 張。\n\n");
        Debug.Log("[CardDataSOGenerator] 完成\n" + sb);
        return sb.ToString();
    }

    private static void PopulateBehavior(SO_CardData so, CardType type)
    {
        var info = BattleCard.BuildDefaultCardInfo(type);
        so.cardType = type;
        so.trueValues = info.TrueValues != null ? new List<int>(info.TrueValues) : new List<int>() { 0 };
        so.requiredAccumulatedDiceValue = info.RequiredAccumulatedDiceValue;
        so.accu_DiceValue = info.Accu_DiceValue;
        so.requireDesignatedDiceValue = info.RequireDesignatedDiceValue;
        so.desi_DiceValue = info.Desi_DiceValue;
        so.isEqualTo = info.IsEqualTo;
        so.isGreaterThan = info.IsGreaterThan;
        so.isLessThan = info.IsLessThan;
        so.requiredOddDiceValue = info.RequiredOddDiceValue;
        so.requiredEvenDiceValue = info.RequiredEvenDiceValue;
        so.isAddEffectStatus = info.IsAddEffectStatus;
        so.effectType = info.EffectType;
        so.isUsedToAttack = info.IsUsedToAttack;
        so.isFunctional = info.IsFunctional;
        so.functionalType = info.FunctionalType;
    }

    /// <summary>從對應的舊卡片 prefab（Cards/&lt;CardType&gt;.prefab）的 CardData.Use_SFX 帶入使用音效；沒有就回 null。</summary>
    private static AudioClip LoadCardSfx(CardType type)
    {
        string prefabPath = $"{CardPrefabFolder}/{type}.prefab";
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null) return null;
        var cd = prefab.GetComponentInChildren<CardData>(true);
        if (cd != null && cd.Use_SFX != null) return cd.Use_SFX.clip;
        return null;
    }

    private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
        }
        else created = false;
        return asset;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace('\\', '/');
        string leaf = Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, leaf);
    }
}
