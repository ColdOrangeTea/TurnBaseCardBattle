# 對話系統使用說明(Dialogue System)

> 適用專案:Wayfarer_2D(Unity 2023.2.12f1 / uGUI 2.0 / TextMeshPro)
> 更新日期:2026-07-23

---

## 一、如何產生 UI 資源與範例場景

1. 回到 Unity 編輯器,等待腳本編譯完成。
2. 點選單:**Tools → Dialogue → Generate Dialogue UI (Prefabs + Sample Scene)**
   - 若尚未匯入 TMP Essential Resources,工具會自動匯入,匯入完成後**再執行一次**選單即可。
3. 產生的內容:

| 路徑 | 內容 |
|---|---|
| `Assets/Prefabs/Dialogue/CharacterPortrait.prefab` | ADV 人物立繪(圖像置換用) |
| `Assets/Prefabs/Dialogue/DialogueBoxPanel.prefab` | 對話框(含角色名稱框 NameBox + 對話內容框 ContentBox,皆為 TMP) |
| `Assets/Prefabs/Dialogue/BottomButton.prefab` | 底部功能按鈕(快轉/自動/紀錄 通用,附開關動畫) |
| `Assets/Prefabs/Dialogue/LogEntry.prefab` | 對話紀錄的單筆條目(名稱 + 對白,TMP) |
| `Assets/Prefabs/Dialogue/DialogueCanvas.prefab` | 完整組裝好的對話 Canvas(巢狀包含上述 Prefab,所有引用已綁定) |
| `Assets/Animations/Dialogue/Bottom_Btn_On_Start.anim` | 按鈕「開啟」動畫(放大 + 轉金色,0.15s) |
| `Assets/Animations/Dialogue/Bottom_Btn_On_Loop.anim` | 按鈕「開啟中」循環動畫(呼吸縮放) |
| `Assets/Animations/Dialogue/Bottom_Btn_On_End.anim` | 按鈕「關閉」動畫(縮回 + 轉深色,0.15s) |
| `Assets/Data/Dialogue/SampleDialogue.asset` | 範例對話資訊表(DialogueData,重複執行工具不會覆蓋你的編輯) |
| `Assets/Font/TaipeiSansTCBeta-Regular SDF.asset` | 中文 TMP 字型資產(Dynamic 動態圖集,避免中文顯示 □□□) |
| `Assets/Scenes/DialogueSample.unity` | 範例場景 |

4. 打開 `DialogueSample` 場景按 **Play**,點右上角「**開始對話**」按鈕即可測試:
   - 點擊對話框 → 推進對話/快速完成當前句
   - 底部「快轉」按鈕 → 整段快速播放(開關型,與「自動」互斥,附開關動畫)
   - 底部「自動」按鈕 → 自動播放(開關型,附開關動畫)
   - 底部「紀錄」按鈕 → 開關對話紀錄面板(由上到下列出「名稱 + 對白」,滾輪或拖曳捲動,開啟時自動捲到最新一筆)
   - 底部「**跳過**」按鈕(紅色) → 對話 UI **淡出並結束對話**;對白播畢後再點擊對話框也會觸發同樣的淡出結束

> 工具可**重複執行**,會覆蓋更新既有產出(GUID 不變、引用不會斷)。

---

## 二、腳本架構總覽

```
DialogueCanvas (Canvas)
 ├─ DialogueTypingEffect   ← 打字機效果引擎(核心)
 ├─ TriggerDialogue        ← 對話 UI 總控制器(輸入、按鈕綁定、淡出結束)
 ├─ DialogueGroup          ← CanvasGroup(「跳過」時整組淡出;預設隱藏)
 │   ├─ CharacterPortrait  ← CharacterPortraitController(立繪置換)
 │   ├─ DialogueBoxPanel   ← NameBox / ContentBox(TMP 顯示)
 │   ├─ LogPanel           ← DialogueLogController + ScrollView(對話紀錄,預設隱藏)
 │   └─ BottomButtonBar
 │       ├─ FastForwardButton(快轉)─┐
 │       ├─ AutoButton(自動)────────┼─ BottomButtonController(開關型,各自掛 Animation)
 │       ├─ LogButton(紀錄)─────────┘
 │       └─ SkipButton(跳過)← 單發型 Button,淡出並結束對話
 └─ OpenButton             ← 範例入口,呼叫 TriggerDialogue.Btn_Open()

EventSystem = InteractionOfUI(繼承 EventSystem 的單例)+ StandaloneInputModule
```

