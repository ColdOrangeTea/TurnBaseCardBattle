using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：單一「彈出物」——飄出傷害/治療數值，或附加狀態（文字＋狀態 icon）。
    /// 掛在彈出物 Prefab 上；由 <see cref="BattlePopupSpawner"/> 生成後呼叫 Show(...)，
    /// 自己跑完彈出→淡入→停留→淡出動畫後銷毀。所有力道/時間參數可在 Inspector 或腳本調整。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class FloatingPopup : MonoBehaviour
    {
        [Header("元件（Prefab 上指定）")]
        [SerializeField] private TMP_Text text;
        [Tooltip("狀態 icon（數值彈出時會自動隱藏）")]
        [SerializeField] private Image icon;

        [Header("彈出動態（可調）")]
        [Tooltip("往上彈的初速（越大彈越高）")]
        public float popForce = 160f;
        [Tooltip("下墜加速度（越大回落越快）")]
        public float gravity = 320f;
        [Tooltip("左右隨機偏移，讓多個彈出物不重疊")]
        public float horizontalJitter = 30f;

        [Header("淡入 / 停留 / 淡出（秒，可調）")]
        public float fadeInTime = 0.08f;
        public float stayTime = 0.5f;
        public float fadeOutTime = 0.35f;

        private CanvasGroup cg;
        private RectTransform rt;

        private void Awake()
        {
            cg = GetComponent<CanvasGroup>();
            rt = GetComponent<RectTransform>();
        }

        /// <summary>顯示一則彈出。iconSprite 為 null 時為純數值/文字彈出。</summary>
        public void Show(string message, Color color, Sprite iconSprite = null)
        {
            if (cg == null) cg = GetComponent<CanvasGroup>();
            if (rt == null) rt = GetComponent<RectTransform>();

            if (text != null)
            {
                text.text = message;
                text.color = color;
            }
            if (icon != null)
            {
                icon.enabled = iconSprite != null;
                if (iconSprite != null) icon.sprite = iconSprite;
            }

            StopAllCoroutines();
            StartCoroutine(Animate());
        }

        private IEnumerator Animate()
        {
            cg.alpha = 0f;
            Vector2 basePos = rt.anchoredPosition + new Vector2(Random.Range(-horizontalJitter, horizontalJitter), 0f);
            rt.anchoredPosition = basePos;

            float velocityY = popForce;   // 目前上彈速度
            float offsetY = 0f;           // 相對起點的位移
            float elapsed = 0f;
            float total = fadeInTime + stayTime + fadeOutTime;

            while (elapsed < total)
            {
                float dt = Time.deltaTime;
                elapsed += dt;

                // 位移：先上彈再受重力回落
                velocityY -= gravity * dt;
                offsetY += velocityY * dt;
                rt.anchoredPosition = basePos + new Vector2(0f, offsetY);

                // 透明度：淡入 → 停留(=1) → 淡出
                if (elapsed <= fadeInTime)
                    cg.alpha = fadeInTime <= 0f ? 1f : Mathf.Clamp01(elapsed / fadeInTime);
                else if (elapsed >= total - fadeOutTime)
                    cg.alpha = fadeOutTime <= 0f ? 0f : Mathf.Clamp01((total - elapsed) / fadeOutTime);
                else
                    cg.alpha = 1f;

                yield return null;
            }

            Destroy(gameObject);
        }
    }
}
