/// <summary>
/// 商店的新手教學觸發鈕。行為全部在 <see cref="GuideButton"/>（點「?」→ 開教學面板、只播一次）。
/// 改接統一的 Guide 系統，已脫離舊對話系統（DialogueManager/DialogueOpenClose）。
/// 保留此類別名與 GUID 以相容既有 prefab 綁定；指定商店的教學面板即可。
/// </summary>
public class GuideButton_Shop : GuideButton
{
}
