using UnityEngine;
using System.Collections;


[ExecuteInEditMode]
public class Endless_GM_EventGrid : MonoBehaviour
{
    public EventType eventType;
    [Header("各類事件格連結")]
    public ShopSystem shopSystem;
    public EventUIManager eventUIManager;
    public TreasureChest treasureChest;
    public QuestManager questManager; // 引用 QuestManager
    public DialogueOpenClose dialogueClose;


    public BattleButtonFunction battleButtonFunction;
    public Endless_GM_S001_PlayerController playerController;
    public bool EventCompleted { get; private set; } = false;

    public bool showGizmo = true;

    public Sprite BosscombatSprite;
    public Sprite shopSprite;
    public Sprite eventSprite;
    public Sprite treasureSprite;

    public GridManager gridManager;
    public SpriteRenderer spriteRenderer;
    public SpriteRenderer BoxspriteRenderer;

    public bool EventStart = false;

    [Header("碰撞相關")]
    public float moveSpeed = 10.0f;       // 移動速度
    public Transform Image; 
    public Vector3 retreatPosition;      // 推撞的位置
    public Vector3 targetPosition;      // 最終圖片的指定位置

    private void OnValidate()
    {
        UpdateGrid();
    }

    public void UpdateGrid()
    {
        if (spriteRenderer != null)
        {
            switch (eventType)
            {
                case EventType.BossCombat:
                    spriteRenderer.sprite = BosscombatSprite;
                    break;
                case EventType.Shop:
                    spriteRenderer.sprite = shopSprite;
                    break;
                case EventType.Event:
                    spriteRenderer.sprite = eventSprite;
                    break;
                case EventType.Treasure:
                    spriteRenderer.sprite = treasureSprite;
                    break;
                case EventType.quest:
                    spriteRenderer.sprite = eventSprite;
                    break;
            }
        }
    }

    private void OnDrawGizmos()
    {
        if (!showGizmo) return;

        Gizmos.color = Color.white;
        switch (eventType)
        {
            case EventType.BossCombat:
                Gizmos.color = Color.red;
                break;
            case EventType.Shop:
                Gizmos.color = Color.green;
                break;
            case EventType.Event:
                Gizmos.color = Color.blue;
                break;
            case EventType.Treasure:
                Gizmos.color = Color.yellow;
                break;
            case EventType.quest:
                Gizmos.color = Color.white;
                break;
        }
        Gizmos.DrawWireCube(transform.position, new Vector3(1, 1, 1));
    }

    //public void TriggerEvent(Transform player)
    //{
    //    EventStart = true;
    //    EventCompleted = false;
    //    shopSystem.CloseShop();

    //    switch (eventType)
    //    {
    //        case EventType.BossCombat:
    //            TriggerBossCombatEvent(player);
    //            break;
    //        case EventType.Shop:
    //            TriggerShopEvent();
    //            break;
    //        case EventType.Event:
    //            TriggerGenericEvent();
    //            break;
    //        case EventType.Treasure:
    //            TriggerTreasureEvent();
    //            break;
    //        case EventType.quest:
    //            TriggerQuestEvent();
    //            break;
    //    }
    //}

    public void TriggerEvent(Transform player)
    {
        //Vector3 retreatTarget = retreatPosition;
        //Vector3 finalTarget = targetPosition;
        //Debug.Log(retreatTarget);
        //StartCoroutine(HandleCollisionEffect(retreatTarget, finalTarget));
        StartCoroutine(DelayedTriggerEvent(player));
    }

    private IEnumerator HandleCollisionEffect(Vector3 retreatTarget, Vector3 finalTarget)
    {
        // 往後退至指定位置
        while (Vector3.Distance(Image.transform.position, retreatTarget) > 0.01f)
        {
            Image.transform.position = Vector3.MoveTowards(Image.transform.position, retreatTarget, moveSpeed * Time.deltaTime);
            yield return null;
            Debug.Log("撞到了");
        }

        yield return new WaitForSeconds(0.1f);

        // 往前移動至最終目標位置
        while (Vector3.Distance(Image.transform.position, finalTarget) > 0.01f)
        {
            Image.transform.position = Vector3.MoveTowards(Image.transform.position, finalTarget, moveSpeed * Time.deltaTime);
            yield return null;
            Debug.Log("回去了");
        }
    }

