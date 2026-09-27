using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MenuFunction : MonoBehaviour
{
    [SerializeField]
    private bool isOpenNewGamePanel;
    public GameObject Logo;
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
            Logo.GetComponent<CanvasGroup>().alpha = 1;
            HomePageOptions.GetComponent<CanvasGroup>().alpha = 1f;
        }
        else
        {
            NewGamePanel.SetActive(isOpenNewGamePanel = true);
            Logo.GetComponent<CanvasGroup>().alpha = 0f;
            HomePageOptions.GetComponent<CanvasGroup>().alpha = 0f;
        }

    }
}
