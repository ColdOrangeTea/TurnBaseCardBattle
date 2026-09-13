# Lost Star v2 — 新專案重構規劃藍圖

> 本文件是「重開一個乾淨 Unity 專案、由 AI 重構 Lost Star」的施工藍圖。
> 舊專案（本 repo）只當**素材來源與參考**，不在此原地重構。
> 所有決定皆已與負責人確認，逐條可追溯（見文末決策紀錄）。

---

## 0. 前提與目標

| 項目 | 結論 |
| --- | --- |
| 主要痛點 | **程式架構為主、資產整理為輔**（兩者都修，架構優先） |
| 設計凍結度 | **核心玩法凍結**（回合制卡牌戰鬥骨架已定）；**內容持續長**（卡片/敵人/關卡/數值） |
| 協作模式 | **負責人 + AI 獨自重構**（無其他人直接進 Unity 編輯資產） |
| 舊資產去留 | **美術素材/字型帶過去（保 GUID）；腳本以移植 V2 為主、其餘重寫** |

**設計含意**：戰鬥引擎當「穩定的程式」、內容當「會長的資料」，兩邊解耦。為「架構乾淨 + 內容可插拔」最佳化，而非為多人協作最佳化。

---

## 1. 資料夾結構 — feature-first 混血

採 **垂直切片（feature-first）+ 一個 Shared 樹 + 一個 Content 樹**。不採 type-first（Prefab/Img/Font 各一大堆再按功能分——那是給多美術團隊用的），也不採「每個 feature 全自帶共用資源」（會複製共用字型/UI/shader）。

```
Assets/
  _Project/
    Features/            ← 垂直切片：一功能的 程式 + 專屬 prefab/img/data + asmdef 綁一起
      Battle/   { Scripts/  Prefabs/  Art/  Battle.asmdef }
      Cards/               抽卡/gacha、技能、卡牌抽取 UI
      Map/                 章節選擇 + 故事節點地圖（含反覆遭遇）
      Dialogue/            對話系統（副團長/聖女/小丑/商人/村民）
      Shop/                商品、武器選擇、道具選擇
      Inventory/           背包
      RunMeta/             碎片經濟、信物門檻、太空船 hub (L9)
      Results/             勝敗結算（流星殞落/勝利、碎片/時間/擊殺）
      SaveLoad/            存讀檔（星圖式 4 格）
      Settings/            設定 + 暫停選單
      TitleScreen/         標題
      HUD/                 遊戲中共用 HUD
    Shared/              ← 「2 個以上功能會用到」才進來
      Fonts/   (烘焙 SDF：華康飾藝體/源石黑體/TaipeiSans)
      UI/      (共用按鈕、視窗底、圖示)
      Art/     (共用材質、shader)
      VFX/  Audio/
      Core/    { EventBus、存檔、工具程式 + Shared.Core.asmdef }
    Content/             ← 純資料：ScriptableObject 的 .asset 實例（見 §3）
      Cards/  Creatures/  Weapons/  Items/  Levels/  Skills/  DialogueText/
  ThirdParty/            ← 匯入套件與外部資源（TMP/DOTween/Ink/Spine runtime…）
  Plugins/
```

### 判準（拿不準時照這個走）
- **資產歸屬**：只有一個功能會用 → 進該 feature 資料夾；2 個以上會用 → 進 `Shared/`。
- **拿不準時先放 feature**，等第二個功能要用時再「升格」搬進 `Shared/`。

---

## 2. 程式邊界 — asmdef 單向分層

每個 feature 一個 asmdef，編譯隔離。依賴**單向、往下**：

```
        Features/*  (Battle、Cards、Map … 彼此「不准」互相引用)
              │  ↓ 只能往下依賴
     Shared.* (UI/VFX/Audio)   +   GameData (SO 型別定義)
              │  ↓
        Shared.Core  (最底層、零依賴：EventBus、存檔、工具)
```

- **Feature 之間不准直接引用**（`Battle.asmdef` 不可引用 `Results.asmdef`）。跨 feature 溝通一律走 `Shared.Core` 的 **EventBus 或介面**。
  → 這是舊系統「兩個靜態事件匯流當隱藏全域」失控的解藥：循環依賴會在**編譯期直接報錯**。
- **SO 類別定義的擺放**（`CardData : ScriptableObject` 這種**程式**，與 `Content/` 的 `.asset` **實例**分開）：
  - 2 個以上 feature 會讀的資料型別（`CardData`、`CreatureData`、`WeaponData`、`LevelData`…）→ 放獨立的 **`GameData.asmdef`**。
  - 只有一個 feature 會讀的（如只有對話用的 `DialogueLineData`）→ 放那個 feature 裡。

---

## 3. 內容資料驅動約定

- **新增內容 = 改資料，不改程式**。卡片/敵人/關卡皆為 ScriptableObject `.asset`，放在 `Content/`；引擎讀資料跑。加一張卡 = 複製一個 `.asset` 填欄位。
- **載入機制：Registry SO + 直接序列化引用**。做 `CardDatabase`、`EnemyDatabase` 等 SO 列出所有內容，引擎持有其引用。
  - ✅ 零魔法字串、編譯期安全、Inspector 點得到。
  - ❌ **不用 `Resources/`**（全塞進 build、無法細粒度卸載，官方不建議）——把舊專案的 `Resources/SO_Battle` 搬出來。
  - ❌ 現階段不用 Addressables（對此規模是殺雞用牛刀；真遇到記憶體/分包需求再升）。

