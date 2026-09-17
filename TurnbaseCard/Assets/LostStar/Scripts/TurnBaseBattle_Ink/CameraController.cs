using UnityEngine;

/// <summary>
/// 地圖相機跟隨控制器。以固定偏移平滑跟隨目標點（通常是某個 Stage 的 CameraPoint）。
///
/// 這是一張「正面看」的星球地圖（格子分布在 XY 平面、視覺朝向 -Z），相機從 -Z 方向正對地圖，
/// 因此偏移用 <see cref="followOffset"/>（預設 (0,0,-20) 正面視角），而非舊版寫死的 Y 軸俯視偏移。
/// 相機本身的旋轉不由本腳本改動（維持場景中設定的朝向，正面視角即 identity 看 +Z）。
/// </summary>
public class CameraController : MonoBehaviour
{
    [Tooltip("相機相對目標點的固定偏移。正面地圖用 (0,0,-負值) 從前方看；俯視地圖則改用 (0,正值,0)。")]
    public Vector3 followOffset = new Vector3(0f, 0f, -100f);

    [Tooltip("相機平滑移動的速度（0~1，越大越快貼齊）")]
    public float smoothSpeed = 0.125f;

    [Tooltip("目前跟隨的目標點；進入某個 Stage 時由 GridManager 設定")]
    public Transform playerTarget;

    void LateUpdate()
    {
        if (playerTarget == null) return;

        Vector3 desiredPosition = playerTarget.position + followOffset;
        transform.position = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
    }

    /// <summary>切換相機跟隨的目標點（進入下一個 Stage 時呼叫）。</summary>
    public void SetCameraTarget(Transform newTarget)
    {
        playerTarget = newTarget;
    }
}
