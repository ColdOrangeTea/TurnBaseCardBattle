---
name: unity-asset-generation
description: 搭配 coplay MCP 在 Unity 專案生成或修改 Unity 資源時必讀的須知 — 涵蓋 Editor Tooling、Prefab、ScriptableObject、Script 的產生與修改規範。凡是要新增/修改 Editor 工具腳本、Prefab、ScriptableObject 資產、遊戲 Script，或使用 coplay MCP 操作 Unity Editor 時，先讀本文件。
---

# Unity 資源生成須知

本 Skill 是提供給 AI（搭配 coplay MCP 或直接編輯檔案）在 Unity 專案生成資源時的規範，可通用於同版本的各個 Unity 專案。
專案作者：**A_Good_Ink**。溝通與程式註解一律使用**繁體中文**。

## 通用原則（所有類型皆適用）

1. **優先重用既有資源**：碰到新需求時，先搜尋專案內既有的 Prefab、ScriptableObject、Script 是否可滿足或擴充，而不是直接產生新的物件。動手前先掃描專案實際結構，常見慣例如下：
   - Prefabs：`Assets/Prefabs/<系統名>/`
   - ScriptableObject 資產：`Assets/Data/<系統名>/`
   - 遊戲 Scripts：`Assets/Scripts/<系統名>/`；跨系統列舉：`Assets/Scripts/GlobalEnums/`
   - Editor 工具：`Assets/Editor/`
   - 動畫：`Assets/Animations/<系統名>/`；場景：`Assets/Scenes/`；字型：`Assets/Font/`
   - 若當前專案的實際結構與上述不同，**以當前專案既有的擺放慣例為準**。
2. **同類資源放同資料夾**：專案內已有放置該類資產的地方時，新產出一律放進同樣的資料夾以便整理；沒有才依上述慣例新建資料夾。
3. **Defensive programming**：所有腳本都要防禦性編程 — null 檢查、資料夾不存在先逐層建立、缺少相依資源（如 TMP Essentials、字型檔）時偵測並友善提示或自動補齊，失敗時給出明確的中文錯誤訊息而不是丟出未處理例外。
4. **開源署名**：製程上開發出的工具未來有開源打算。每個工具腳本的開頭註解必須說明：**此工具是由 A_Good_Ink 使用 AI 生成的工具**。
5. **可重複執行（idempotent）**：產出資產的工具必須可重複執行 — 再次執行時**覆蓋更新既有產出，GUID 不變，引用不會斷**。做法：已存在的資產用 `AssetDatabase.LoadAssetAtPath` 載入後就地更新，或用 `PrefabUtility.SaveAsPrefabAsset` 存回同一路徑；**絕不**先刪除再重建（會換 GUID 斷引用）。
6. **留接口**：腳本盡量留有可置入 Prefab、ScriptableObject、其他功能 Script 的接口（序列化欄位、`ObjectField`、公開方法），讓後續能掛接資源與擴充功能，而不是把路徑或行為寫死到無法替換。
7. **尊重使用者的手動調整（不要擅自改回去）**：使用者可能已經自行調整過既有資產的數值（Prefab 的按鈕大小、間距、字級、顏色、Transform、SO 欄位值等）。之後要修改或重新產生相關資產時：
   - **先讀現況再動手**：動手前先把目標資產「目前實際的數值」讀出來（`PrefabUtility.LoadPrefabContents`、`SerializedObject`，或用 coplay MCP 讀 Inspector），不要憑先前對話內容或工具腳本裡的預設值直接覆蓋。
   - **發現差異先確認**：現況與你（或既有 Editor 工具）預期寫入的數值不一致時，**一律視為使用者刻意調整**。先回報「偵測到 X 目前是 A，工具預設會寫成 B，要保留還是覆蓋？」，等使用者答覆再動作。
   - **預設保留**：使用者沒有明確要求改動的欄位一律保留原值，只寫入這次需求真正相關的欄位。
   - **工具要支援保留**：可重複執行的產生工具，對「外觀 / 手感」類欄位（尺寸、間距、字級、顏色、錨點、Padding）預設採「資產不存在時才寫入初始值，既有資產不覆寫」；或提供 `覆寫既有外觀數值` 勾選選項（**預設不勾**）交給使用者決定。
   - 這條**優先於**下方「修改既有 Prefab 可自行更動補齊」的授權：補齊**缺少的東西**可以自己來，覆蓋**使用者已經設定過的值**一定要先問。