---

## 4. 命名規範

| 對象 | 規則 | 範例 |
| --- | --- | --- |
| 資料夾 | PascalCase；feature 資料夾名 = asmdef 名 | `Battle/` ↔ `Battle.asmdef` |
| 程式檔 | 檔名 = 類別名（Unity 硬性要求） | `BattleController.cs` |
| SO 資料 `.asset` | `型別_識別`，方便排序搜尋 | `Card_Fireball`、`Enemy_L1_Goblin`、`Weapon_Sword`、`Level_Ch1_01` |

**鐵律：檔名全面禁止空格與中文。** 中文只留在資產「內容」（SO 顯示欄位、TMP 文字）。
（舊專案地雷：`Line Shader.shadergraph`、`TX_Planet 1.mat` 這類帶空格檔名會害 CLI/腳本出錯。）

---

## 5. 舊資產遷移

- **方式：保 GUID 直接搬**（連 `.meta` 一起複製），舊引用不斷。字型 SDF `.asset` 亦然（重烙要重設 atlas，值得保）。
- **節奏：按需逐片搬**。建到哪個 feature，才從舊專案把那片**實際引用到**的乾淨素材搬過來。只有「地基級共用」（字型 SDF、Spine runtime、共用 UI）在第 1 步先搬。
  → **沒有任何 feature 引用到的素材 = 該丟的垃圾**，按需搬會讓它們自動留在舊專案、進不了新專案。這是「不要再雜亂」的最強過濾器。

### 明確「不要搬」的垃圾清單（舊 repo 已確認）
- `_BattleScriptsBackup_2026-09-10/`（整包舊戰鬥備份）
- `TutorialInfo/`、`Readme.asset`（Unity 範本殘留）
- 重複的動畫資料夾：`A_CharacterAnimations/`（186）與 `Animations/`（29）——擇一、去重
- `Data/` 與 `Resources/` 各一份的 `SO_Battle`——合併去重，搬去 `Content/`
- repo 根目錄散落的 `CameraController.cs`
- 測試殘留 `AsterDiaTest.asset`

---

## 6. 執行順序（切片順序）

由「被依賴最多的地基」往「最外圈的殼」推：

```
0. 骨架：建 _Project/{Features,Shared,Content} 與 ThirdParty/
        先把第三方(TMP/DOTween/Ink/Spine runtime)歸到 ThirdParty/
1. 地基：Shared.Core(EventBus/存檔/工具) + GameData(SO 型別定義) → 能編譯
2. 核心迴圈：Battle(移植已完成的 V2，非重寫) + Content 的 Card/Enemy registry
        ✅ 第一個可玩里程碑：測試場景能跑完一整場戰鬥
3. 外圈 feature：Map/遭遇 → Shop/Inventory → Dialogue → RunMeta(碎片/信物/太空船)
4. 殼層 UI：Title → SaveLoad → Settings/Pause → Results → HUD 整合
5. 串成完整遊戲流程
```

**關鍵**：Battle V2 已大致完成（`V2/` 9 支已在驅動、舊系統已刪），第 2 步是「把已完成的 V2 搬進乾淨骨架」，一開局就有能跑的核心，後面每片都掛在穩定核心上。

---

## 7. 每片的驗收門檻

- **通則**：編譯零錯 + 該 feature 的**獨立測試場景能手動跑過核心操作**。**沒過門檻，不准動下一片。**
- **戰鬥數值核心額外加 EditMode 單元測試**：`CardCalculation`、`StatusValueSetter`（本來就保留的 verified core）。
  → 只有數值計算值得寫測試——最容易 regression、又最難用眼睛看出錯。其餘 feature 手動跑通即可，不追求全自動測試。

---

## 決策紀錄（可追溯）

| 題 | 決定 |
| --- | --- |
| Q1 痛點 | (c) 兩者都修，程式架構為主 |
| Q2 凍結度 | 核心凍結、內容持續長 |
| Q3 協作 | 負責人 + AI 獨自重構 |
| Q4 去留 | 美術/字型帶過去、腳本重寫（以移植 V2 為主） |
| Q5 組織 | feature-first 混血（Shared 共用 / feature 專屬 / Content 資料） |
| Q6 內容 | (a) 資料驅動 ScriptableObject |
| Q7 遷移方式 | (a) 保 GUID 直接搬（含字型 SDF） |
| Q8 程式邊界 | asmdef 單向分層 + 獨立 GameData asmdef 放共用資料型別 |
| Q9 載入 | (a) Registry SO；棄用 Resources/ |
| Q10 命名 | PascalCase 資料夾、`型別_識別` SO 命名、禁空格禁中文檔名 |
| Q11 順序 | 骨架 → 地基 → Battle 核心 → 外圈 feature → 殼層 UI |
| Q12 驗收 | (b) 手動場景門檻 + 戰鬥數值核心 EditMode 測試 |
| Q13 節奏 | (b) 按需逐片搬，只有地基級共用先搬 |
