using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Assets.Scripts.GlobalEnums; // MapTurnBaseType

public class MapTurnUIController : MonoBehaviour
{
    public CanvasGroup turnIndicatorCanvasGroup;  // 綁定到 TurnIndicatorUI 的 CanvasGroup
    public S001_PlayerController playerController;
    public TMP_Text turnIndicatorText;                // 綁定到 TurnIndicatorUI 的 Text 元件
    public float fadeDuration = 1.0f;             // 淡入與淡出的時間
    public float displayDuration = 3.0f;          // UI 顯示的持續時間

    // 顯示當前回合的 UI 效果
    public void ShowTurnIndicator(MapTurnBaseType turnType)
    {
        playerController.DisablePlayerInputForCheck();
        if (turnType == MapTurnBaseType.PlayerTurn)
        {
            turnIndicatorText.text = "玩家回合";
        }
        else if (turnType == MapTurnBaseType.EnemyTurn)
        {
            turnIndicatorText.text = "敵人回合";
        }

        // 開始淡入淡出流程
        StartCoroutine(FadeInAndOut());
    }

    private IEnumerator FadeInAndOut()
    {
        // 淡入效果
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            turnIndicatorCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
            yield return null;
        }

        // 保持 UI 顯示 displayDuration 秒
        yield return new WaitForSeconds(displayDuration);

        // 淡出效果
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            turnIndicatorCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            yield return null;
        }
    }
}
