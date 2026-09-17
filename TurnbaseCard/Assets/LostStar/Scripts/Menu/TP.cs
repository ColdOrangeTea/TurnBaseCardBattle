using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement; // 添加命名空间
using DG.Tweening; // 引入DOTween命名空间

public class TP : MonoBehaviour
{
    public CanvasGroup transitionPanel; // 用于控制Transition透明度的CanvasGroup

    public void ToL1()
    {
        SceneManager.LoadScene(3);
    }

    public void ToEndless()
    {
        SceneManager.LoadScene(16);
    }

    public void ToCustomScene()
    {
        StartCoroutine(TransitionToScene(17));
    }

    private IEnumerator TransitionToScene(int sceneIndex)
    {
        // 将TransitionPanel设为可见
        transitionPanel.gameObject.SetActive(true);

        // 确保Transition的初始状态为透明
        transitionPanel.alpha = 0;

        // 恢复游戏时间为正常速度
        Time.timeScale = 1;

        // 使用DOTween渐变到全黑
        yield return transitionPanel.DOFade(1f, 1f).WaitForCompletion(); // 1秒内完全变为黑色

        // 场景加载
        SceneManager.LoadScene(sceneIndex);
    }
}

