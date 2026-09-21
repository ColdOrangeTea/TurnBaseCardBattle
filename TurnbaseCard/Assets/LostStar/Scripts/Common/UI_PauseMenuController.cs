using UnityEngine;
using Assets.Scripts.GlobalEnums;

public class UI_PauseMenuController : MonoBehaviour
{
    #region 宣告
    public Canvas PMCanvas; // 暫停選單的 Canvas
    public GameObject MenuPanel; // 暫停選單主面板(UI_SetUpBackgroundPanel)：預設關閉，靠本腳本開/關
    public GameObject SittingsPanel; // 宣告 SittingsPanel 作為一個 GameObject
    public AudioSource OpenMenu; // 用於播放開啟暫停選單的音效
    public AudioSource CloseMenu; // 用於播放關閉暫停選單的音效
    private bool hasPlayedSound = false; // 用來確保音效只播放一次
    private bool hasClosedSoundPlayed = false; // 確保關閉音效只播放一次
    #endregion

    #region 暫停選單隱藏(Start)
    void Start()
    {
        // 在遊戲開始時隱藏 Canvas、主面板 和 SittingsPanel
        PMCanvas.enabled = false;

        if (MenuPanel != null)
        {
            MenuPanel.SetActive(false); // 主面板預設關閉，靠開啟選單時再啟用
        }

        if (SittingsPanel != null)
        {
            SittingsPanel.SetActive(false); // 確保 Panel 初始為隱藏狀態
        }
    }
    #endregion

    #region ESC鍵觸發暫停選單
    void Update()
    {
        // 每當按下 ESC 鍵後顯示或隱藏 Canvas
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleCanvas();
        }
    }

    // 用來切換 PMCanvas 顯示狀態的通用方法
    public void ToggleCanvas()
    {
        PMCanvas.enabled = !PMCanvas.enabled;

        // 主面板跟著 Canvas 一起開/關（面板預設是關閉的，這裡負責開起來）
        if (MenuPanel != null) MenuPanel.SetActive(PMCanvas.enabled);

        // 如果 Canvas 被啟用，則暫停時間；否則恢復時間
        Time.timeScale = PMCanvas.enabled ? 0 : 1;

        // 播放開啟或關閉音效
        if (PMCanvas.enabled && !hasPlayedSound && OpenMenu != null)
        {
            PlaySfx(OpenMenu); // 播放開啟音效
            hasPlayedSound = true;
            hasClosedSoundPlayed = false;
        }
        else if (!PMCanvas.enabled && !hasClosedSoundPlayed && CloseMenu != null)
        {
            PlaySfx(CloseMenu); // 播放關閉音效
            hasClosedSoundPlayed = true;
            hasPlayedSound = false;
        }
    }

    public void OpenCanvas()
    {
        // 直接啟用 PMCanvas、主面板 並暫停遊戲時間
        PMCanvas.enabled = true;
        if (MenuPanel != null) MenuPanel.SetActive(true);
        Time.timeScale = 0;

        // 播放開啟音效
        if (!hasPlayedSound && OpenMenu != null)
        {
            PlaySfx(OpenMenu);
            hasPlayedSound = true;
            hasClosedSoundPlayed = false;
        }
    }

    public void CloseCanvas()
    {
        // 關閉 Canvas、主面板 並恢復時間
        PMCanvas.enabled = false;
        if (MenuPanel != null) MenuPanel.SetActive(false);
        Time.timeScale = 1;

        // 播放關閉音效
        if (!hasClosedSoundPlayed && CloseMenu != null)
        {
            PlaySfx(CloseMenu);
            hasClosedSoundPlayed = true;
        }

        hasPlayedSound = false; // 重置開啟音效播放標記
    }

    public void ClosePanels()
    {
        // 關閉 SittingsPanel 並恢復時間
        if (SittingsPanel != null)
        {
            SittingsPanel.SetActive(false); // 使用 SetActive(false) 隱藏
        }
        Time.timeScale = 1;

        // 播放關閉音效
        if (!hasClosedSoundPlayed && CloseMenu != null)
        {
            PlaySfx(CloseMenu);
            hasClosedSoundPlayed = true;
        }

        hasPlayedSound = false; // 重置開啟音效播放標記
    }

    public void ShowSittingsPanel()
    {
        // 顯示 SittingsPanel
        if (SittingsPanel != null)
        {
            SittingsPanel.SetActive(true); // 使用 SetActive(true) 顯示
        }
    }

    // 選單開/關音效一律交給 AudioDirector 播（吃全域 SFX 音量），不再用本地 AudioSource 直接播。
    // OpenMenu/CloseMenu 保留為 AudioSource，只當作 clip 來源。
    private void PlaySfx(AudioSource src)
    {
        if (src == null || src.clip == null) return;
        if (AudioDirector.Instance != null) AudioDirector.Instance.PlaySFX(src.clip);
        else Debug.LogWarning("[UI_PauseMenuController] 場上沒有 AudioDirector，選單音效未播放。");
    }

    // public void Back_to_Homepage()
    // {
    //     GameManager.ExecuteIfIsSpecifyMode(() => // 是無盡模式的話就觸發
    //     {

    //         SelectGameMode.ResetGameMode();
    //         GameManager.instance.ChangeSceneByName(MapType.HomePage.ToString());
    //     }, 2);

    //     // 返回主頁面並重置時間
    //     GameManager.instance.ChangeSceneByIndex(0);
    //     Time.timeScale = 1;
    // }
    #endregion
}
