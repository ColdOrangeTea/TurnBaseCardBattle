using System;
using UnityEngine;
using UnityEngine.SceneManagement;
using Assets.Scripts.GlobalEnums;

/// <summary>
/// 遊戲總管（精簡重構版，由 A_Good_Ink 使用 AI 重構）。
///
/// 保留的功能：
///   1. 單例 + 跨場景常駐（DontDestroyOnLoad）。
///   2. 編輯器旗標 <see cref="IsInEditor"/>（開發中作弊/除錯用；同步到靜態 <see cref="InEditorState"/>）。
///   3. 場景切換（<see cref="ChangeSceneByName"/> / <see cref="ChangeSceneByIndex"/> / <see cref="IsSceneInBuild"/>）。
///   4. 目前場景名稱與對應地圖區域（<see cref="CurrentSceneName"/> / <see cref="CurrentMapArea"/>）；
///      場景載入時若有 <see cref="UI_SceneSwitcher"/> 先播轉場。
///
/// 已移除（原本依賴 V2 重構已移除／擱置的系統，待其回歸再接回）：
///   - DialogueManager / TurnBaseBattleManager 管理器初始化（ManagerMono&lt;&gt;）。
///   - CheatManager 作弊系統（仍在 _OnHold~）。
///   - SelectGameMode 遊戲模式（故事／多人／無盡）判定（仍在 _OnHold~）。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;

    [Header("編輯器內作弊/除錯旗標，勾了才啟用相關功能（手動勾選）")]
    public bool IsInEditor = true;
    public static bool InEditorState;

    [Header("目前場景資訊（唯讀顯示）")]
    public string CurrentSceneName = "";
    public MapType CurrentMapArea;

    #region Unity 生命週期

    void Awake()
    {
        GameManagerSingleton();
    }

    void Update()
    {
        if (InEditorState != IsInEditor) InEditorState = IsInEditor;
    }

    void OnDestroy()
    {
        if (instance == this) SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    #endregion

    #region 單例

    void GameManagerSingleton()
    {
        if (instance == null)
        {
            instance = this;
            SceneManager.sceneLoaded += OnSceneLoaded; // 只綁一次
            DontDestroyOnLoad(gameObject);             // 跨場景常駐
        }
        else if (instance != this)
        {
            gameObject.SetActive(false);
            Destroy(gameObject);
        }
    }

    #endregion

    #region 場景切換與追蹤

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 有轉場控制器就先播轉場（由它接手後續流程）；否則更新目前場景資訊。
        UI_SceneSwitcher sceneSwitcher = FindAnyObjectByType<UI_SceneSwitcher>();
        if (sceneSwitcher != null)
        {
            sceneSwitcher.PlayTPTrans();
            return;
        }
        LogCurrentSceneInfo();
    }

    /// <summary>以場景名稱切換場景（先確認存在於 Build Settings）。</summary>
    public void ChangeSceneByName(string sceneName)
    {
        if (!IsSceneInBuild(sceneName)) return;
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>以 Build Index 切換場景。</summary>
    public void ChangeSceneByIndex(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>檢查場景是否存在於 Build Settings；不存在時輸出中文錯誤並回傳 false。</summary>
    public bool IsSceneInBuild(string sceneName)
    {
        for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
        {
            string path = SceneUtility.GetScenePathByBuildIndex(i);
            string name = System.IO.Path.GetFileNameWithoutExtension(path);
            if (name == sceneName) return true;
        }
        Debug.LogError($"[GameManager] Scene 不存在於 Build Settings 中: {sceneName}");
        return false;
    }

    /// <summary>取得並記錄目前場景名稱與對應的地圖區域（<see cref="MapType"/>）。</summary>
    private void LogCurrentSceneInfo()
    {
        CurrentSceneName = SceneManager.GetActiveScene().name;

        foreach (MapType map in Enum.GetValues(typeof(MapType)))
        {
            if (CurrentSceneName == map.ToString())
            {
                CurrentMapArea = map;
                return;
            }
        }
        CurrentMapArea = MapType.Undefined;
    }

    #endregion
}
