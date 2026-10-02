using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TEST_Canva : MonoBehaviour
{
    public GameObject repairCanvas;

    public void OpenCanvas()
    {
        repairCanvas .SetActive(true);
    }

    public void CloseCanvas()
    {
        repairCanvas.SetActive(false);
    }
}
