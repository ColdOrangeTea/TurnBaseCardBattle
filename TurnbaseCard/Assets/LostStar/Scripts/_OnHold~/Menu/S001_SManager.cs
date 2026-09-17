
using System.Collections;
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums;
using UnityEngine;
using UnityEngine.SceneManagement;

using static System.Net.Mime.MediaTypeNames;
public class S001_SManager : MonoBehaviour

{
    #region 宣告物件
    public GameObject player;
    public int Scene;
    #endregion

    public const string GO_STORY_MODE_SCENE = "GoStoryMode";
    public const string GO_MULTIPLAYER_MODE_SCENE = "GoMultiplayerMode";
    public const string GO_ENDLESS_MODE_SCENE = "GoEndlessMode";


    #region UI.開始遊戲進入選單頁面　Homepage To Loading

    public void Play() // 按鈕掛這個方法
    {
        Debug.Log("傳送");

        if (GameManager.instance != null)
        {
            if (GameManager.instance.p_GameModes[0] == true && GameManager.instance.p_GameModes[1] == false &&
             GameManager.instance.p_GameModes[2] == false) // 故事模式
            {
                Invoke(GO_STORY_MODE_SCENE, 1f);
                return;
            }
            if (GameManager.instance.p_GameModes[0] == false && GameManager.instance.p_GameModes[1] == true &&
             GameManager.instance.p_GameModes[2] == false) // 同機對戰模式
            {
                Invoke(GO_MULTIPLAYER_MODE_SCENE, 1f);
                return;
            }
            if (GameManager.instance.p_GameModes[0] == false && GameManager.instance.p_GameModes[1] == false &&
             GameManager.instance.p_GameModes[2] == true) // 無盡模式
            {
                Invoke(GO_ENDLESS_MODE_SCENE, 1f);
                return;
            }
        }

    }

    void GoStoryMode()
    {
        Debug.Log("故事模式");
        GameManager.instance.ChangeSceneByName(MapType.HomePage_ForStoryModeTransition.ToString()); // 進入主選單頁面

    }
    void GoMultiplayerMode()
    {
        Debug.Log("同機對戰模式");
    }
    void GoEndlessMode()
    {
        string sceneName = MapType.EndlessModeRoom.ToString();
        Debug.Log($"{sceneName} 天塔模式");
        // GameManager.instance.ChangeSceneByName(MapType.HomePage_ForEndlessmodeTransition.ToString()); // 先不要過度

        GameManager.instance.ChangeSceneByName(sceneName); //進入頁面
        // GameManager.instance.ChangeSceneByIndex(9);



    }

    public void PlayafterDelay()
    {
        GameManager.instance.ChangeSceneByName(MapType.HomePage_ForStoryModeTransition.ToString());
        // GameManager.instance.ChangeSceneByIndex(3);//進入主選單頁面
        // SceneManager.LoadScene(3);
    }
    #endregion
    #region UI.按下按鈕結束遊戲
    public void Quit()
    {
        //離開遊戲
        //下行程式碼僅供測試時使用，輸出時需拔除
        //Application.Quit();
        //EditorApplication.isPlaying = false;

        UnityEngine.Application.Quit();

    }
    #endregion

    #region UI.回到大廳選單
    public void HomePage()
    {
        GameManager.instance.ChangeSceneByName(MapType.HomePage.ToString());//進入主選單頁面
        // GameManager.instance.ChangeSceneByIndex(0);//進入主選單頁面
    }
    #endregion

    #region UI.加入Discord測試群
    public void OpenDiscordInviteLink()
    {
        //進入主選單頁面
        UnityEngine.Application.OpenURL("https://discord.gg/W5y7egnGQK");
    }
    #endregion

    #region L0 To L9
    public void ToLoading()
    {
        SceneManager.LoadScene(Scene);
    }
    #endregion

    #region L9 To L0
    void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.tag == "Player")
        {
            SceneManager.LoadScene(5);
            //    IsToLX = true;
            //}
            //if (IsToLX)
            //{
            //    SceneManager.LoadScene(Scene);
            //    IsToLX = false;
            //} 
        }
    }
    #endregion
    #region 重回關卡
    public void Return()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }


    public void ToSpaceShip()
    {
        SceneManager.LoadScene(17);
    }
    #endregion
}
