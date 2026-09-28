using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFunction : MonoBehaviour
{
    [SerializeField]
    private bool isOpenNewGamePanel;
    public GameObject HomePageOptions;
    public GameObject NewGamePanel;
    void Start()
    {
        NewGamePanel.SetActive(isOpenNewGamePanel = false);

    }
    public void OpenNewGamePanel()
    {
        if (isOpenNewGamePanel)
        {
            NewGamePanel.SetActive(isOpenNewGamePanel = false);
            HomePageOptions.GetComponent<CanvasGroup>().alpha = 1f;
        }
        else
        {
            NewGamePanel.SetActive(isOpenNewGamePanel = true);
            HomePageOptions.GetComponent<CanvasGroup>().alpha = 0f;
        }

    }
}