## Editor Tooling / Editor Extension 工具腳本

- **位置與性質**：放在 `Assets/Editor/` 資料夾、使用 `UnityEditor` API、只在編輯器內執行，打包後的遊戲不會包含它。不要在 runtime 腳本中引用 `UnityEditor`（如必要須包在 `#if UNITY_EDITOR` 內）。
- **開頭註解**：檔案開頭必須有 `/// <summary>` XML 註解，包含：
  - 工具說明（做什麼、產出什麼、產出位置）
  - 使用方式（Unity 選單路徑與操作步驟）
  - 「可重複執行：會覆蓋更新既有產出（GUID 不變，引用不會斷）」聲明
  - 由 A_Good_Ink 使用 AI 生成的署名
- **選單調用**：工具一律從 Unity 上方工具列調用，`[MenuItem]` 路徑格式：`Tools/(功能分類名稱)/(工具名稱)`。同一系統的工具用同一分類，例：
  - `Tools/Dialogue/字型烘焙 (Bake SDF Font from DialogueData)`
  - `Tools/Dialogue/Generate Dialogue UI (Prefabs + Sample Scene)`
  - 工具名稱可中英並用（中文描述 + 英文括號說明）。
- **UI 形式**：需要參數設定的工具做成 `EditorWindow`（ObjectField 指定來源資產、可調輸出資料夾與名稱、結果預覽區）；一鍵執行的工具可用 `static class` + `MenuItem` 直接執行，完成後用 `EditorUtility.DisplayDialog` 列出所有產出路徑。
- **常用慣例**：
  - 路徑用 `private const string` 集中宣告在類別頂端。
  - 產出前逐層確認/建立資料夾（`AssetDatabase.IsValidFolder` + `CreateFolder`，慣例包成 `EnsureFolder` 輔助方法）。
  - 有修改場景的工具先呼叫 `EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()`。
  - 產出後 `AssetDatabase.SaveAssets()` + `AssetDatabase.Refresh()`。
  - GUI 欄位用 `new GUIContent(label, tooltip)` 附上中文 tooltip。
  - 預設值自動帶入專案既有資源（如 `OnEnable` 中嘗試載入專案內既有的字型檔、資料資產）。
  - UnityEvent 的持久化綁定用 `UnityEditor.Events.UnityEventTools`，不要只在記憶體綁定。
- **範例參照**：若專案內已有 Editor 工具（如 `Assets/Editor/` 下的既有 EditorWindow / MenuItem 工具），先閱讀並沿用其風格與慣例。

## Prefab 資產

- **位置**：`Assets/Prefabs/<系統名>/`，或當前專案既有的 Prefab 擺放位置。
- **新增**：使用者提到 Prefab 操作時，要新增就新增（建 GameObject 階層 → `PrefabUtility.SaveAsPrefabAssetAndConnect` 存到對應資料夾）。
- **修改既有 Prefab**：若提到使用某 Prefab，但該 Prefab 有 Component 缺少或設定需要調整，**AI 可在進行途中自行更動補齊**（加 Component、補引用、補上沒設定過的參數），不必先停下來詢問；完成後回報動了什麼。
  - **但「使用者已經調整過的既有數值」不在此授權內**（見通用原則 7）。修改前先把 Prefab 現況讀出來比對，發現尺寸 / 間距 / 字級 / 顏色 / RectTransform 等與工具預設不同時，**先問過再決定保留或覆蓋**，不要自動改回工具預設值。
  - 判斷準則：**缺的可以補，既有的要問**。無法判斷某個數值是預設還是使用者刻意調的，就當成使用者調的、先問。
  - 修改方式：`PrefabUtility.LoadPrefabContents` → 修改 → `SaveAsPrefabAsset` → `UnloadPrefabContents`；或透過 coplay MCP 直接在 Editor 內操作。
  - 保持 GUID 不變：一律就地更新，不刪除重建。
  - 回報時明確列出「保留了哪些使用者的既有設定」與「改動了哪些欄位」。
