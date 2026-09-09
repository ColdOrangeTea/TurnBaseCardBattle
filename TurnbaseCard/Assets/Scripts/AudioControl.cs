using System.Diagnostics;
using UnityEngine;

public class AudioControl : MonoBehaviour
{
    // 宣告 Canvas 和 Panel
    public Canvas WorldCanva;
    public GameObject BattlePanel;
    public GameObject SettlementEmpty;
    public GameObject SHOP;  // 新增 SHOP 物件
    public GameObject Joker; // 新增 Joker 物件

    // 宣告 Player 和 Boss_Level
    public GameObject Boss_Level; // 範圍物件 Boss_Level
    public GameObject Player;     // 玩家物件

    // 宣告 Audio Sources
    public AudioSource WorldBGM;
    public AudioSource BattleBGM;
    public AudioSource Boss_LevelBGM;   // Boss 範圍背景音樂
    public AudioSource Boss_BattleBGM; // Boss 戰鬥背景音樂

    private bool isBattlePanelLastState = false; // 用於記錄 BattlePanel 的上一次狀態
    private bool isPlayerInBossLevel = false;   // 用於記錄 Player 是否在 Boss_Level 中
    private bool isShopVisibleLastState = false; // 用於記錄 SHOP 上一次是否可視
    private bool isJokerVisibleLastState = false; // 用於記錄 Joker 上一次是否可視

    void Update()
    {
        // 改用 Physics.CheckSphere 判斷 Player 是否在 Boss_Level 中
        if (Boss_Level != null && Player != null)
        {
            Collider bossLevelCollider = Boss_Level.GetComponent<Collider>();
            if (bossLevelCollider != null)
            {
                bool currentPlayerInBossLevel = bossLevelCollider.bounds.Contains(Player.transform.position);

                if (currentPlayerInBossLevel != isPlayerInBossLevel)
                {
                    if (currentPlayerInBossLevel)
                    {
                        WorldBGM.Stop();
                        Boss_LevelBGM.Play();
                    }
                    else
                    {
                        Boss_LevelBGM.Stop();
                        Boss_BattleBGM.Stop();
                        WorldBGM.UnPause();
                    }
                }

                isPlayerInBossLevel = currentPlayerInBossLevel;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Boss_Level 沒有 Collider，請檢查配置！");
            }
        }

        // 檢查 BattlePanel 是否啟用
        if (BattlePanel != null && BattlePanel.activeSelf)
        {
            if (!isBattlePanelLastState)
            {
                if (isPlayerInBossLevel)
                {
                    Boss_BattleBGM.Stop();
                    Boss_BattleBGM.Play();
                }
                else
                {
                    BattleBGM.Stop();
                    BattleBGM.Play();
                }
            }
            if (isPlayerInBossLevel)
            {
                Boss_LevelBGM.Pause();
            }
            else
            {
                WorldBGM.Pause();
            }
        }
        else
        {
            if (isBattlePanelLastState)
            {
                if (isPlayerInBossLevel)
                {
                    Boss_LevelBGM.UnPause();
                }
                else
                {
                    WorldBGM.UnPause();
                }
            }

            if (isPlayerInBossLevel)
            {
                Boss_BattleBGM.Pause();
            }
            else
            {
                BattleBGM.Pause();
            }
        }

        // 檢查 SettlementEmpty 是否啟用
        if (SettlementEmpty != null && SettlementEmpty.activeSelf)
        {
            if (isPlayerInBossLevel)
            {
                Boss_BattleBGM.Pause();
            }
            else
            {
                BattleBGM.Pause();
            }
        }

        // 檢查 SHOP 是否可視
        if (SHOP != null && SHOP.activeSelf != isShopVisibleLastState)
        {
            if (SHOP.activeSelf)
            {
                WorldBGM.Pause();
                UnityEngine.Debug.Log("SHOP 可視，停止 WorldBGM");
            }
            else
            {
                WorldBGM.UnPause();
                UnityEngine.Debug.Log("SHOP 不可視，恢復播放 WorldBGM");
            }

            isShopVisibleLastState = SHOP.activeSelf;
        }

        // 檢查 Joker 是否可視
        if (Joker != null && Joker.gameObject.activeSelf != isJokerVisibleLastState)
        {
            if (Joker.gameObject.activeSelf)
            {
                Boss_LevelBGM.Pause();
                UnityEngine.Debug.Log("Joker 可視，停止 WorldBGM");
            }
            else
            {
                Boss_LevelBGM.UnPause();
                UnityEngine.Debug.Log("Joker 不可視，恢復播放 WorldBGM");
            }

            isJokerVisibleLastState = Joker.gameObject.activeSelf;
        }

        // 如果玩家不在 Boss_Level，確保 Boss 音樂完全停止
        if (!isPlayerInBossLevel)
        {
            if (Boss_LevelBGM.isPlaying) Boss_LevelBGM.Stop();
            if (Boss_BattleBGM.isPlaying) Boss_BattleBGM.Stop();
        }

        // 更新上一次狀態
        isBattlePanelLastState = BattlePanel != null && BattlePanel.activeSelf;
    }
}

