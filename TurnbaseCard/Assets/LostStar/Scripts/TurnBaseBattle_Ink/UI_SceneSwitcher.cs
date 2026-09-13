using System.Collections;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class UI_SceneSwitcher : MonoBehaviour
{
    [Tooltip("Index of the scene to load, as listed in Build Settings.")]
    [SerializeField] private int sceneIndex = 0;

    [Tooltip("Delay before switching to the target scene, in seconds.")]
    [SerializeField] private float delayInSeconds = 3f;

    private void Start()
    {
        // 原本會先確認場上沒有 GameManager 才轉場；GameManager 已於移植時移除，
        // 依原註解「沒有 GM 的話，也可以執行轉場」的用意，這裡直接執行轉場。
        PlayTPTrans();
    }
    public void PlayTPTrans()
    {
        if (IsSceneIndexValid(sceneIndex))
        {
            StartCoroutine(SwitchSceneAfterDelay());
        }
        else
        {
            UnityEngine.Debug.LogError($"Scene index {sceneIndex} is invalid. Check your Build Settings.");
        }
    }

    private IEnumerator SwitchSceneAfterDelay()
    {
        yield return new WaitForSeconds(delayInSeconds);
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// Validates if the given scene index is within the Build Settings scene range.
    /// </summary>
    private bool IsSceneIndexValid(int index)
    {
        return index >= 0 && index < SceneManager.sceneCountInBuildSettings;
    }
}

