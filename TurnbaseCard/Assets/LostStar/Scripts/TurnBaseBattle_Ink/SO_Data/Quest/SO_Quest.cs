using UnityEngine;

[CreateAssetMenu(fileName = "NewQuest", menuName = "SO/Quest System/Quest")]
public class QuestData : ScriptableObject
{
    public string questName;          // ���ȦW��
    public string description;        // ���ȴy�z

    [Header("���ȼ��y")]
    public RewardData successReward;  // ���Ȧ��\���y
    public RewardData failureReward;  // ���ȥ����g�@�μ��y

    // �i�H�X�i��h�ݩʡA�Ҧp���Ȫ��A�B�ؼмƶq�B���Ȯɶ���
}

[System.Serializable]
public class RewardData
{
    public int gold;                  // ���y�����ƶq
    public string item;               // ���y���~�W��

}
