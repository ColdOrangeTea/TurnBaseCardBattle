using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float cameraHeight = 20.35f; // 攝影機與關卡之間的高度距離
    public float smoothSpeed = 0.125f; // 攝影機平滑移動的速度

    public Transform playerTarget; // 當前的關卡攝影機目標點 = 玩家

    void LateUpdate()
    {
        if (playerTarget != null)
        {
            // 攝影機的位置應該是目標點的上方
            Vector3 desiredPosition = new Vector3(playerTarget.position.x, playerTarget.position.y + cameraHeight, playerTarget.position.z);
            // 平滑移動攝影機位置
            Vector3 smoothedPosition = Vector3.Lerp(transform.position, desiredPosition, smoothSpeed);
            transform.position = smoothedPosition;
        }
    }

    // 玩家進入不同關卡時呼叫此方法
    public void SetCameraTarget(Transform newTarget)
    {
        playerTarget = newTarget; // 更新攝影機的目標點
    }
}
