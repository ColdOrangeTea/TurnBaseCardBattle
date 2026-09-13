namespace Assets.Scripts.StoryNodeMap
{
    /// <summary>
    /// 章節內單一劇情節點的類型。
    /// 決定詳情面板上方顯示的分類文字（可被節點自身的 categoryOverride 覆蓋），
    /// 未來也能依類型改變節點外觀。
    /// </summary>
    public enum StoryNodeType
    {
        MainLine = 0,     // 本章節主線節點
        Branch = 1,       // 分支
        BranchEnding = 2, // 分支結局
        Ending = 3,       // 結局
    }
}