- **巢狀 Prefab**：Canvas 類的組合 Prefab 可內含巢狀子 Prefab（先把子物件各自存成 Prefab 再組進母 Prefab）。修改子 Prefab 時注意母 Prefab 的 override 狀態。
- **UI Prefab 注意**：文字一律用 TMP；若專案含中文文本，使用專案內的中文 SDF 字型資產（避免中文顯示為 □□□），沒有就先產生。Canvas 參考解析度依專案設定（常見 1920×1080）。

## ScriptableObject 資產

- **定義**（runtime 類別）：放 `Assets/Scripts/<系統名>/`，加 `[CreateAssetMenu]`；欄位加 `[Tooltip]` 中文說明。
- **資產檔**：放 `Assets/Data/<系統名>/`，或當前專案既有的資料資產擺放位置。
- **產生/修改資產的工具**：與 Prefab 相同，屬 Editor Tooling 類型，遵守上方 Editor Tooling 全部規範 — 已存在的資產載入後就地更新（GUID 不變），新資產用 `AssetDatabase.CreateAsset`，改完 `EditorUtility.SetDirty` + `AssetDatabase.SaveAssets()`。
- **自訂 Inspector 繪製**：複雜巢狀資料用 `[CustomPropertyDrawer]`（`PropertyDrawer`）改善編輯體驗，放 `Assets/Editor/`。慣例：
  - 依模式切換顯示欄位（如「讀取共用資訊表 / 自定義」用不同欄位組）。
  - 引用資產可視化：顏色用色塊點選、Sprite 用縮圖點選 + 放大預覽、位置用示意圖預覽。
  - 用 `property.FindPropertyRelative` 取子欄位、`EditorGUI.BeginProperty/EndProperty` 包住、正確實作 `GetPropertyHeight`。
  - 樣式（GUIStyle）用 static 延遲初始化；尺寸常數集中宣告在類別頂端。
  - 若專案內已有 PropertyDrawer，先閱讀並沿用其風格。

## Script（遊戲腳本）

- **位置**：`Assets/Scripts/<系統名>/`；系統專用列舉放 `Assets/Scripts/<系統名>/<系統名>Enum/`，跨系統列舉放 `Assets/Scripts/GlobalEnums/`。
- **命名空間**：沿用當前專案既有慣例（如 `Assets.Scripts.<系統名>`）；專案沒有用命名空間就不強加。
- **註解**：類別頂端 `/// <summary>` 中文說明職責；序列化欄位加 `[Tooltip]` 或行內中文註解。
- **接口設計**：對外相依（Prefab、ScriptableObject、其他 Controller）用序列化欄位或屬性注入，不在程式碼中硬編 `Find` / 路徑字串；讓 Editor 工具可以用 `UnityEventTools` 或直接指定引用來完成綁定。
- **Defensive programming**：Awake/Start 檢查必要引用，缺少時 `Debug.LogWarning`（中文訊息、指出是哪個物件缺什麼）並安全退場，不讓 NullReferenceException 直接炸出。

## coplay MCP 使用注意

- 透過 coplay MCP 在 Unity Editor 內操作（建物件、掛 Component、改參數）時，同樣遵守上述規範：重用既有資源、就地更新保 GUID、產出放對應資料夾、**先讀現況再改（不覆蓋使用者的手動調整）**。
- 大量或可重複的產出流程，優先寫成 Editor 工具腳本（可重複執行、可開源），而不是靠 MCP 一次性手動堆出來。
- 操作完成後確認 Unity 無 compile error，並儲存場景與資產。
