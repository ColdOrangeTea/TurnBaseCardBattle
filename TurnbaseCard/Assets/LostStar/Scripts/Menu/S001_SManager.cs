using Assets.Scripts.GlobalEnums;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 選單/場景切換管理（去耦重構版，由 A_Good_Ink 使用 AI 生成）。
///
/// 掛在選單面板上，按鈕 OnClick 綁到此處的方法（Play / HomePage / Return / Quit / ToLoading …）。
/// 原本切場景走已移除的 GameManager.instance.ChangeSceneByName / p_GameModes；
/// 這裡改為直接用 <see cref="SceneManager"/> 載入，遊戲模式改為本地 <see cref="gameMode"/> 欄位，
/// 不再依賴已擱置的 GameManager（保留類別名與方法名，prefab 上的按鈕綁定不受影響）。
/// </summary>
public class S001_SManager : MonoBehaviour
{
    [Header("引用（沿用舊 prefab 欄位）")]
    public GameObject player;
    [Tooltip("ToLoading 要載入的場景編號")]
    public int Scene;

    public enum GameMode { Story = 0, Multiplayer = 1, Endless = 2 }

    [Header("遊戲模式（原本由 GameManager 提供，改為本地設定）")]
    [SerializeField] private GameMode gameMode = GameMode.Story;

    private const string GO_STORY_MODE_SCENE = "GoStoryMode";
    private const string GO_MULTIPLAYER_MODE_SCENE = "GoMultiplayerMode";
    private const string GO_ENDLESS_MODE_SCENE = "GoEndlessMode";

    #region 開始遊戲 / 進入各模式
    /// <summary>開始遊戲：依目前遊戲模式，延遲 1 秒後進入對應場景。</summary>
    public void Play()
    {
        switch (gameMode)
        {
            case GameMode.Story: Invoke(GO_STORY_MODE_SCENE, 1f); break;
            case GameMode.Multiplayer: Invoke(GO_MULTIPLAYER_MODE_SCENE, 1f); break;
            case GameMode.Endless: Invoke(GO_ENDLESS_MODE_SCENE, 1f); break;
        }
    }

    private void GoStoryMode() => LoadSceneByName(MapType.HomePage_ForStoryModeTransition.ToString());
    private void GoMultiplayerMode() => Debug.Log("多人對戰模式（尚未實作）");
    private void GoEndlessMode() => LoadSceneByName(MapType.EndlessModeRoom.ToString());

    public void PlayafterDelay() => LoadSceneByName(MapType.HomePage_ForStoryModeTransition.ToString());
    #endregion

    #region 選單按鈕
    public void Quit() => Application.Quit();

    public void HomePage() => LoadSceneByName(MapType.HomePage.ToString());

    public void OpenDiscordInviteLink() => Application.OpenURL("https://discord.gg/W5y7egnGQK");

    /// <summary>載入 Inspector 指定編號的場景。</summary>
    public void ToLoading() => SceneManager.LoadScene(Scene);

    /// <summary>重新載入目前場景（回到關卡）。</summary>
    public void Return() => SceneManager.LoadScene(SceneManager.GetActiveScene().name);

    public void ToSpaceShip() => SceneManager.LoadScene(17);
    #endregion

    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("Player"))
            SceneManager.LoadScene(5);
    }

    // 切場景統一入口（取代舊 GameManager.ChangeSceneByName）
    private static void LoadSceneByName(string sceneName)
    {
        if (Application.CanStreamedLevelBeLoaded(sceneName))
            SceneManager.LoadScene(sceneName);
        else
            Debug.LogWarning($"[S001_SManager] 場景「{sceneName}」不在 Build Settings，無法載入。");
    }
}
