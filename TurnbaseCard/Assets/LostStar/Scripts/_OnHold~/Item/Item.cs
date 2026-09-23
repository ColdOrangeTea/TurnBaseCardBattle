using UnityEngine;

public enum ItemEffectType
{
    Heal,        // ��_�ͩR��
    Buff,        // �W�[�ݩ�
    Debuff,      // ����ݩ�
    Damage,      // ��ĤH�y���ˮ`
    Special      // �S���ĪG�]�Ҧp�ǰe�^
}

[CreateAssetMenu(fileName = "NewItem", menuName = "SO/Inventory/SO_Item")]
public class Item : ScriptableObject
{
    public string itemName;     // ���~�W��
    public string description;  // ���~�y�z
    public Sprite icon;         // ���~�ϥ�
    public int value;           // ���~���ȡ]�i�Ω�ө�����^
    public int Sellvalue;           // �c�檫�~����
    public ItemEffectType effectType; // �ĪG����
    public int effectValue;           // �ĪG�ƭȡ]�Ҧp��_�h�֥ͩR�ȡ^
}