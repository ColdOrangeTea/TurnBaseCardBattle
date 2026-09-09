using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;
using System;

/// <summary>
/// 戰鬥敵人的名稱與頭銜
/// </summary>
[CreateAssetMenu(fileName = "SO_BattleEnemyNAT", menuName = "SO/Battle/Create SO_BattleEnemyNAT", order = 3)]

public class SO_BattleEnemyNameAndTitle : ScriptableObject
{

    [Header("敵人名稱與頭銜設定：從台詞取得的Enum類型讀取角色名稱與頭銜並解析成中文")]
    [Space(15)]
    public TextAsset EnemyTextAsset; // 總對話人物的中英對照表
    public string[] Temp_Array = null;

    [Header("角色名稱")]
    public List<EnemyType> NameTypeList = new List<EnemyType>();
    public List<string> En_EnemyNameList = new List<string>();
    public List<string> Tw_EnemyNameList = new List<string>();

    [Header("角色頭銜")]
    public List<string> Tw_EnemyTitleList = new List<string>();
    private const string customDelimiter = "[part]"; // 自定義分隔符號
    private const string newLineMarkDelimiter = "\n"; // 換行符號
    public const int stringColumnCount = 3;

    public const string EMPTYSTRING = "Empty";
    public const string E_STRING = "";


    void StringToEnum()
    {
        foreach (string str in En_EnemyNameList)
        {
            EnemyType type = NameStringToEnum(str);
            NameTypeList.Add(type);
        }
    }
    EnemyType NameStringToEnum(string nameString)
    {
        EnemyType enemyType = EnemyType.Undefined_Temp_ThisIsTypeEndNumber;
        foreach (EnemyType type in Enum.GetValues(typeof(EnemyType)))
        {
            if (type.ToString() == nameString)
            {
                // Debug.Log(type);
                enemyType = type;
            }
        }
        if (enemyType == EnemyType.Undefined_Temp_ThisIsTypeEndNumber)
        {
            Debug.LogWarning($"{nameString} 敵人type不正確，請確認是否有對應Type或人名是否有拚寫錯誤 {enemyType}");
            return enemyType;
        }
        else
        {
            return enemyType;
        }
    }
    public string RemoveZWSP(string String)
    {
        // 去除文本中的零寬空格及其他特殊空白字符
        char[] allWhiteSpace = new char[] {
    // SpaceSeparator category
    '\u0020', '\u1680', '\u180E', '\u2000', '\u2001', '\u2002', '\u2003',
    '\u2004', '\u2005', '\u2006', '\u2007', '\u2008', '\u2009', '\u200A',
    '\u202F', '\u205F', '\u3000',
    // LineSeparator category
    '\u2028',
    // ParagraphSeparator category
    '\u2029',
    // Latin1 characters
    '\u0009', '\u000A', '\u000B', '\u000C', '\u000D', '\u0085', '\u00A0',
    // ZERO WIDTH SPACE (U+200B) & ZERO WIDTH NO-BREAK SPACE (U+FEFF)
    '\u200B', '\uFEFF'
    };
        return String.Trim(allWhiteSpace);
    }

    void GetTextFromFile(TextAsset textAsset, List<string> EN_namelist, List<string> TW_namelist, List<string> titlelist)
    {
        // 將文本中的角色名稱與頭銜分別存入對應的列表
        if (Temp_Array != null)
            Array.Clear(Temp_Array, 0, Temp_Array.Length);

        // 將文本按照自定義的分隔符和換行符分割並依次存入對應的名稱和頭銜列表
        Temp_Array = textAsset.text.Split(new[] { customDelimiter, newLineMarkDelimiter }, StringSplitOptions.RemoveEmptyEntries);
        List<string>[] lists = { En_EnemyNameList, Tw_EnemyNameList, Tw_EnemyTitleList };

        if (stringColumnCount != lists.Length)
        {
            Debug.LogWarning("敵人名稱與頭銜的 ScriptableObject 中的名稱列表數量與設定不符: " + lists.Length + " 預期數量: " + stringColumnCount);
            for (int i = 0; i < Temp_Array.Length; i++)
            {
                int temp_Count = lists.Length;
                int remainder = i % temp_Count;
                lists[remainder].Add(RemoveZWSP(Temp_Array[i]));
            }
        }
        else
        {
            for (int i = 0; i < Temp_Array.Length; i++)
            {
                int remainder = i % stringColumnCount;
                lists[remainder].Add(RemoveZWSP(Temp_Array[i]));
            }
        }
    }

    void FindNarration()
    {
        for (int i = 0; i < Tw_EnemyNameList.Count; i++)
        {
            if (Tw_EnemyNameList[i] == EMPTYSTRING)
            {
                Tw_EnemyNameList[i] = E_STRING;
                Tw_EnemyTitleList[i] = E_STRING;
            }
        }
    }
    // 當數據變更時，將會重新讀取文本檔案並更新角色名稱與頭銜
    void OnValidate()
    {
        En_EnemyNameList.Clear();
        Tw_EnemyNameList.Clear();
        Tw_EnemyTitleList.Clear();
        NameTypeList.Clear();

        if (EnemyTextAsset != null)
        {
            GetTextFromFile(EnemyTextAsset, En_EnemyNameList, Tw_EnemyNameList, Tw_EnemyTitleList);
            StringToEnum();
            FindNarration();
        }
    }
}
