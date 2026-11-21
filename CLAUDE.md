# TUGJ0th

GameJam 2D 橫向平台遊戲。Unity 6000.3.27f2，目標平台 WebGL。原始需求與企劃想法在 [NoteOld.md](NoteOld.md)，標 (X) 的項目不做；[Note.md](Note.md) 是之後追加的更改清單。

## 工作方式
- 用繁體中文回覆。
- 框架保持簡單整潔，不過度優化。改完要說明改了哪些檔案、怎麼調整，方便使用者之後手動改。
- 新需求先用 grilling 對齊設計，使用者確認共識後才動手。
- Commit 在 `framework` 分支，不直接 commit 到 main。

## 架構 (Assets/Script)
- **Core**：`GameManager` 是遊戲場景的總管 (讀檔套用、存檔、死亡重載、地圖轉場 `TransitionTo`)；`GameInput` 集中所有按鍵綁定，改鍵位只改這裡。
- **Items**：`ItemDefinition` (ScriptableObject) 定義道具，右鍵生命週期是 `OnUseStart` → `OnUseHold` → `OnUseEnd`，由 `PlayerItemUser` 呼叫。`Inventory` 是純資料，UI 監聽 `Changed`。
- **Save**：`SaveData` 是一個存檔欄位的 JSON 內容，存在 PlayerPrefs，共 3 個欄位。每個節點 (`SavePoint`) 各有一份快照 (物品欄)，`currentNodeId` 是重生點。往前推進到節點才覆蓋快照，往回走只改重生點；地圖 (M) 點節點兩次可回溯。改格式要把 `SaveData.CurrentVersion` +1。
- **World**：節點 (隱形自動存檔)、致死區、MapArea/MapEdge (地圖分區與左右轉場)、黑洞、電池、壓力板、拉桿、門 (橫放 = 橋)、可破壞物件。
- **Player**：`PlayerController` 移動跳躍；`PlayerAnimator` 直接換 sprite 播逐格動畫 (不用 Animator)，圖來自 `Assets/Art/protagnist/pro-Sheet.png` (4 格 50x40，PPU 24)；`PlayerRocket` 火箭狀態與噴射粒子。
- **特效**：Unity ParticleSystem，共用材質 `Assets/Art/Placeholder/Particle.mat` (URP 2D Sprite-Unlit + 圓形貼圖)。火箭 = Player 的 `ThrustFx`，黑洞 = BlackHole 的 `PullFx`。
- **UI**：uGUI 的 legacy `Text`。
- **Editor/DevContentBuilder**：產生佔位美術與測試場景，見下方。

## 擴充慣例
- **新道具**：繼承 `ItemDefinition`。成功時呼叫 `ctx.Consume()`，用 `ctx.CheckUses()` 擋掉次數為 0 的情況。資產放在 `Assets/Resources/Items/` (讀檔靠 id 找)，要開局就有的話加進 GameManager 的 `startingItems`。
- **提示訊息**：道具相關 (名稱、失敗原因) 用 `ItemStatusUI.Hint`；系統訊息 (已存檔) 用 `ToastUI.Show`。
- **次數規則**：次數用完道具留在格子裡，顯示 0、圖示變灰。
- **地圖狀態不存檔**：電池、門、拉桿、可破壞物件在死亡重載後都會重置。遊戲沒有撿道具要素。
- **圖層**：Ground (layer 8) 放地形、電池、門、可破壞物件，也就是玩家能站、黑洞會貼上去的東西。觸發區用 layer 2 (Ignore Raycast)。
- **static 必須自己重置**：專案關閉了 Domain Reload，static 會延續到下一次 Play。新的 static 要在 `OnDestroy` 清掉，或用 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 重置，參考 `GameInput.ResetStatics`。
- **ID**：複製節點 (SavePoint) 後，要在 Inspector 執行 Regenerate ID。
- **黑洞拉力上限**：`maxPullSpeed` 必須小於火箭的 `riseSpeed` 和玩家的 `moveSpeed`，玩家才逃得掉。

## DevContentBuilder (Tools/TUGJ/Build Test Content)
- 會覆蓋 `Assets/Art/Placeholder`、`Assets/Prefabs`、Title/Game 場景、`pro-Sheet.png` 的切圖設定，並重新產生節點 ID，所以舊存檔會失效 (遊戲中按 F2 清除)。
- 美術換圖或手動編輯過關卡後就不要再執行。
- Tilemap 要先畫格子，再加 TilemapCollider2D 和 CompositeCollider2D，最後呼叫 `ProcessTilemapChanges()` 和 `GenerateGeometry()`。順序反了碰撞體會是空的，玩家會掉穿地板。
- `EditorSceneManager.NewScene` 之後，先前拿到的資產參照會失效，要用 `AssetDatabase.LoadAssetAtPath` 重新載入。

## Unity MCP
- 改完腳本後 Unity 會重新編譯。等 Unity 視窗標題沒有 Reloading / Compiling / Importing 再呼叫 MCP (用 PowerShell `Get-Process Unity` 看 `MainWindowTitle`)。回傳「Unity not detected」代表還在 reload。
- RunCommand 不能用 `System.Reflection`，也不能刪資產，要刪就從檔案系統刪，連同 `.meta` 一起。
- Unity 不在前景時 Play 模式不會推進，所以只能驗證結構和邏輯。手感、物理效果請使用者自己按 Play 測。
- `Capture2DScene` 的座標原點在左下角。

## 尚未處理
- WebGL 上 legacy 字型顯示不了中文，需要加入 CJK 字型。
- Build 平台還是 Windows，還沒切到 WebGL。
- 二周目機制 (Note.md 的詳細記錄) 還沒做。
