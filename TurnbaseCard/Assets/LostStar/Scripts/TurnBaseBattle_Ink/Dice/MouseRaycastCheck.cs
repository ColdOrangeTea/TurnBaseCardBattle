using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

public class MouseRaycastCheck : MonoBehaviour
{
    public static MouseRaycastCheck MouseRaycastCheckInstance;
    [SerializeField] private MouseRaycastCheck temp_MouseRaycastCheckInstance;

    public Camera curMainCamera; // 要自己手動指定
    [SerializeField] private Vector3 MousePos;
    [SerializeField] private Vector3 mouseToWorldPos;
    [SerializeField] private float mouseZ = 10f;
    public GameObject raycasthitTarget;
    public List<GameObject> HitGOs;

    private string diceLayerName = "Dice";
    public const string DICE_INPUT_AREA_TAG = "DiceInputArea";

    [SerializeField] private int diceLayerNum;
    [SerializeField] private int diceLayerMask;


    void Start()
    {
        diceLayerMask = LayerMask.GetMask(diceLayerName); // 這是返回位元碼
        diceLayerNum = LayerMask.NameToLayer(diceLayerName); // 這是返回layer編號
        MouseRaycastCheckInstance = this;
        temp_MouseRaycastCheckInstance = this;
    }
    void Update()
    {
        CheckRaycastHit();
    }

    public void DragRaycastHitTarget(Action actionToExecute)
    {
        if (raycasthitTarget == null) return;
        int layerNumber = raycasthitTarget.layer;
        // Debug.Log($"layerNumber == diceLayerNum {layerNumber == diceLayerNum}, Layer number: " + layerNumber);
        if (layerNumber == diceLayerNum)
        {
            if (HitGOs.Count <= 1)
            {
                actionToExecute?.Invoke(); // Execute the provided action
                return;
            }
            foreach (GameObject go in HitGOs)
            {
                if (go.gameObject.CompareTag(DICE_INPUT_AREA_TAG)) // 從卡上拿開骰子
                {
                    actionToExecute?.Invoke(); // Execute the provided action

                }
            }
        }
    }
    void CheckRaycastHit()
    {
        MousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, mouseZ);
        mouseToWorldPos = curMainCamera.ScreenToWorldPoint(MousePos);
        Vector3 direction = mouseToWorldPos - curMainCamera.transform.position;
        HitGOs.Clear();

        RaycastHit[] hits = Physics.RaycastAll(curMainCamera.transform.position, direction, 50);
        if (hits.Length > 0)
        {
            foreach (RaycastHit hit in hits)
            {
                // Debug.Log("hit Object: " + hit.transform.gameObject.name + " hit Tag: " + hit.transform.tag);
                HitGOs.Add(hit.transform.gameObject);
                // 滑鼠鼠標在骰子上
                if (hit.transform.gameObject.layer == diceLayerNum)
                {
                    // Debug.Log("hit Object: " + hit.transform.gameObject.name + " hit Tag: " + hit.transform.tag);
                    raycasthitTarget = hit.transform.gameObject;
                }

            }
        }
        else
        {
            // Debug.Log($"hits: {hits.Length} ");
            if (raycasthitTarget != null)
                raycasthitTarget = null;
        }
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.blue;

        if (curMainCamera != null)
        {
            // Vector3 mousePos = new Vector3(Input.mousePosition.x, Input.mousePosition.y, 100f);
            // Vector3 worldPos = curMainCamera.ScreenToWorldPoint(mousePos);
            Vector3 cameraOffset = new Vector3(0, -0.5f, 0);
            Vector3 cameraPos = cameraOffset + new Vector3(
                curMainCamera.transform.position.x,
                curMainCamera.transform.position.y,
                curMainCamera.transform.position.z);

            Gizmos.DrawLine(cameraPos, mouseToWorldPos);
            Gizmos.DrawSphere(cameraOffset + curMainCamera.transform.position, 0.02f);
        }
    }

}
