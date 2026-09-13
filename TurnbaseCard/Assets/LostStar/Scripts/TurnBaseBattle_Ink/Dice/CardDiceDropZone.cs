using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.EventSystems;

public class CardDiceDropZone : MonoBehaviour //, IDropHandler
{
    public List<RectTransform> diceList = new List<RectTransform>();  // 保存骰子列表
    public float spacing = 50f;  // 骰子之間的間距
    void Start()
    {
        Init();
    }

    void Init()
    {
        diceList = new List<RectTransform>();
    }


    public void OnDrop(PointerEventData eventData)
    {
    }
    public void SetDiceToDropZone(RectTransform diceUI)
    {
        AddDiceToPanel(diceUI);
    }

    private void AddDiceToPanel(RectTransform diceUI)
    {
        diceList.Add(diceUI);
        ArrangeDice(diceUI); // 重新排列
    }

    private void ArrangeDice(RectTransform diceUI)
    {
        float totalWidth = (diceList.Count - 1) * spacing;
        float startX = -totalWidth / 2f;

        // 保存 diceUI 的当前父级和位置
        Transform originalParent = diceUI.parent;
        Vector2 originalPosition = diceUI.anchoredPosition;

        for (int i = 0; i < diceList.Count; i++)
        {
            RectTransform temp_Dice = diceList[i]; // 从 diceList 中取得每个 RectTransform

            float targetX = startX + i * spacing;

            // 将 temp_Dice 设置为新的父级并调整位置
            temp_Dice.SetParent(transform.parent);
            temp_Dice.anchoredPosition = new Vector2(targetX, transform.GetComponent<RectTransform>().anchoredPosition.y);
            temp_Dice.localScale = Vector3.one; // 重置缩放
        }

        // 还原 diceUI 的父级和位置
        diceUI.SetParent(originalParent); // 恢复原始父级
    }
}
