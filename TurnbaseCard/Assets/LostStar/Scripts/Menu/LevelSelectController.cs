using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 選地圖畫面的流程控制（由 A_Good_Ink 使用 AI 生成）。
///
/// 流程：在選地圖場景放一顆常駐的 <see cref="LevelMapInitializer"/>（設定本關要帶入的血量/金錢/道具/戰鬥先攻），
/// 玩家按下地圖按鈕 → 呼叫 <see cref="LoadMap"/> 載入地圖場景；LevelMapInitializer 因 DontDestroyOnLoad
/// 會一起帶進地圖，地圖裡的系統(ShopSystem/PlayerMapStatus_UI/開戰流程)再各自向它取值。
///
/// 目前只有一張地圖場景可測，三顆按鈕都接同一個 LoadMap；日後要分關（不同初值或不同地圖場景）再擴充。
/// 場景名稱以序列化欄位指定，不寫死；載入前會檢查是否在 Build Settings 內。
/// </summary>
public class LevelSelectController : MonoBehaviour
{
    [Tooltip("要載入的地圖場景名稱（需已加入 Build Settings）。")]
    [SerializeField] private string mapSceneName = "LevelMapSample";

    /// <summary>載入地圖場景（接到地圖按鈕的 onClick）。</summary>
    public void LoadMap()
    {
        if (string.IsNullOrEmpty(mapSceneName))
        {
            Debug.LogError("[LevelSelectController] 未設定地圖場景名稱(mapSceneName)。");
            return;
        }
        if (!Application.CanStreamedLevelBeLoaded(mapSceneName))
        {
            Debug.LogError($"[LevelSelectController] 場景 '{mapSceneName}' 不在 Build Settings 內或名稱錯誤，無法載入。");
            return;
        }
        SceneManager.LoadScene(mapSceneName);
    }
}
