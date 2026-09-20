using UnityEngine;
using Assets.Scripts.GlobalEnums.BattleEnum;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：戰鬥彈出物生成器。負責在指定錨點生成傷害/治療數值與附加狀態彈出（文字＋icon）。
    /// 由各單位的 <see cref="BattleUnitView"/> 呼叫。顏色與（可選的）動態參數覆寫都在這裡集中調整。
    /// </summary>
    public class BattlePopupSpawner : MonoBehaviour
    {
        [Header("彈出物 Prefab（需含 FloatingPopup）")]
        [SerializeField] private FloatingPopup popupPrefab;

        [Tooltip("彈出物生成層（可空）。留空＝掛到 anchor 所在 Canvas 的根，避免被 HP 條的遮罩(Mask)裁掉、也不受 HP 條縮放影響。")]
        [SerializeField] private RectTransform overlayLayer;

        [Header("顏色")]
        public Color damageColor = new Color(0.85f, 0.15f, 0.15f); // 傷害紅
        public Color healColor = new Color(0.30f, 0.80f, 0.35f);   // 治療綠
        public Color statusColor = Color.white;                    // 狀態文字色

        [Header("是否用下列參數覆寫 Prefab 的動態（統一在此調）")]
        public bool overridePopupParams = false;
        public float popForce = 160f;
        public float gravity = 320f;
        public float horizontalJitter = 30f;
        public float fadeInTime = 0.08f;
        public float stayTime = 0.5f;
        public float fadeOutTime = 0.35f;

        /// <summary>彈出傷害/治療數值。value 負＝傷害、正＝治療；為 0 不彈。</summary>
        public void PopupNumber(RectTransform anchor, int value)
        {
            if (value == 0) return;
            string msg = value > 0 ? "+" + value : value.ToString(); // 負值本身已含「-」
            Color color = value < 0 ? damageColor : healColor;
            Spawn(anchor, msg, color, null);
        }

        /// <summary>彈出附加狀態（文字＋狀態 icon）。</summary>
        public void PopupStatus(RectTransform anchor, BattleStatusEffectType effectType, Sprite icon, string displayName = null)
        {
            string msg = string.IsNullOrEmpty(displayName) ? effectType.ToString() : displayName;
            Spawn(anchor, msg, statusColor, icon);
        }

        private bool warnedNoPrefab;

        private void Spawn(RectTransform anchor, string message, Color color, Sprite icon)
        {
            if (popupPrefab == null)
            {
                if (!warnedNoPrefab) // 只警告一次，避免每次彈出都洗 Console 造成卡頓
                {
                    warnedNoPrefab = true;
                    Debug.LogWarning($"[{name}] 未指派 popupPrefab，無法生成彈出物。");
                }
                return;
            }
            // 生成層：優先 overlayLayer；否則用 anchor 所在 Canvas 的根。
            // 不掛在 anchor(HP 條)底下——那裡會被 HP 條的遮罩裁掉、又吃到 HP 條的縮放而變小/看不到。
            RectTransform layer = overlayLayer;
            if (layer == null)
            {
                Canvas canvas = anchor != null ? anchor.GetComponentInParent<Canvas>() : GetComponentInParent<Canvas>();
                if (canvas != null) layer = canvas.transform as RectTransform;
            }
            Transform parent = layer != null ? layer : (anchor != null ? anchor : transform);

            FloatingPopup popup = Instantiate(popupPrefab, parent);
            // 掛到 overlay/Canvas 根時，把位置對到 anchor 的螢幕位置（單位身上）；否則就用 anchor 本地原點
            if (anchor != null && parent != (Transform)anchor)
                popup.GetComponent<RectTransform>().position = anchor.position;
            else
                popup.transform.localPosition = Vector3.zero;
            popup.transform.localScale = Vector3.one;

            if (overridePopupParams)
            {
                popup.popForce = popForce;
                popup.gravity = gravity;
                popup.horizontalJitter = horizontalJitter;
                popup.fadeInTime = fadeInTime;
                popup.stayTime = stayTime;
                popup.fadeOutTime = fadeOutTime;
            }

            popup.Show(message, color, icon);
        }
    }
}
