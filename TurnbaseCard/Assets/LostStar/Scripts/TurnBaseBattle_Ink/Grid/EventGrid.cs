using UnityEngine;
using System.Collections;

public enum GridEventType
{
    BossCombat,
    Shop,
    Event,
    Treasure,
    quest
}

[ExecuteInEditMode]
public class EventGrid : MonoBehaviour
{
    public GridEventType eventType;
    // [Header("�U���ƥ��s��")]
    // public ShopSystem shopSystem;
    // public EventUIManager eventUIManager;
    // public TreasureChest treasureChest;
    // public QuestManager questManager; // �ޥ� QuestManager
    // public DialogueOpenClose dialogueClose;
    // public bool EventCompleted { get; private set; } = false;

    // public bool showGizmo = true;

    // public Sprite BosscombatSprite;
    // public Sprite shopSprite;
    // public Sprite eventSprite;
    // public Sprite treasureSprite;

    // public GridManager gridManager;
    // public SpriteRenderer spriteRenderer;
    // public SpriteRenderer BoxspriteRenderer;

    // public bool EventStart = false;

    // [Header("�I������")]
    // public float moveSpeed = 10.0f;       // ���ʳt��
    // public Transform Image; 
    // public Vector3 retreatPosition;      // ��������m
    // public Vector3 targetPosition;      // �̲׹Ϥ������w��m

    // private void OnValidate()
    // {
    //     UpdateGrid();
    // }

    // public void UpdateGrid()
    // {
    //     if (spriteRenderer != null)
    //     {
    //         switch (eventType)
    //         {
    //             case GridEventType.BossCombat:
    //                 spriteRenderer.sprite = BosscombatSprite;
    //                 break;
    //             case GridEventType.Shop:
    //                 spriteRenderer.sprite = shopSprite;
    //                 break;
    //             case GridEventType.Event:
    //                 spriteRenderer.sprite = eventSprite;
    //                 break;
    //             case GridEventType.Treasure:
    //                 spriteRenderer.sprite = treasureSprite;
    //                 break;
    //             case GridEventType.quest:
    //                 spriteRenderer.sprite = eventSprite;
    //                 break;
    //         }
    //     }
    // }

    // private void OnDrawGizmos()
    // {
    //     if (!showGizmo) return;

    //     Gizmos.color = Color.white;
    //     switch (eventType)
    //     {
    //         case GridEventType.BossCombat:
    //             Gizmos.color = Color.red;
    //             break;
    //         case GridEventType.Shop:
    //             Gizmos.color = Color.green;
    //             break;
    //         case GridEventType.Event:
    //             Gizmos.color = Color.blue;
    //             break;
    //         case GridEventType.Treasure:
    //             Gizmos.color = Color.yellow;
    //             break;
    //         case GridEventType.quest:
    //             Gizmos.color = Color.white;
    //             break;
    //     }
    //     Gizmos.DrawWireCube(transform.position, new Vector3(1, 1, 1));
    // }

    // //public void TriggerEvent(Transform player)
    // //{
    // //    EventStart = true;
    // //    EventCompleted = false;
    // //    shopSystem.CloseShop();

    // //    switch (eventType)
    // //    {
    // //        case GridEventType.BossCombat:
    // //            TriggerBossCombatEvent(player);
    // //            break;
    // //        case GridEventType.Shop:
    // //            TriggerShopEvent();
    // //            break;
    // //        case GridEventType.Event:
    // //            TriggerGenericEvent();
    // //            break;
    // //        case GridEventType.Treasure:
    // //            TriggerTreasureEvent();
    // //            break;
    // //        case GridEventType.quest:
    // //            TriggerQuestEvent();
    // //            break;
    // //    }
    // //}

    // public void TriggerEvent(Transform player)
    // {
    //     Vector3 retreatTarget = retreatPosition;
    //     Vector3 finalTarget = targetPosition;
    //     Debug.Log(retreatTarget);
    //     StartCoroutine(HandleCollisionEffect(retreatTarget, finalTarget));
    //     StartCoroutine(DelayedTriggerEvent(player));
    // }

    // private IEnumerator HandleCollisionEffect(Vector3 retreatTarget, Vector3 finalTarget)
    // {
    //     // ����h�ܫ��w��m
    //     while (Vector3.Distance(Image.transform.position, retreatTarget) > 0.01f)
    //     {
    //         Image.transform.position = Vector3.MoveTowards(Image.transform.position, retreatTarget, moveSpeed * Time.deltaTime);
    //         yield return null;
    //         Debug.Log("����F");
    //     }

