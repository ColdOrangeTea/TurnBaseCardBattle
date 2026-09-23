using System.Collections;
using UnityEngine;

/// <summary>
/// 全域「畫面震動」效果（單例），震動主攝影機 <see cref="Camera.main"/> 的位置。由 A_Good_Ink 使用 AI 生成。
///
/// 已從戰鬥 prefab 抽出、改為通用效果：可用在過場動畫、事件演出、打擊感等任何地方——
/// 隨處呼叫 <c>ScreenShake.Instance.Shake()</c>（找不到現成物件會自動建立持久化單例）。
///
/// 註：震相機只影響「相機拍到的內容」（世界/角色/2D 背景）；ScreenSpaceOverlay 的 UI（如戰鬥面板）
/// 不受相機震動影響，那類請用各自的 UI 震動（例如戰鬥受擊改為只搖角色本體）。
/// 若相機有跟隨腳本於 LateUpdate 覆寫位置，震動可能被蓋過——需要時再讓跟隨在震動偏移上疊加。
/// </summary>
public class ScreenShake : MonoBehaviour
{
    [Header("預設力道 / 時間（可被 Shake 參數覆寫）")]
    [Tooltip("最大位移（世界單位）")]
    public float defaultStrength = 0.3f;
    [Tooltip("震動持續秒數")]
    public float defaultDuration = 0.22f;
    [Tooltip("衰減曲線指數：>1 越快收斂")]
    public float damping = 1.5f;

    private static ScreenShake _instance;
    /// <summary>全域單例；沒有現成的會自動建立一個持久化物件。</summary>
    public static ScreenShake Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<ScreenShake>();
                if (_instance == null)
                {
                    var go = new GameObject("[ScreenShake]");
                    _instance = go.AddComponent<ScreenShake>();
                }
            }
            return _instance;
        }
    }

    private Coroutine shakeCo;
    private Transform shakingCam;   // 目前正在震的相機
    private Vector3 camOrigin;      // 震動前的相機位置（回正用）
    private bool hasOrigin;

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(gameObject); return; }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>震動主攝影機。strength/duration 傳負值則用預設值。</summary>
    public void Shake(float strength = -1f, float duration = -1f)
    {
        var cam = Camera.main;
        if (cam == null) { Debug.LogWarning("[ScreenShake] 找不到 Camera.main，略過震動。"); return; }

        float s = strength < 0f ? defaultStrength : strength;
        float d = duration < 0f ? defaultDuration : duration;

        // 若正在震（可能是別台相機或同一台），先把上一台回正，避免把偏移量當成新原點造成漂移
        if (shakeCo != null)
        {
            StopCoroutine(shakeCo);
            if (hasOrigin && shakingCam != null) shakingCam.localPosition = camOrigin;
        }

        shakingCam = cam.transform;
        camOrigin = shakingCam.localPosition;
        hasOrigin = true;
        shakeCo = StartCoroutine(DoShake(s, d));
    }

    private IEnumerator DoShake(float strength, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float falloff = Mathf.Pow(1f - Mathf.Clamp01(elapsed / duration), damping); // 隨時間衰減
            Vector2 rnd = Random.insideUnitCircle * strength * falloff;
            if (shakingCam != null) shakingCam.localPosition = camOrigin + new Vector3(rnd.x, rnd.y, 0f);
            yield return null;
        }
        if (shakingCam != null) shakingCam.localPosition = camOrigin; // 回正
        shakeCo = null;
        hasOrigin = false;
    }
}
