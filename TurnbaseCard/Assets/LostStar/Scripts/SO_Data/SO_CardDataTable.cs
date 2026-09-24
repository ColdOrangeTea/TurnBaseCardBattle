// 此 ScriptableObject 由 A_Good_Ink 使用 AI 生成。
// 用途：所有卡片 SO_CardData 的總表，供 BattleDataProvider 依 CardType 查詢，
//       讓 BattleCard.SendCardInfo() 與抽卡流程都以 SO 為單一資料來源。
// 資產由 Editor 工具「Tools/TurnBaseBattle/生成卡片資料 SO」生成，置於 Resources/SO_Battle/BattleCards。
using System.Collections.Generic;
using Assets.Scripts.GlobalEnums.BattleEnum;
using UnityEngine;

/// <summary>卡片資料總表：以 CardType 查對應的 SO_CardData。</summary>
[CreateAssetMenu(fileName = "BattleCards", menuName = "SO/Battle/Create SO_CardDataTable", order = 4)]
public class SO_CardDataTable : ScriptableObject
{
    [Tooltip("所有卡片資料；每個 CardType 一筆")]
    public List<SO_CardData> cards = new List<SO_CardData>();

    /// <summary>依卡片型別取得對應資料；找不到回傳 false。</summary>
    public bool TryGet(CardType type, out SO_CardData card)
    {
        if (cards != null)
        {
            for (int i = 0; i < cards.Count; i++)
            {
                if (cards[i] != null && cards[i].cardType == type)
                {
                    card = cards[i];
                    return true;
                }
            }
        }
        card = null;
        return false;
    }
}