資料流:

```
玩家點擊對話框 → TriggerDialogue.TextChange()(Raycast 判定)
              → DialogueTypingEffect.ToNextDialogue()
              → 閒置時翻頁 TriggerTyping() / 打字中則快速完成當前句
              → 對白播畢後再點擊 → OnDialogueFinished 事件 → 淡出結束

開關型按鈕按下 → TriggerDialogue.Btn_FastForward/Auto/Log()
            → BottomButtonController.OnButtonPressed()
            → 透過注入的 getState/setState 切換 DialogueTypingEffect 的狀態
            → 播放 Start→Loop 或 End 動畫

跳過按下 → TriggerDialogue.Btn_Skip()
        → 關閉快轉/自動模式 → DialogueGroup(CanvasGroup)淡出
        → DialogueTypingEffect.ToEndDialogue() 重置 → 收起對話 UI
```

---

## 三、各腳本使用方式

### 0. DialogueData(對話資訊表,ScriptableObject)

建立方式:**Assets 右鍵 → Create → Dialogue → 對話資訊表 (DialogueData)**

每行對白(`DialogueLine`)可設定:

| 欄位 | 說明 |
|---|---|
| `text` | 對白文本,可含停頓標記與 TMP 富文本標籤 |
| `speaker` | 說話的人物(`DialogueUnitType`;`Narration` = 旁白,自動隱藏立繪) |
| `displayName` | 顯示名稱,留空則顯示 speaker 的列舉名稱 |
| `expression` | 人物的表情差分(`CharacterExpressionType`,搭配立繪登錄表置換圖像) |
| `pauseDuration` | 此行 `\|pause\|` 的預設停頓秒數;0 = 用打字機全域的 `specialCharDelay` |

**停頓標記語法**(標記寫在文本中要停頓的位置):
- `|pause|` — 停頓,秒數依序取:該行 `pauseDuration` > 全域 `specialCharDelay`
- `|pause=1.5|` — 停頓指定秒數(此例 1.5 秒)

範例:`"接下來的路……|pause=1.5|會有些顛簸,準備好了嗎?"`

### 1. DialogueTypingEffect(打字機效果引擎)

掛載位置:`DialogueCanvas` 根物件。

| Inspector 欄位 | 說明 |
|---|---|
| `dialogueData` | **對話資訊表**,對白來源;也可在程式中呼叫 `SetDialogueData(data)` 切換 |
| `charaNameBox` / `dialogueContentBox` | 指向 NameBox / ContentBox 的 TMP_Text |
| `portraitController` | 人物立繪控制器(可為空,空則不顯示立繪) |
| `specialCharDelay` | `\|pause\|` 的全域預設停頓秒數 |
| `charactersPerSecond` | 打字速度(每秒字元數) |
| `pagePerSecond` | 自動模式的翻頁間隔秒數 |
| `fastSpeedup` | 快轉倍率 |

**文本語法**:
- 支援 TMP 富文本標籤,如 `<color=red>紅字</color>`(打字時整個標籤一次跳過,不會逐字顯示 `<c...`)
- 停頓標記見上方 DialogueData 說明

**主要公開方法**(可綁 UnityEvent):
- `SetDialogueData(DialogueData)`:更換對話資訊表(切換劇情段落)
- `ToStartDialogue()`:從資訊表第一行開始對話
- `ToNextDialogue()`:推進對話;打字中呼叫則快速完成當前句
- `ToFastForwardDialogue(bool)` / `ToAutoDialogue(bool)`:切換快轉/自動模式
- `GetCurrentlyFastForwarding()` / `GetCurrentlyAuto()`:讀取模式狀態
- `ToEndDialogue()`:立即結束對話並重置(由跳過的淡出流程呼叫)
- `OnDialogueFinished` 事件:對白播畢後玩家再點擊時觸發(TriggerDialogue 訂閱後執行淡出)

