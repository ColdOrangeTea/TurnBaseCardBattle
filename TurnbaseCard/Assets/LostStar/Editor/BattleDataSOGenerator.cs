/// <summary>
/// 回合制戰鬥資料 SO 生成工具。
///
/// 做什麼：把原本寫死在程式碼裡的單位數值（玩家/敵人 的 HP、骰子數）與狀態效果設定，
///         一鍵生成為兩個 ScriptableObject 資產，供執行期經 BattleDataProvider 讀取。
/// 產出位置：
///   - Assets/LostStar/Resources/Battle/BattleUnitStats.asset
///   - Assets/LostStar/Resources/Battle/BattleStatusEffectData.asset
///   （放在 Resources 是為了讓非 MonoBehaviour 的資料類別可直接 Resources.Load）
/// 使用方式：Unity 上方選單「Tools/TurnBaseBattle/生成戰鬥資料 SO (Generate Battle Data SO)」。
/// 可重複執行：會覆蓋更新既有產出（就地更新欄位，GUID 不變，引用不會斷）。
///
/// 此工具由 A_Good_Ink 使用 AI 生成。
/// </summary>
using System.Collections.Generic;
using System.IO;
using System.Text;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEditor;
using UnityEngine;

public static class BattleDataSOGenerator
{
    private const string TargetFolder = "Assets/LostStar/Resources/Battle";
    private const string UnitStatsPath = TargetFolder + "/BattleUnitStats.asset";
    private const string StatusEffectPath = TargetFolder + "/BattleStatusEffectData.asset";

    [MenuItem("Tools/TurnBaseBattle/生成戰鬥資料 SO (Generate Battle Data SO)")]
    public static void Generate()
    {
        string report = GenerateSilent();
        EditorUtility.DisplayDialog("戰鬥資料 SO 生成完成", report, "OK");
    }

    /// <summary>不跳對話框的生成入口（供自動化/腳本呼叫），回傳結果摘要。</summary>
    public static string GenerateSilent()
    {
        EnsureFolder(TargetFolder);

        var sb = new StringBuilder();

        var unitStats = LoadOrCreate<SO_BattleUnitStats>(UnitStatsPath, out bool unitCreated);
        PopulateUnitStats(unitStats);
        EditorUtility.SetDirty(unitStats);
        sb.AppendLine($"{(unitCreated ? "[新建]" : "[更新]")} {UnitStatsPath}（玩家 {unitStats.players.Count}、敵人 {unitStats.enemies.Count}）");

        var statusData = LoadOrCreate<SO_BattleStatusEffectData>(StatusEffectPath, out bool statusCreated);
        PopulateStatusEffects(statusData);
        EditorUtility.SetDirty(statusData);
        sb.AppendLine($"{(statusCreated ? "[新建]" : "[更新]")} {StatusEffectPath}（狀態 {statusData.effects.Count}）");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        BattleDataProvider.ReloadAll();

        Debug.Log("[BattleDataSOGenerator] 完成\n" + sb);
        return sb.ToString();
    }

    // ---------------- 單位數值種子值（原 GetInfo 內容） ----------------
    private static void PopulateUnitStats(SO_BattleUnitStats so)
    {
        so.players = new List<SO_BattleUnitStats.PlayerStatEntry>
        {
            Player(CharacterType.Seraphis, 16, 2),
        };

        so.enemies = new List<SO_BattleUnitStats.EnemyStatEntry>
        {
            Enemy(EnemyType.Yarn, 19, 1),
            Enemy(EnemyType.Swordsman, 33, 1),
            Enemy(EnemyType.Boy, 28, 1),
            Enemy(EnemyType.Nun, 25, 1),
            Enemy(EnemyType.Preacher, 26, 1),
            Enemy(EnemyType.Godness, 54, 1),
            Enemy(EnemyType.PotatoAlpha, 20, 2),
            Enemy(EnemyType.PotatoBeta, 25, 1),
            Enemy(EnemyType.PotatoGamma, 28, 1),
            Enemy(EnemyType.PotatoDelta, 35, 1),
            Enemy(EnemyType.PotatoDigamma, 19, 1),
            Enemy(EnemyType.PotatoOmega, 20, 1),
            Enemy(EnemyType.PotatoKappa, 26, 1),
            Enemy(EnemyType.PotatoVex, 25, 1),
            Enemy(EnemyType.PotatoPowder, 28, 1),
        };
    }

