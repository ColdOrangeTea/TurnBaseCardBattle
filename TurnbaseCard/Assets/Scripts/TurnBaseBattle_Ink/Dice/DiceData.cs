using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceData : MonoBehaviour
{
    [SerializeField] private DiceData data;
    [SerializeField] private int DiceValue = 0;
    [SerializeField] private Sprite diceFace;
    void Start()
    {
        data = this;
    }

    public void SetDiceInfo(int value, Sprite sprite)
    {
        DiceValue = value;
        diceFace = sprite;
        SpriteRenderer sr = this.GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
        {
            sr.sprite = diceFace;
        }
    }

    public void SetDiceValue(int value) => DiceValue = value;
    public int GetDiceValue() => DiceValue;
}
