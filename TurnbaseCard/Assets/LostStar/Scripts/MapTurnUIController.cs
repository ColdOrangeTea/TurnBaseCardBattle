using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Assets.Scripts.GlobalEnums; // MapTurnBaseType

public class MapTurnUIController : MonoBehaviour
{
    public CanvasGroup turnIndicatorCanvasGroup;  // �s����TurnIndicatorUI��CanvasGroup
    public S001_PlayerController playerController;
    public TMP_Text turnIndicatorText;                // �s����TurnIndicatorUI��Text�ե�
    public float fadeDuration = 1.0f;             // �H�J�M�H�X�ɶ�
    public float displayDuration = 3.0f;          // UI��ܪ��`�ɪ�

    // ��ܷ��e�^�X��UI�ĪG
    public void ShowTurnIndicator(MapTurnBaseType turnType)
    {
        playerController.DisablePlayerInputForCheck();
        if (turnType == MapTurnBaseType.PlayerTurn)
        {
            turnIndicatorText.text = "���a�^�X";
        }
        else if (turnType == MapTurnBaseType.EnemyTurn)
        {
            turnIndicatorText.text = "�ĤH�^�X";
        }

        // �}�l�H�J�H�X��{
        StartCoroutine(FadeInAndOut());
    }

    private IEnumerator FadeInAndOut()
    {
        // �H�J�ĪG
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            turnIndicatorCanvasGroup.alpha = Mathf.Lerp(0, 1, elapsedTime / fadeDuration);
            yield return null;
        }

        // �O��UI��� displayDuration ��
        yield return new WaitForSeconds(displayDuration);

        // �H�X�ĪG
        elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            turnIndicatorCanvasGroup.alpha = Mathf.Lerp(1, 0, elapsedTime / fadeDuration);
            yield return null;
        }
    }
}
