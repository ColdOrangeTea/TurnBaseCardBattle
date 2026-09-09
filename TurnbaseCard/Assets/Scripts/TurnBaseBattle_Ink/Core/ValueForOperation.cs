using System.Collections.Generic;

/// <summary>
/// 一次用卡／狀態觸發運算過程中，攜帶所有暫存數值的資料結構。
/// 由 <see cref="BattleAction"/> 在計算流程中傳遞與更新。
/// </summary>
public struct ValueForOperation
{
    public string UserName;
    public string TargetName;

    public int BaseValue_ToRival;
    public int BaseValue_ToUser;

    public int FinalValue_ToRival;
    public int FinalValue_ToUser;

    public int EffectValue;
    public int LastTurn;
    public int AddTimes;
    public bool IsUserSkippedTurn;
    public bool IsTargetSkippedTurn;
    public int UserDiceValue;
    public int UserDiceCount;
    public int TargetDiceCount;
    public List<BattleStatusEffect> UserStatus;
    public List<BattleStatusEffect> TargetStatus;

    public bool UserNeedsOperation;
    public bool TargetNeedsOperation;

    public bool IsAssignFixedDiceValue_CardUser;
    public bool IsAssignFixedDiceValue_Target;

    public int AssignFixedDiceValue;
    public int AssignFixedDiceCount;


    public ValueForOperation(string userName, string targetName, int baseValue_ToRival, int baseValue_ToUser, int finalValue_ToRival, int finalValue_ToUser,
    int effectValue, int lastTurn, int addTimes, bool isUserSkippedTurn, bool isTargetSkippedTurn,
    int userDiceValue, int userDiceCount, int targetDiceCount,
    List<BattleStatusEffect> userStatus, List<BattleStatusEffect> targetStatus,
    bool userNeedsOperation, bool targetNeedsOperation,
    bool isAssignFixedDiceValue_CardUser, bool isAssignFixedDiceValue_Target, int assignFixedDiceValue, int assignFixedDiceCount)
    {
        UserName = userName;
        TargetName = targetName;

        BaseValue_ToRival = baseValue_ToRival;
        BaseValue_ToUser = baseValue_ToUser;

        FinalValue_ToRival = finalValue_ToRival;
        FinalValue_ToUser = finalValue_ToUser;

        EffectValue = effectValue;
        LastTurn = lastTurn;
        AddTimes = addTimes;

        IsUserSkippedTurn = isUserSkippedTurn;
        IsTargetSkippedTurn = isTargetSkippedTurn;

        UserDiceValue = userDiceValue;

        UserDiceCount = userDiceCount;
        TargetDiceCount = targetDiceCount;

        UserStatus = userStatus;
        TargetStatus = targetStatus;

        UserNeedsOperation = userNeedsOperation;
        TargetNeedsOperation = targetNeedsOperation;

        IsAssignFixedDiceValue_CardUser = isAssignFixedDiceValue_CardUser;
        IsAssignFixedDiceValue_Target = isAssignFixedDiceValue_Target;
        AssignFixedDiceValue = assignFixedDiceValue;
        AssignFixedDiceCount = assignFixedDiceCount;

    }
}