    private static SO_BattleUnitStats.PlayerStatEntry Player(CharacterType t, int hp, int dice)
        => new SO_BattleUnitStats.PlayerStatEntry { characterType = t, maxHp = hp, diceCount = dice };

    private static SO_BattleUnitStats.EnemyStatEntry Enemy(EnemyType t, int hp, int dice)
        => new SO_BattleUnitStats.EnemyStatEntry { enemyType = t, maxHp = hp, diceCount = dice };

    // ---------------- 狀態效果種子值（原 SendEffectInfo 內容） ----------------
    private static void PopulateStatusEffects(SO_BattleStatusEffectData so)
    {
        so.effects = new List<SO_BattleStatusEffectData.StatusEffectEntry>
        {
            // Burnt：回合開始減 1 顆骰子，作用於對手，自己回合生效，回合結束移除
            new SO_BattleStatusEffectData.StatusEffectEntry
            {
                effectType = BattleStatusEffectType.Burnt,
                effectValue = BattleStatusEffect.BurntReduceDiceCountValue, lastTurn = BattleStatusEffect.BurntTurnLastValue, addTimes = 1,
                isAbleToRemoveAfterEffect = false, isRemovable = true,
                isSelfAffecting = false, isRivalAffecting = true,
                isSkippedTurn = false, affectedCalculation = false, operationType = BattleValueOperation.NormalOperation, affectedDice = true,
                affectDamage = false, affectHeal = false, affectReceivedDamage = false, affectReceivedHeal = false,
                activateImmediately = false, activateAtTurnStart = true, activateAtTurnEnd = false,
                removeImmediately = false, removeAtTurnStart = false, removeAtTurnEnd = true,
                isTurnBasedEffect = true, isEqual = false, isGreaterThan = false, isLessThan = true,
                activeOwnTurn = true, activeRivalTurn = false, activeAnyTurn = false,
            },
            // Poisoned：回合開始造成傷害，作用於對手，任何回合生效，回合開始移除，傷害/回合可累加
            new SO_BattleStatusEffectData.StatusEffectEntry
            {
                effectType = BattleStatusEffectType.Poisoned,
                effectValue = BattleStatusEffect.PoisonedDamageValue, lastTurn = BattleStatusEffect.PoisonedTurnLastValue, addTimes = 1,
                isAbleToRemoveAfterEffect = true, isRemovable = true,
                isSelfAffecting = false, isRivalAffecting = true,
                isSkippedTurn = false, affectedCalculation = false, operationType = BattleValueOperation.NormalOperation, affectedDice = false,
                affectDamage = false, affectHeal = false, affectReceivedDamage = false, affectReceivedHeal = false,
                activateImmediately = false, activateAtTurnStart = true, activateAtTurnEnd = false,
                removeImmediately = false, removeAtTurnStart = true, removeAtTurnEnd = false,
                isTurnBasedEffect = true, isEqual = false, isGreaterThan = false, isLessThan = true,
                activeOwnTurn = false, activeRivalTurn = false, activeAnyTurn = true,
            },
            // Dizziness：造成傷害並暈眩對方，跳過回合，任何回合生效，回合結束移除
            new SO_BattleStatusEffectData.StatusEffectEntry
            {
                effectType = BattleStatusEffectType.Dizziness,
                effectValue = BattleStatusEffect.DizzinessDamageValue, lastTurn = BattleStatusEffect.DizzinessTurnLastValue, addTimes = 1,
                isAbleToRemoveAfterEffect = false, isRemovable = true,
                isSelfAffecting = false, isRivalAffecting = true,
                isSkippedTurn = true, affectedCalculation = false, operationType = BattleValueOperation.NormalOperation, affectedDice = false,
                affectDamage = false, affectHeal = false, affectReceivedDamage = false, affectReceivedHeal = false,
                activateImmediately = false, activateAtTurnStart = true, activateAtTurnEnd = false,
                removeImmediately = false, removeAtTurnStart = false, removeAtTurnEnd = true,
                isTurnBasedEffect = true, isEqual = false, isGreaterThan = false, isLessThan = true,
                activeOwnTurn = false, activeRivalTurn = false, activeAnyTurn = true,
            },
            // HolyProtect：下回合受到的傷害 /2，作用於自己，敵人回合生效，回合結束移除
            new SO_BattleStatusEffectData.StatusEffectEntry
            {
                effectType = BattleStatusEffectType.HolyProtect,
                effectValue = BattleStatusEffect.HolyProtectDamageDivideValue, lastTurn = BattleStatusEffect.HolyProtectTurnLastValue, addTimes = 1,
                isAbleToRemoveAfterEffect = true, isRemovable = true,
                isSelfAffecting = true, isRivalAffecting = false,
                isSkippedTurn = false, affectedCalculation = true, operationType = BattleValueOperation.Divide, affectedDice = false,
                affectDamage = false, affectHeal = false, affectReceivedDamage = true, affectReceivedHeal = false,
                activateImmediately = false, activateAtTurnStart = false, activateAtTurnEnd = false,
                removeImmediately = false, removeAtTurnStart = false, removeAtTurnEnd = true,
                isTurnBasedEffect = true, isEqual = false, isGreaterThan = false, isLessThan = true,
                activeOwnTurn = false, activeRivalTurn = true, activeAnyTurn = false,
            },
            // StarThreaten：影響骰子（使玩家只能骰出特定值），作用於對手，自己回合生效，回合結束移除
            new SO_BattleStatusEffectData.StatusEffectEntry
            {
                effectType = BattleStatusEffectType.StarThreaten,
                effectValue = BattleStatusEffect.StarThreatenValue, lastTurn = BattleStatusEffect.StarThreatenTurnLastValue, addTimes = 1,
                isAbleToRemoveAfterEffect = false, isRemovable = true,
                isSelfAffecting = false, isRivalAffecting = true,
                isSkippedTurn = false, affectedCalculation = false, operationType = BattleValueOperation.NormalOperation, affectedDice = true,
                affectDamage = false, affectHeal = false, affectReceivedDamage = false, affectReceivedHeal = false,
                activateImmediately = false, activateAtTurnStart = true, activateAtTurnEnd = false,
                removeImmediately = false, removeAtTurnStart = false, removeAtTurnEnd = true,
                isTurnBasedEffect = true, isEqual = false, isGreaterThan = false, isLessThan = true,
                activeOwnTurn = true, activeRivalTurn = false, activeAnyTurn = false,
            },
            // Oath：下回合受到的傷害 x2，作用於自己，敵人回合生效，回合結束移除
            new SO_BattleStatusEffectData.StatusEffectEntry
            {
                effectType = BattleStatusEffectType.Oath,
                effectValue = BattleStatusEffect.OathDamageMultipleValue, lastTurn = BattleStatusEffect.OathTurnLastValue, addTimes = 1,
                isAbleToRemoveAfterEffect = true, isRemovable = true,
                isSelfAffecting = true, isRivalAffecting = false,
                isSkippedTurn = false, affectedCalculation = true, operationType = BattleValueOperation.Multiply, affectedDice = false,
                affectDamage = false, affectHeal = false, affectReceivedDamage = true, affectReceivedHeal = false,
                activateImmediately = false, activateAtTurnStart = false, activateAtTurnEnd = false,
                removeImmediately = false, removeAtTurnStart = false, removeAtTurnEnd = true,
                isTurnBasedEffect = true, isEqual = false, isGreaterThan = false, isLessThan = true,
                activeOwnTurn = false, activeRivalTurn = true, activeAnyTurn = false,
            },
        };
    }

    // ---------------- 輔助 ----------------
    private static T LoadOrCreate<T>(string path, out bool created) where T : ScriptableObject
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            created = true;
        }
        else
        {
            created = false;
        }
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
