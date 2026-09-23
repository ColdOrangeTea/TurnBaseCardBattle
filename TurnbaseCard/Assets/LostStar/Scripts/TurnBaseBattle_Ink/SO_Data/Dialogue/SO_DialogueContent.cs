using System;
using System.Collections.Generic;
using Assets.Scripts.Dialogue;
using UnityEngine;

/// <summary>
/// 角色對話內容管理
/// </summary>
[CreateAssetMenu(fileName = "SO_DialogueContent", menuName = "SO/Dialogue/Create SO_DialogueContent", order = 1)]
public class SO_DialogueContent : ScriptableObject
{
    [Header("角色對話設定：從文本文件中讀取對話數據並解析")]
    [Space(15)]
    public TextAsset DialogueTextAsset;
    public string[] Temp_Array = null;
    private const string customDelimiter = "[part]"; // 自定義分隔符號
    private const string newLineMarkDelimiter = "\n"; // 換行符號
    public const int stringColumnCount = 3;

    public const string Seraphis = @"Seraphis";
    public const string Jephthah = @"Jephthah";
    public const string Orlana = @"Orlana";
    public const string Sephil = @"Sephil";
    public const string Eva = @"Eva";
    public const string Ava = @"Ava";
    public const string Margaret = @"Margaret";

    [Header("角色對話數據")]
    public List<string> CharacterNameList = new List<string>();
    public List<DialogueUnitType> NameTypeList = new List<DialogueUnitType>();
    public List<string> CharacterExpressionList = new List<string>();
    public List<CharacterExpressionType> ExpressionTypeList = new List<CharacterExpressionType>();

    public List<string> DialogueTextList = new List<string>();

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

    void GetTextFromFile(TextAsset textAsset, List<String> nameList, List<String> expressionList, List<String> contentList)
    {
        // 將文本中的對話數據分別存入對應的列表
        if (Temp_Array != null)
            Array.Clear(Temp_Array, 0, Temp_Array.Length);

        // 將文本按照自定義的分隔符和換行符分割並依次存入對應的角色名、表情和對話內容列表
        Temp_Array = textAsset.text.Split(new[] { customDelimiter, newLineMarkDelimiter }, StringSplitOptions.RemoveEmptyEntries);
        List<string>[] lists = { nameList, expressionList, contentList };

        if (stringColumnCount != lists.Length)
        {
            Debug.LogWarning("對話數據的 ScriptableObject 中的列表數量與設定不符: " + lists.Length + " 預期數量: " + stringColumnCount);
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

    void StringToEnum()
    {
        foreach (string str in CharacterExpressionList)
        {
            CharacterExpressionType type = ExpressionStringToEnum(str);
            ExpressionTypeList.Add(type);
        }
        foreach (string str in CharacterNameList)
        {
            DialogueUnitType type = NameStringToEnum(str);
            NameTypeList.Add(type);
        }
    }

    DialogueUnitType NameStringToEnum(string nameString) // 文本的英文名稱轉Enum
    {
        DialogueUnitType charaType = DialogueUnitType.Undefined;
        foreach (DialogueUnitType type in Enum.GetValues(typeof(DialogueUnitType)))
        {
            if (type.ToString() == nameString)
            {
                // Debug.Log(type);
                charaType = type;
            }
        }
        if (charaType == DialogueUnitType.Undefined)
        {
            Debug.LogWarning("人物type不正確，請確認人名或表情是否有拚寫錯誤" + charaType + " 文件: " + DialogueTextAsset.name);

            return charaType;
        }
        else
        {
            return charaType;
        }
    }

    CharacterExpressionType ExpressionStringToEnum(string expressionString)
    {
        foreach (CharacterExpressionType type in Enum.GetValues(typeof(CharacterExpressionType)))
        {
            if (expressionString == type.ToString())
            {
                return type;
            }
        }
        Debug.LogWarning($"表情type不正確: {expressionString} 文檔 {DialogueTextAsset.name} 中可能有拼字錯誤或包含未定義的表情。");
        return CharacterExpressionType.Noexpression;
    }

    // 當數據變更時，將會重新讀取文本檔案並更新角色對話數據
    void OnValidate()
    {
        CharacterNameList.Clear();
        NameTypeList.Clear();
        CharacterExpressionList.Clear();
        ExpressionTypeList.Clear();

        DialogueTextList.Clear();
        if (DialogueTextAsset != null)
        {
            GetTextFromFile(DialogueTextAsset, CharacterNameList, CharacterExpressionList, DialogueTextList);
            StringToEnum();
        }

    }
}