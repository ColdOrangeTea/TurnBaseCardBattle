namespace Assets.Scripts.Dialogue
{
    /// <summary>
    /// 對話單位（說話者）的類型。
    /// </summary>
    public enum DialogueUnitType
    {
        Aster,
        Narration, // 旁白
        Unknown,   // 未知說話者
        Undefined_Temp_ThisIsTypeEndNumber = 99, // 型別結尾編號（暫定）
    }
}
