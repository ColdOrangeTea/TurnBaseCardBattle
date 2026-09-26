using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums; // Effect / EffectType

[CreateAssetMenu(fileName = "NewEvent", menuName = "SO/Event/事件資料 (SO_Event)")]
public class SO_Event : ScriptableObject
{
    public string Title; // �ƥ�W��
    public string question;     // �ƥ���D
    public string optionA;      // �ﶵA
    public string optionB;      // �ﶵB
    public string optionC;      // �ﶵC
    public string resultA;      // ���A�����G�奻
    public string resultB;      // ���B�����G�奻
    public string resultC;      // ���C�����G�奻

    // �C�ӿﶵ���ĪG�C��
    public List<Effect> effectsA; 
    public List<Effect> effectsB; 
    public List<Effect> effectsC; 
}