    //     yield return new WaitForSeconds(0.1f);

    //     // ���e���ʦ̲ܳץؼЦ�m
    //     while (Vector3.Distance(Image.transform.position, finalTarget) > 0.01f)
    //     {
    //         Image.transform.position = Vector3.MoveTowards(Image.transform.position, finalTarget, moveSpeed * Time.deltaTime);
    //         yield return null;
    //         Debug.Log("�^�h�F");
    //     }
    // }

    // private IEnumerator DelayedTriggerEvent(Transform player)
    // {
    //     yield return new WaitForSeconds(1f);

    //     EventStart = true;
    //     EventCompleted = false;
    //     shopSystem.CloseShop();

    //     switch (eventType)
    //     {
    //         case GridEventType.BossCombat:
    //             TriggerBossCombatEvent(player);
    //             break;
    //         case GridEventType.Shop:
    //             TriggerShopEvent();
    //             break;
    //         case GridEventType.Event:
    //             TriggerGenericEvent();
    //             break;
    //         case GridEventType.Treasure:
    //             TriggerTreasureEvent();
    //             break;
    //         case GridEventType.quest:
    //             TriggerQuestEvent();
    //             break;
    //     }
    // }

    // private void TriggerBossCombatEvent(Transform player)
    // {
    //     Debug.Log("Ĳ�oBoss�԰��ƥ�!");
    // }

    // private void TriggerShopEvent()
    // {
    //     Debug.Log("�}�Ұө�UI!");
    //     shopSystem.OpenShop();

    //     shopSystem.OnShopClosed += () =>
    //     {
    //         EventCompleted = true;
    //         Debug.Log("�ө��ƥ󧹦�!");
    //         EventStart = false;
    //     };
    // }

    // private void TriggerGenericEvent()
    // {
    //     Debug.Log("Ĳ�o�@��ƥ�!");
    //     eventUIManager.ShowEvent();
    //     eventUIManager.EventEnd += () =>
    //     {
    //         EventCompleted = true;
    //         Debug.Log("�@��ƥ󵲧�");
    //         EventStart = false;

    //         // ���ú��F�ð��θ}��
    //         if (spriteRenderer != null)
    //         {
    //             spriteRenderer.enabled = false;
    //         }
    //         Destroy(this); // �R�����e�}��
    //     };
    // }

    // private void TriggerTreasureEvent()
    // {
    //     Debug.Log("���}�_�c�A��o���y!");
    //     treasureChest.TriggerTreasureChest();

    //     treasureChest.TreasureEventEnd += () =>
    //      {
    //          EventCompleted = true;
    //          Debug.Log("�_�c�ƥ󵲧�");
    //          EventStart = false;

    //          // ���ú��F�ð��θ}��
    //          if (spriteRenderer != null)
    //          {
    //              spriteRenderer.enabled = false;
    //              BoxspriteRenderer.enabled = false;
    //              Destroy(spriteRenderer);
    //              Destroy(BoxspriteRenderer);
    //          }
    //          Destroy(this); // �R�����e�}��
    //      };

    // }

    // private void TriggerQuestEvent()
    // {
    //     Debug.Log("Ĳ�o����");

    //     // dialogueClose.OpenDialogue();
    //     GameManager.DialogueManagerInstance.Temp_AssignL1MainStoryStage(2);

    //     dialogueClose.OpenDialogue();

    //     // ����@�ӥi�Ϊ�����
    //     QuestData newQuest = questManager.GetQuest(); // �T�O�� GetQuest ��k��^�@�� QuestData

    //     if (newQuest != null)
    //     {
    //         questManager.AddQuest(newQuest); // �N���ȲK�[�쬡�ʥ��ȦC��
    //         questManager.questUI.DisplayQuest(newQuest); // ��ܥ��� UI
    //         Debug.Log($"��ܥ���: {newQuest.questName}");
    //     }
    //     else
    //     {
    //         Debug.Log("�S���i�Ϊ�����!");
    //     }

    //     questManager.QuestEventEnd += () =>
    //     {
    //         EventCompleted = true;
    //         Debug.Log("���Ȩƥ󵲧�");
    //         EventStart = false;

    //         // ���ú��F�ð��θ}��
    //         if (spriteRenderer != null)
    //         {
    //             spriteRenderer.enabled = false;
    //         }
    //         Destroy(this); // �R�����e�}��
    //     };

    // }
}

