namespace Assets.Scripts.GlobalEnums
{
    public enum MainMenuState
    {
        LOGO = 0,
        MainMenu = 1, // HomePage
        SaveMenu = 2,
        GameSettingMenu = 3,
        KeyBoard_InputMenu = 4, // 滑鼠操作應該也算在這邊?
        GamePad_InputMenu = 5,
        InputConfirmPrompt = 6, // 跳出確認KeyBind的提示
        QuitGameMenu = 7,// 從標題畫面離開遊戲
        PauseMenu = 8,
        BackToTitlePrompt = 9, // 跳出QuitGame的提示
    }
}
