using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums; // Effect / EffectType

[CreateAssetMenu(fileName = "NewEvent", menuName = "SO/Event/事件資料 (SO_Event)")]
public class SO_Event : ScriptableObject
{
    public string Title; // 事件名稱
    public string question;     // 事件題目
    public string optionA;      // 選項A
    public string optionB;      // 選項B
    public string optionC;      // 選項C
    public string resultA;      // 選項A 的結果文字
    public string resultB;      // 選項B 的結果文字
    public string resultC;      // 選項C 的結果文字

    // 每個選項對應的效果清單
    public List<Effect> effectsA;
    public List<Effect> effectsB;
    public List<Effect> effectsC;
}
