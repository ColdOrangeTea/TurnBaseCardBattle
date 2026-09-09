# 回合制卡牌戰鬥 — 範例（由 A_Good_Ink 使用 AI 生成）

用來觀察整理後的戰鬥腳本／Prefab 如何掛載與運作的可運行範例。
UI 已套用專案 `Assets/Image/TurnBaseCardBattle/2DTexture_UI` 內的 2D 美術（背景、卡框、血條、骰子等）；
單位圖像為 Spine，未指定 SO 時會自動略過，只顯示血條/名稱。

## 如何產生 / 重新產生

Unity 上方選單：**Tools/TurnBaseBattle/生成戰鬥範例場景 (Battle Example Scene)**

- 可重複執行：覆蓋更新同路徑的 Prefab 與場景（GUID 不變、引用不會斷）。
- 產生後開啟 `Assets/Scenes/BattleExampleSample.unity` 按 **Play**，會自動開一場測試戰鬥
  （玩家 Seraphis 16HP/2 骰，敵人 Yarn 19HP/1 骰）。
- 「下一回合」按鈕已在產生器內用 UnityEventTools 綁定 `BattleButtonFunction.IsReadyToNextTurn`；
  「開始戰鬥」綁定 `Test_OpenBattle`。

產生器原始碼：`Assets/Editor/BattleExampleSceneGenerator.cs`（本身即「如何組裝」的完整說明）。

## 場景與 BattleEmpty 結構

```
BattleExampleSample.unity
├─ Main Camera / EventSystem
├─ UICanva (Tag: UI_Canva)
│   └─ BattleEmpty (Tag: BattleEmpty)
└─ BattleManagerRoot  (TurnBaseBattleManager + TurnBaseBattleUI + BattleAction + BattleExampleBootstrap)
```

`TurnBaseBattleManager` 以 `transform.GetChild(index)` 取子系統，**BattleEmpty 子物件順序固定、不可調換**：

```
BattleEmpty
├─ [0] BattleUI
│    ├─ [0] BackGround         (2D 背景圖)
│    ├─ [1] PlayerTwo (敵人資訊：Image / NameAndDiceCount / HPEmpty / StatesEmpty)
│    ├─ [2] PlayerOne (玩家資訊：同上)
│    ├─ [3] Group_Cards (4 卡槽，每槽 [0]=保留標籤，抽到的卡成為 [1])
│    ├─ [4] Player2_DicesEmpty ([0]=Dots, [1]=Player2Dices)
│    ├─ [5] Player1_DicesEmpty ([0]=Dots, [1]=Player1Dices)
│    ├─ [6] TurnCountEmpty     ([1]=回合數, [2]=輪到誰)
│    └─ [7] SettlementEmpty
├─ [1] ButtonsEmpty ([0]=ToNextTurn, [1]=ShowCheat, [2]=CheatButtons[0]=開始戰鬥 [1]=施加中毒)
├─ [2] EnemyActionPanel
├─ [3] TurnBaseBattleScripts ([0]=Dice系統, [1]=Card系統, [2]=佔位, [3]=SetUp, [4]=ButtonFunction)
└─ [4] UnitDatas ([0]=PlayerUnit Tag:Player1, [1]=EnemyUnit Tag:Player2)
```

## 用到的 2D 美術（可自行換成其他張）

| 用途 | 檔案 |
|------|------|
| 戰鬥背景 | `LS2_BackGround/battlebackground_Endless.png` |
| 卡框 | `LS2_Card/Card504_704.png` |
| 卡槽/骰子投放區 | `LS2_Card/DiceInputArea120_120.png` |
| 骰子 | `LS2_Dice&State/Dice.png` |
| 血條底/滿 | `LS1/BattleCommon/UI_BattleCommon_Hp_Empty.png` / `..._Hp_Full.png` |
| 結算勝/敗 | `LS1/Settlement/UI_Settlement_Victory_*` / `..._Lose_*` |

## 資料來源
- 單位數值：`Resources/SO_Battle/BattleUnitStats.asset`
- 狀態效果：`Resources/SO_Battle/BattleStatusEffectData.asset`
（由 `BattleDataProvider` 執行期載入，改數值改這裡。）

## 接上正式美術／Spine
於兩個單位的 `BattleUnitProfile` 指定 `characterProfiles` / `enemyProfiles`（Spine 圖像 SO）與 `UnitVFXPlayer`；
未指定時自動略過 Spine。標籤 `UI_Canva / BattleEmpty / Player1 / Player2` 由產生器自動建立，請保留。
```
