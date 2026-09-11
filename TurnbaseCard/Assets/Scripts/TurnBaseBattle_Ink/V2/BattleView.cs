using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using DG.Tweening;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：整場戰鬥的「顯示層」，由 A_Good_Ink 使用 AI 生成的重構版本。
    ///
    /// 職責：把 BattleController 算好的狀態呈現到畫面。所有 UI 物件一律用 [SerializeField]
    /// 在 Inspector 直接拖引用——**完全不使用 Tag / GetChild(index) 尋找**，
    /// 因此子物件順序可以任意調整，不會像舊版那樣默默壞掉。
    ///
    /// 取代舊架構的 TurnBaseBattleUI（約 450 行 GetChild 快取）。
    /// 本類別只負責顯示，不含任何回合邏輯、不訂閱事件匯流排。
    /// </summary>
    public class BattleView : MonoBehaviour
    {
        [Header("整場戰鬥 UI 根（顯示/隱藏用）")]
        [Tooltip("通常指到 BattleEmpty；反覆遭遇戰時開戰顯示、結束隱藏。")]
        [SerializeField] private GameObject battleRoot;

        [Header("單位顯示（各自對應一個 BattleUnitView）")]
        [SerializeField] private BattleUnitView playerView;
        [SerializeField] private BattleUnitView enemyView;

        [Header("背景")]
        [SerializeField] private Image background;
        [Tooltip("背景圖清單；SetBackground(index) 依編號切換")]
        [SerializeField] private List<Sprite> allBackgrounds = new List<Sprite>();

        [Header("回合資訊文字")]
        [SerializeField] private TMP_Text turnCountText;
        [SerializeField] private TMP_Text whoseTurnText;

        [Header("按鈕 / 遮罩")]
        [Tooltip("下一回合按鈕（GameObject，控制顯示/隱藏）")]
        [SerializeField] private GameObject toNextTurnButton;
        [Tooltip("敵人行動時的遮罩")]
        [SerializeField] private GameObject enemyActionPanel;

        [Header("結算")]
        [Tooltip("結算面板根物件")]
        [SerializeField] private GameObject settlementRoot;
        [Tooltip("結算結果文字（勝利 / 失敗）")]
        [SerializeField] private TMP_Text settlementResultText;

        [Header("結算 - 勝/敗 框圖（來自 BattleResult 圖集）")]
        [Tooltip("上框 Image")]
        [SerializeField] private Image settlementFrameUp;
        [Tooltip("中間資訊面板 Image")]
        [SerializeField] private Image settlementInfoPanel;
        [Tooltip("下框 Image")]
        [SerializeField] private Image settlementFrameDown;
        [Tooltip("勝利三張，依序對應：上框 / 中間面板 / 下框")]
        [SerializeField] private List<Sprite> victorySprites = new List<Sprite>();
        [Tooltip("失敗三張，依序對應：上框 / 中間面板 / 下框")]
        [SerializeField] private List<Sprite> loseSprites = new List<Sprite>();
        [Tooltip("勝利文字顏色 (#55FEFF)")]
        [SerializeField] private Color victoryTextColor = new Color(0.3333f, 0.9961f, 1f); // #55FEFF
        [Tooltip("失敗文字顏色 (#D92626)")]
        [SerializeField] private Color loseTextColor = new Color(0.851f, 0.149f, 0.149f);   // #D92626
        [Tooltip("結算的角色名字文字（會跟著勝/敗變色）")]
        [SerializeField] private TMP_Text settlementPlayerNameText;
        [Tooltip("結算的「失去連接」文字（僅失敗時顯示、紅字）")]
        [SerializeField] private TMP_Text settlementLostConnectText;
        [Tooltip("戰利品捲動區（僅勝利時顯示）")]
        [SerializeField] private GameObject settlementLootsScroll;

        #region 開戰綁定
        /// <summary>開戰時把兩個資料單位綁到對應的顯示 View。</summary>
        public void Bind(BattleUnit player, BattleUnit enemy)
        {
            if (playerView != null) playerView.Bind(player);
            else Debug.LogWarning($"[{name}] BattleView.Bind：playerView 未指派。");

            if (enemyView != null) enemyView.Bind(enemy);
            else Debug.LogWarning($"[{name}] BattleView.Bind：enemyView 未指派。");
        }

        /// <summary>強制刷新兩個單位的顯示（一般不用手動呼叫，資料變動會自動刷新）。</summary>
        public void RefreshUnits()
        {
            if (playerView != null) playerView.Refresh();
            if (enemyView != null) enemyView.Refresh();
        }
        #endregion

        #region 背景
        public void SetBackground(int index)
        {
            if (background == null)
            {
                Debug.LogWarning($"[{name}] SetBackground：background Image 未指派。");
                return;
            }
            if (allBackgrounds == null || allBackgrounds.Count == 0)
            {
                Debug.LogWarning($"[{name}] SetBackground：allBackgrounds 為空。");
                return;
            }
            if (index < 0 || index >= allBackgrounds.Count)
            {
                Debug.LogWarning($"[{name}] SetBackground：index {index} 超出範圍，改用第 0 張。");
                index = 0;
            }
            if (allBackgrounds[index] != null)
                background.sprite = allBackgrounds[index];
        }
        #endregion

        #region 回合資訊
        /// <summary>更新「回合數」與「輪到誰」的文字。</summary>
        public void SetTurnInfo(int roundCountFromZero, string whoseTurnTw)
        {
            if (turnCountText != null)
                turnCountText.text = "回合數：" + (roundCountFromZero + 1);
            if (whoseTurnText != null)
                whoseTurnText.text = whoseTurnTw + "的回合";
        }
        #endregion

        #region 整場戰鬥開關（反覆遭遇戰用）
        /// <summary>開戰：顯示整個戰鬥 UI 並確保結算面板隱藏。</summary>
        public void OpenBattle()
        {
            if (battleRoot != null) battleRoot.SetActive(true);
            else Debug.LogWarning($"[{name}] OpenBattle：battleRoot 未指派（通常應指到 BattleEmpty）。");
            ShowSettlement(false, false);
        }

        /// <summary>結束戰鬥：隱藏整個戰鬥 UI（供地圖端在結算後收尾）。</summary>
        public void CloseBattle()
        {
            ShowSettlement(false, false);
            if (battleRoot != null) battleRoot.SetActive(false);
        }
        #endregion

        #region 顯示切換
        public void ShowNextTurnButton(bool show)
        {
            if (toNextTurnButton != null) toNextTurnButton.SetActive(show);
        }

        public void ShowEnemyActionPanel(bool show)
        {
            if (enemyActionPanel != null) enemyActionPanel.SetActive(show);
        }
        #endregion

        #region 結算
        /// <summary>顯示 / 隱藏結算面板，並依勝負套用框圖、結果文字與文字顏色。</summary>
        public void ShowSettlement(bool show, bool isWin, string resultText = "")
        {
            if (settlementRoot != null) settlementRoot.SetActive(show);
            if (!show) return;

            ApplySettlementSkin(isWin); // 依勝/敗換框圖

            Color textColor = isWin ? victoryTextColor : loseTextColor; // 敗方紅字

            if (settlementResultText != null)
            {
                if (!string.IsNullOrEmpty(resultText)) settlementResultText.text = resultText;
                settlementResultText.color = textColor;
            }
            if (settlementPlayerNameText != null) settlementPlayerNameText.color = textColor; // 角色名跟著變色

            // 勝利顯示戰利品、失敗顯示「失去連接」
            if (settlementLootsScroll != null) settlementLootsScroll.SetActive(isWin);
            if (settlementLostConnectText != null)
            {
                settlementLostConnectText.color = loseTextColor;
                settlementLostConnectText.gameObject.SetActive(!isWin);
            }

            // 舊版結算面板預設在畫面外，靠彈跳動畫移到定位（結束於 anchored y=0）。
            // 這裡沿用同一段動畫把它移進畫面，否則只 SetActive 會停在畫面外看不到。
            PlaySettlementAnimation(settlementRoot);
        }

        /// <summary>依勝/敗把上框、中間面板、下框三張 Image 換成對應的 BattleResult sprite。</summary>
        private void ApplySettlementSkin(bool isWin)
        {
            List<Sprite> set = isWin ? victorySprites : loseSprites;
            if (set == null || set.Count < 3) return; // 沒指定就維持原圖

            if (settlementFrameUp != null && set[0] != null) settlementFrameUp.sprite = set[0];
            if (settlementInfoPanel != null && set[1] != null) settlementInfoPanel.sprite = set[1];
            if (settlementFrameDown != null && set[2] != null) settlementFrameDown.sprite = set[2];
        }

        /// <summary>結算面板進場的彈跳動畫（移植自舊 TurnBaseBattleUI.PlaySettlementAnimation，結束於 y=0）。</summary>
        private void PlaySettlementAnimation(GameObject settlementObject)
        {
            if (settlementObject == null) return;
            RectTransform rt = settlementObject.GetComponent<RectTransform>();
            if (rt == null) return;

            float x = rt.anchoredPosition.x;
            Sequence seq = DOTween.Sequence();
            seq.Append(rt.DOJumpAnchorPos(new Vector2(x, 1000), 150, 1, 0.5f).SetEase(Ease.OutQuad))
               .Append(rt.DOJumpAnchorPos(new Vector2(x, 0), 125, 1, 0.5f).SetEase(Ease.OutQuad))
               .Append(rt.DOJumpAnchorPos(new Vector2(x, 250), 75, 1, 0.5f).SetEase(Ease.OutQuad))
               .Append(rt.DOJumpAnchorPos(new Vector2(x, 0), 50, 1, 0.5f).SetEase(Ease.OutQuad))
               .Append(rt.DOJumpAnchorPos(new Vector2(x, 100), 25, 1, 0.5f).SetEase(Ease.OutQuad))
               .Append(rt.DOJumpAnchorPos(new Vector2(x, 0), 0, 1, 0.5f).SetEase(Ease.OutQuad));
        }
        #endregion
    }
}
