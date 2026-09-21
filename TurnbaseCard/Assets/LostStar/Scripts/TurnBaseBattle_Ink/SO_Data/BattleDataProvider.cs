// 此腳本由 A_Good_Ink 使用 AI 生成。
// 用途：回合制戰鬥數值/狀態設定的統一存取點。從 Resources/SO_Battle 載入 SO 資產並快取，
//       讓非 MonoBehaviour 的純資料類別（TurnBaseBattleEnemyData / TurnBaseBattlePlayerData /
//       BattleStatusEffect）也能取得資料，且不受初始化順序影響。
// 註：資產放在 Resources 是為了讓上述純類別/靜態流程可直接載入，不必在 Inspector 逐一掛引用。
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;

/// <summary>回合制戰鬥資料（單位數值、狀態效果設定）的載入與快取入口。</summary>
public static class BattleDataProvider
{
    // Resources 相對路徑（不含副檔名）
    public const string UnitStatsResourcePath = "SO_Battle/BattleUnitStats";
    public const string StatusEffectResourcePath = "SO_Battle/BattleStatusEffectData";

    static SO_BattleUnitStats _unitStats;
    static SO_BattleStatusEffectData _statusEffects;

    /// <summary>單位數值資料表；缺少資產時會輸出中文警告並回傳 null。</summary>
    public static SO_BattleUnitStats UnitStats
    {
        get
        {
            if (_unitStats == null)
            {
                _unitStats = Resources.Load<SO_BattleUnitStats>(UnitStatsResourcePath);
                if (_unitStats == null)
                    Debug.LogWarning($"[BattleDataProvider] 找不到單位數值資產：Resources/{UnitStatsResourcePath}。請執行選單「Tools/TurnBaseBattle/生成戰鬥資料 SO」生成。");
            }
            return _unitStats;
        }
    }

    /// <summary>狀態效果設定資料表；缺少資產時會輸出中文警告並回傳 null。</summary>
    public static SO_BattleStatusEffectData StatusEffects
    {
        get
        {
            if (_statusEffects == null)
            {
                _statusEffects = Resources.Load<SO_BattleStatusEffectData>(StatusEffectResourcePath);
                if (_statusEffects == null)
                    Debug.LogWarning($"[BattleDataProvider] 找不到狀態效果資產：Resources/{StatusEffectResourcePath}。請執行選單「Tools/TurnBaseBattle/生成戰鬥資料 SO」生成。");
            }
            return _statusEffects;
        }
    }

    /// <summary>清除快取，下次存取會重新載入（Editor 生成資產後可呼叫）。</summary>
    public static void ReloadAll()
    {
        _unitStats = null;
        _statusEffects = null;
    }

    /// <summary>取得玩家角色數值；找不到回傳 false。</summary>
    public static bool TryGetPlayerStat(CharacterType type, out int maxHp, out int diceCount)
    {
        maxHp = 0;
        diceCount = 0;
        var so = UnitStats;
        if (so != null && so.TryGetPlayer(type, out var entry))
        {
            maxHp = entry.maxHp;
            diceCount = entry.diceCount;
            return true;
        }
        Debug.LogWarning($"[BattleDataProvider] 找不到玩家角色 {type} 的數值設定。");
        return false;
    }

    /// <summary>取得敵人數值；找不到回傳 false。</summary>
    public static bool TryGetEnemyStat(EnemyType type, out int maxHp, out int diceCount)
    {
        maxHp = 0;
        diceCount = 0;
        var so = UnitStats;
        if (so != null && so.TryGetEnemy(type, out var entry))
        {
            maxHp = entry.maxHp;
            diceCount = entry.diceCount;
            return true;
        }
        Debug.LogWarning($"[BattleDataProvider] 找不到敵人 {type} 的數值設定。");
        return false;
    }

    /// <summary>取得狀態效果設定（轉為 BattleEffectInfo）；找不到回傳 false。</summary>
    public static bool TryGetStatusInfo(BattleStatusEffectType type, out BattleEffectInfo info)
    {
        var so = StatusEffects;
        if (so != null && so.TryGet(type, out var entry))
        {
            info = entry.ToBattleEffectInfo();
            return true;
        }
        info = default;
        return false;
    }
}