    private IEnumerator DelayedTriggerEvent(Transform player)
    {
        yield return new WaitForSeconds(1f);

        EventStart = true;
        EventCompleted = false;
        //shopSystem.CloseShop();

        switch (eventType)
        {
            case EventType.BossCombat:
                TriggerBossCombatEvent(player);
                break;
            case EventType.Shop:
                TriggerShopEvent();
                break;
            case EventType.Event:
                TriggerGenericEvent();
                break;
            case EventType.Treasure:
                TriggerTreasureEvent();
                break;
            case EventType.quest:
                TriggerQuestEvent();
                break;
        }
    }

    private void TriggerBossCombatEvent(Transform player)
    {
        Debug.Log("觸發Boss戰鬥事件!");
    }

    private void TriggerShopEvent()
    {
        Debug.Log("開啟商店UI!");
        shopSystem.OpenShopForEndless();

        shopSystem.OnShopClosed += () =>
        {
            EventCompleted = true;
            Debug.Log("商店事件完成!");
            EventStart = false;
            battleButtonFunction.BattleIsEnd = true;
            playerController.ResetPlayerMove();
        };
    }

    private void TriggerGenericEvent()
    {
        Debug.Log("觸發一般事件!");
        eventUIManager.ShowEvent();
        eventUIManager.EventEnd += () =>
        {
            EventCompleted = true;
            Debug.Log("一般事件結束");
            EventStart = false;

            // 隱藏精靈並停用腳本
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
            Destroy(this); // 刪除當前腳本
        };
    }

    private void TriggerTreasureEvent()
    {
        Debug.Log("打開寶箱，獲得獎勵!");
        treasureChest.TriggerTreasureChestForEndLess();

        treasureChest.TreasureEventEnd += () =>
         {
             
             EventCompleted = true;
             Debug.Log("寶箱事件結束");
             EventStart = false;
             battleButtonFunction.BattleIsEnd = true;

             // 隱藏精靈並停用腳本
             if (spriteRenderer != null)
             {
                 spriteRenderer.enabled = false;
                 BoxspriteRenderer.enabled = false;
   
             }
             

             // 啟動協程來延遲執行隱藏精靈的操作
             StartCoroutine(HideSpritesAfterDelay(2f));  // 2秒後隱藏精靈
         };

    }

    // 協程：延遲隱藏精靈
    private IEnumerator HideSpritesAfterDelay(float delayTime)
    {
        yield return new WaitForSeconds(delayTime);  // 等待指定的時間

        if (spriteRenderer != null)
        {
            spriteRenderer.enabled = true;
            BoxspriteRenderer.enabled = true;
            Debug.Log("隱藏 spriteRenderer");
        }

      
    }

    private void TriggerQuestEvent()
    {
        Debug.Log("觸發任務");

        // dialogueClose.OpenDialogue();
        GameManager.DialogueManagerInstance.Temp_AssignL1MainStoryStage(2);

        dialogueClose.OpenDialogue();

        // 獲取一個可用的任務
        QuestData newQuest = questManager.GetQuest(); // 確保有 GetQuest 方法返回一個 QuestData

        if (newQuest != null)
        {
            questManager.AddQuest(newQuest); // 將任務添加到活動任務列表
            questManager.questUI.DisplayQuest(newQuest); // 顯示任務 UI
            Debug.Log($"顯示任務: {newQuest.questName}");
        }
        else
        {
            Debug.Log("沒有可用的任務!");
        }

        questManager.QuestEventEnd += () =>
        {
            EventCompleted = true;
            Debug.Log("任務事件結束");
            EventStart = false;

            // 隱藏精靈並停用腳本
            if (spriteRenderer != null)
            {
                spriteRenderer.enabled = false;
            }
            Destroy(this); // 刪除當前腳本
        };

    }
}