每次翻頁時會自動從資訊表讀取該行的文本、名稱、說話者與表情,並更新立繪。

### 2. TriggerDialogue(對話 UI 總控制器)

掛載位置:`DialogueCanvas` 根物件。負責:
- `Start()` 時把 `DialogueTypingEffect` 的狀態讀寫方法**注入**到各開關按鈕的 `BottomButtonController`,並訂閱 `OnDialogueFinished`
- `Update()` 監聽點擊對話框(只在點擊當幀做 UI Raycast)推進對話
- 提供給 Button OnClick 綁定的方法:`Btn_Open()`、`Btn_FastForward()`、`Btn_Auto()`、`Btn_Log()`、`Btn_Skip()`
- 「快轉」與「自動」互斥:開其中一個會自動關掉另一個
- **跳過 = 淡出結束**:`Btn_Skip()` 會關閉進行中的模式、將 `DialogueGroup`(CanvasGroup)在 `fadeOutDuration` 秒內淡出,然後呼叫 `ToEndDialogue()` 重置並收起對話 UI

| Inspector 欄位 | 說明 |
|---|---|
| `ContentTyper` | DialogueTypingEffect 引用 |
| `DialogueGroup` | 對話 UI 群組的 CanvasGroup(跳過時整組淡出) |
| `DialogueBoxPanel` | 對話框面板(其 Image 必須勾 Raycast Target,點擊判定用) |
| `LogUI` | 對話紀錄面板(開啟時不推進對話) |
| `FastForwardButton` / `AutoButton` / `LogButton` | 掛有 BottomButtonController 的開關按鈕 |
| `SkipButton` | 跳過按鈕(單發型,不掛 BottomButtonController) |
| `OpenButton` | 開始對話按鈕 |
| `fadeOutDuration` | 跳過時的淡出秒數(預設 0.6) |

### 3. BottomButtonController(底部按鈕通用控制器)

掛載位置:每顆底部按鈕上,**同物件需有 `Animation`(Legacy)組件**。

- 狀態不存在按鈕上,而是由外部(TriggerDialogue)注入 `getState` / `setState`,按鈕只負責「切換 + 播動畫」,因此同一個 Prefab 可以通用於任何開關型功能。
- 動畫名稱由欄位設定,預設對應產生的三個 clip:
  - 開啟:`Bottom_Btn_On_Start` → 播完自動接 `Bottom_Btn_On_Loop`(循環)
  - 關閉:`Bottom_Btn_On_End`
  - `loopAnim` 留空則開啟時只播 Start 不循環
- **新增一顆開關按鈕的步驟**:
  1. 拖 `BottomButton.prefab` 進 BottomButtonBar,改 Label 文字
  2. 在 TriggerDialogue(或你的控制器)中注入 `getState` / `setState`
  3. Button OnClick 綁定會呼叫 `OnButtonPressed()` 的方法

### 4. CharacterPortraitController(ADV 立繪置換)

掛載位置:`CharacterPortrait` Prefab 根物件。

- 在 Inspector 的 **Portraits** 清單登錄 `(角色 DialogueUnitType, 表情 CharacterExpressionType, Sprite)` 組合。
- 查找順序:完全符合 → 同角色 `Noexpression` → 同角色任一張 → `defaultSprite` → 全部沒有則維持現狀。
- 登錄第一張 Sprite 後,佔位提示會自動隱藏。
- 公開方法:`SetPortrait(unit, expression)`、`Show()`、`Hide()`(旁白時可呼叫 Hide)。
- `DialogueTypingEffect` 在設定對話內容時會自動呼叫 `SetPortrait`;文本為空時自動 `Hide`。

### 5. DialogueLogController(對話紀錄)

掛載位置:`LogPanel` 根物件。

