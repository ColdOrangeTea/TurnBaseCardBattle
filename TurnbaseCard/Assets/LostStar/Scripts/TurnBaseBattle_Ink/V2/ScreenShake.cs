using System.Collections;
using UnityEngine;

namespace TurnBaseBattleV2
{
    /// <summary>
    /// V2 架構：畫面震動。因戰鬥是 ScreenSpaceOverlay UI，震 Camera 無效，故震動指定的 UI 根
    /// （通常指到 BattleEmpty 或其 BattleUI）。攻擊/受擊時呼叫 Shake()。力道與時間可調。
    /// </summary>
    public class ScreenShake : MonoBehaviour
    {
        [Header("震動目標（通常是 BattleEmpty / BattleUI 的 RectTransform）")]
        [SerializeField] private RectTransform target;

        [Header("預設力道 / 時間（可被 Shake 參數覆寫）")]
        [Tooltip("最大位移像素")]
        public float defaultStrength = 16f;
        [Tooltip("震動持續秒數")]
        public float defaultDuration = 0.22f;
        [Tooltip("衰減曲線指數：>1 越快收斂")]
        public float damping = 1.5f;

        private Vector2 originAnchoredPos;
        private bool originCaptured;
        private Coroutine shakeCo;

        private void Awake() => CaptureOrigin();

        private void CaptureOrigin()
        {
            if (target != null && !originCaptured)
            {
                originAnchoredPos = target.anchoredPosition;
                originCaptured = true;
            }
        }

        /// <summary>震動。strength/duration 傳負值則用預設值。</summary>
        public void Shake(float strength = -1f, float duration = -1f)
        {
            if (target == null)
            {
                Debug.LogWarning($"[{name}] 未指派震動 target。");
                return;
            }
            CaptureOrigin();

            float s = strength < 0f ? defaultStrength : strength;
            float d = duration < 0f ? defaultDuration : duration;

            if (shakeCo != null) StopCoroutine(shakeCo);
            shakeCo = StartCoroutine(DoShake(s, d));
        }

        private IEnumerator DoShake(float strength, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float falloff = Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), damping); // 隨時間衰減
                Vector2 offset = Random.insideUnitCircle * strength * falloff;
                target.anchoredPosition = originAnchoredPos + offset;
                yield return null;
            }
            target.anchoredPosition = originAnchoredPos; // 回正
            shakeCo = null;
        }
    }
}
