using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SelectGameMode : MonoBehaviour
{
    [Header("各模式進入的場景名稱（需已加入 Build Settings）")]
    [Tooltip("劇情模式進入的場景")]
    [SerializeField] private string storySceneName = "LevelMapSample";
    [Tooltip("無盡模式進入的場景")]
    [SerializeField] private string endlessSceneName = "EndlessMode";

    private static List<bool> storyMode = new List<bool>()
        {
            true,
            false,
            false
        };

    private static List<bool> multiplayerMode = new List<bool>()
        {
            false,
            true,
            false
        };
    private static List<bool> endlessMode = new List<bool>()
        {
            false,
            true,
            false
        };
    /// <summary>gameModes[0] = IsStoryMode    gameModes[1] = IsMultiplayer   gameModes[2] = IsEndlessMode</summary>
    /// <param name="gameModeIndex"></param>
    /// <returns>是哪個遊戲模式就輸入對應List的數字，那個模式的bool為True </returns>
    public static List<bool> GetGamemodesBools(int gameModeIndex)
    {
        switch (gameModeIndex)
        {
            case 0:
                {
                    return storyMode;
                }
            case 1:
                {
                    return multiplayerMode;
                }
            case 2:
                {
                    return endlessMode;
                }
        }
        Debug.LogError($"gameModeIndex: {gameModeIndex} 目前只有三種遊戲模式 只能輸入0~2");
        return null;

    }

    public static void ResetGameMode()
    {
        GameManager.instance.p_GameModes = new List<bool>()
        {
            false,
            false,
            false
            };
    }
    public void SelectStoryMode()
    {
        if (GameManager.instance != null)
            GameManager.instance.p_GameModes = new List<bool>() { true, false, false };
        LoadModeScene(storySceneName); // 劇情模式 → LevelMapSample
    }

    public void SelectMultiplayerMode()
    {
        if (GameManager.instance != null)
            GameManager.instance.p_GameModes = new List<bool>() { false, true, false };
    }

    public void SelectEndlessMode()
    {
        if (GameManager.instance != null)
            GameManager.instance.p_GameModes = new List<bool>() { false, false, true };
        LoadModeScene(endlessSceneName); // 無盡模式 → EndlessMode
    }

    /// <summary>載入指定模式的場景：優先透過 GameManager（含 Build 檢查），否則直接 LoadScene。</summary>
    private void LoadModeScene(string sceneName)
    {
        if (string.IsNullOrEmpty(sceneName))
        {
            Debug.LogWarning("[SelectGameMode] 未設定該模式的場景名稱。");
            return;
        }
        if (GameManager.instance != null) GameManager.instance.ChangeSceneByName(sceneName);
        else SceneManager.LoadScene(sceneName);
    }
}