- **記錄時機**:`DialogueTypingEffect` 每行對白開始顯示時自動呼叫 `AddEntry(名稱, 對白)`;對白已去除 `|pause|` 停頓標記,但**保留** TMP 富文本標籤(顏色等照常顯示)
- **清空時機**:`ToStartDialogue()` 開始新對話時自動 `Clear()`
- 條目由 `LogEntry.prefab` 實例化,放進 `Content`(VerticalLayoutGroup + ContentSizeFitter)由上到下排列,高度隨文字自適應
- 面板開啟或新增條目時自動捲動到最新一筆(`verticalNormalizedPosition = 0`)
- `nameColorHex` 欄位可調整名稱的顯示顏色(預設金色 `#F2D388`)

| Inspector 欄位 | 說明 |
|---|---|
| `scrollRect` / `contentRoot` / `entryPrefab` | 產生工具已自動綁定 |
| `nameColorHex` | 說話者名稱的顯示顏色 |

### 6. InteractionOfUI(EventSystem 單例)

範例場景的 EventSystem 使用此腳本(繼承 Unity `EventSystem`),額外追蹤「當前選取的 UI 物件」,跨場景不銷毀。搭配 `StandaloneInputModule` 使用(專案目前使用舊版 Input Manager)。

---

## 四、按鈕開關動畫規格

- 三個 clip 皆為 **Legacy** 動畫(`Animation` 組件 + `anim.Play(名稱)` 需要 legacy clip)。
- 動畫同時控制 **縮放**(Transform.localScale)與 **顏色**(Image.m_Color):
  - 關閉色 `#212129`(深灰藍)→ 開啟色 `#D9A633`(金色)
- 因為顏色交由動畫控制,按鈕的 `Transition` 已設為 `None`,避免 ColorTint 和動畫互搶。
- 想改動畫:直接改 `Assets/Animations/Dialogue/` 下的 clip,或換成自己的 clip 後修改 BottomButtonController 上的動畫名稱欄位。

---

## 五、已知待辦與擴充指引

| 項目 | 位置 | 說明 |
|---|---|---|
| ~~對話資料來源~~ | `DialogueData` | **已完成**:對白改由對話資訊表驅動,寫死文本已移除 |
| ~~對話結束流程~~ | `TriggerDialogue.EndDialogueWithFade()` | **已完成**:「跳過」按鈕或對白播畢後點擊 → 淡出並結束對話 |
| ~~對話紀錄內容~~ | `DialogueLogController` | **已完成**:每行對白自動記錄「名稱 + 對白」,ScrollView 捲動觀看 |
| 立繪素材 | `CharacterPortrait.prefab` | 目前為佔位圖,請在 CharacterPortraitController 登錄各角色/表情的 Sprite |
| 打字音效 | `DialogueTypingEffect` | 原程式碼中曾規劃 `TypingSound()`,需要時在打字迴圈的字元顯示處播放 AudioSource |

---

## 六、常見問題

- **按了按鈕沒有動畫?** 確認按鈕物件上有 `Animation` 組件且 clip 列表包含三個 `Bottom_Btn_On_*`,並且 clip 是 Legacy(產生工具已自動設定)。
- **點對話框沒反應?** 確認 `DialogueBoxPanel` 的 Image 勾了 **Raycast Target**、場景中有 EventSystem,且目前不在「自動 / 快轉」模式或淡出過程中(這些狀態下不接受手動點擊)。
- **TMP 文字不顯示?** 未匯入 TMP Essential Resources,由 Window → TextMeshPro → Import TMP Essential Resources 匯入。
- **中文顯示成 □□□?** 該 TMP 文字沒有使用中文字型。產生工具建立的 UI 已全部套用 `TaipeiSansTCBeta-Regular SDF`(Dynamic 動態圖集,用到的字才填入,不需預烘焙);自己新增的 TMP 文字請在 Font Asset 欄位選它。也可到 **Project Settings → TextMesh Pro → Settings** 把它設為 Default Font Asset 或加入 Fallback 清單,一勞永逸。
- **對話沒開始、Console 顯示「尚未設定對話資訊表」?** `DialogueTypingEffect` 的 `dialogueData` 欄位是空的,拖入一個 DialogueData 資產(範例:`Assets/Data/Dialogue/SampleDialogue.asset`)。
- **中文註解變亂碼?** 專案腳本已全部統一為 UTF-8 with BOM;新增腳本時請確認編輯器存檔編碼為 UTF-8。
