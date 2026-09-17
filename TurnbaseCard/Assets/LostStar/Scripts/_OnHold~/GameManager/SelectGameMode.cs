using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SelectGameMode : MonoBehaviour
{
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
        GameManager.instance.p_GameModes = new List<bool>()
        {
            true,
            false,
            false
            };
    }

    public void SelectMultiplayerMode()
    {
        GameManager.instance.p_GameModes = new List<bool>()
        {
            false,
            true,
            false
        };
    }

    public void SelectEndlessMode()
    {
        GameManager.instance.p_GameModes = new List<bool>()
        {
            false,
            false,
            true
        };
    }
}
