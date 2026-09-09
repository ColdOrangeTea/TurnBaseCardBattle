namespace Assets.Scripts.GlobalEnums
{
    /// <summary>
    /// 與存檔狀態相關的列舉 (enum) 資料。
    /// </summary>
    public enum SaveFileStates
    {
        Empty = 0,               // 預設空白狀態 / 刪檔後的狀態 → 可操作：Create
        NewGame = 1,             // 創建資料但未進入遊戲（用不到再刪）→ 可操作：Load、Delete
        OperationInProgress = 2, // 存檔創建中 / 覆蓋中 / 自動儲存中，過渡用的狀態；一般不會在玩家存檔選單操作時出現，用在程式編寫時需要寫分支的地方 → 可操作：None
        Loaded = 3,              // 已有存檔 → 可操作：Load、Delete、Copy
        Clear = 4,               // 已通關(過)的存檔 → 可操作：Load、Delete、Copy
        Corrupted = 5,           // 類似空洞騎士一命鋼魂模式，掛掉後存檔不能讀取的狀態（用不到再刪）→ 可操作：Delete
        Undefined = 6            // 未定義明確的情況
    }
}
