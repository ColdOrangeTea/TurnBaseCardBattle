using System.Collections.Generic;
using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：單一單位的「顯示層」。
    ///
    /// 職責單純：訂閱一個 <see cref="BattleUnit"/> 的 Changed 事件，
    /// 把它目前的數值餵給現有的 <see cref="BattleUnitProfile"/>（沿用專案原本的美術：
    /// 名字 / 骰數 / HP 條 / 狀態圖示 / Spine）。
    ///
    /// 取代舊架構中 TurnBaseBattleUnitDisplayData 裡「資料＋顯示＋事件」三合一的糾纏：
    /// 這裡只做顯示，不存 runtime 狀態、不含遊戲規則、不參與事件匯流排。
    /// </summary>
    public class BattleUnitView : MonoBehaviour
    {
        [Tooltip("這個 View 對應的資料來源。可在 Inspector 指定，或由 BattleController 在開戰時用 Bind() 指定。")]
        [SerializeField] private BattleUnit unit;

        [Tooltip("沿用專案原本的美術驅動器（HP 條 / 名字 / 狀態圖 / Spine）。")]
        [SerializeField] private BattleUnitProfile profile;

        [Header("彈出數值 / 狀態 / 震動（表現細節）")]
        [Tooltip("彈出物生成的錨點（放在此單位上方）；留空則以本物件為錨點。")]
        [SerializeField] private RectTransform popupAnchor;
        [Tooltip("共用的彈出物生成器（傷害/治療/狀態）。")]
        [SerializeField] private BattlePopupSpawner popupSpawner;
        [Tooltip("共用的畫面震動器；受擊時觸發。")]
        [SerializeField] private ScreenShake screenShake;
        [Tooltip("受擊（扣血）時是否震動畫面。")]
        [SerializeField] private bool shakeOnDamage = true;

        private void OnEnable() => Subscribe(unit);
        private void OnDisable() => Unsubscribe(unit);

        /// <summary>runtime 期間指定／更換要顯示的單位（換單位會自動重新訂閱並刷新一次）。</summary>
        public void Bind(BattleUnit newUnit)
        {
            if (unit == newUnit)
            {
                Refresh();
                return;
            }
            Unsubscribe(unit);
            unit = newUnit;
            Subscribe(unit);
            Refresh();
        }

        private void Subscribe(BattleUnit u)
        {
            if (u == null) return;
            u.Changed += Refresh;
            u.VfxRequested += OnVfxRequested;
            u.HpPopupRequested += OnHpPopupRequested;
            u.StatusPopupRequested += OnStatusPopupRequested;
            Refresh();
        }

        private void Unsubscribe(BattleUnit u)
        {
            if (u == null) return;
            u.Changed -= Refresh;
            u.VfxRequested -= OnVfxRequested;
            u.HpPopupRequested -= OnHpPopupRequested;
            u.StatusPopupRequested -= OnStatusPopupRequested;
        }

        /// <summary>收到單位的特效需求，轉呼叫沿用的 BattleUnitProfile.PlaySE（未接特效播放器時 PlaySE 內部會安全略過）。</summary>
        private void OnVfxRequested(CardType cardType, bool isDamage, bool isApplyState, BattleStatusEffectType effectType)
        {
            if (profile != null)
                profile.PlaySE(cardType, isDamage, isApplyState, effectType);
        }

        /// <summary>彈出傷害/治療數值；受擊(負值)時震動畫面。</summary>
        private void OnHpPopupRequested(int delta)
        {
            RectTransform anchor = ResolveAnchor();
            if (popupSpawner != null && anchor != null)
                popupSpawner.PopupNumber(anchor, delta);

            if (shakeOnDamage && delta < 0 && screenShake != null)
                screenShake.Shake();
        }

        /// <summary>彈出附加狀態（文字＋狀態 icon，icon 取自 BattleUnitProfile.AllStatusImage）。</summary>
        private void OnStatusPopupRequested(BattleStatusEffectType effectType)
        {
            RectTransform anchor = ResolveAnchor();
            if (popupSpawner == null || anchor == null) return;
            Sprite icon = GetStatusIcon(effectType);
            popupSpawner.PopupStatus(anchor, effectType, icon);
        }

        /// <summary>
        /// 決定彈出物的錨點（必須是 UI RectTransform）：
        /// 優先 popupAnchor；否則本物件若為 RectTransform 就用它；再否則退回單位的 HP 條位置；都沒有則 null。
        /// </summary>
        private RectTransform ResolveAnchor()
        {
            if (popupAnchor != null) return popupAnchor;
            if (transform is RectTransform selfRt) return selfRt;
            if (profile != null && profile.HpRedBar != null) return profile.HpRedBar.rectTransform;
            Debug.LogWarning($"[{name}] 找不到彈出錨點：請指定 BattleUnitView.popupAnchor（單位上方的 RectTransform）。");
            return null;
        }

        private Sprite GetStatusIcon(BattleStatusEffectType effectType)
        {
            if (profile == null || profile.AllStatusImage == null) return null;
            int i = (int)effectType;
            return (i >= 0 && i < profile.AllStatusImage.Count) ? profile.AllStatusImage[i] : null;
        }

        /// <summary>把目前單位的數值同步到美術。資料一變（Changed）就會自動被呼叫。</summary>
        public void Refresh()
        {
            if (unit == null || profile == null) return;

            profile.SetUnitName(unit.NameTw);
            profile.SetUnitDiceCount(unit.MaxDiceCount); // 顯示的「x2」是骰子容量，沿用舊版行為
            profile.SetOriHPCount(unit.MaxHp);
            profile.SetCurHPCount(unit.CurrentHp);
            profile.SetStatus(ToEffectTypes(unit.StatusEffects));

            profile.UpdateProfileUI();
        }

        private static List<BattleStatusEffectType> ToEffectTypes(List<BattleStatusEffect> effects)
        {
            var types = new List<BattleStatusEffectType>();
            if (effects == null) return types;
            foreach (var e in effects)
            {
                if (e != null) types.Add(e.GetEffectType());
            }
            return types;
        }
    }
}
