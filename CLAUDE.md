# TUGJ0th

GameJam 2D 橫向平台遊戲。Unity 6000.3.27f2，目標平台 WebGL。原始需求與企劃想法在 [NoteOld.md](NoteOld.md)，標 (X) 的項目不做；[Note.md](Note.md) 是之後追加的更改清單。

## 工作方式
- 用繁體中文回覆。
- 框架保持簡單整潔，不過度優化。改完要說明改了哪些檔案、怎麼調整，方便使用者之後手動改。
- 新需求先用 grilling 對齊設計，使用者確認共識後才動手。
- Commit 在 `framework` 分支，不直接 commit 到 main。

## 架構 (Assets/Script)
- **Core**：`GameManager` 是遊戲場景的總管 (讀檔套用、存檔、死亡重載、R 鍵回到重生點 `RestartAtNode`、地圖轉場 `TransitionTo`、通關 `ClearGame`)；`GameInput` 集中所有按鍵綁定，改鍵位只改這裡。
- **Items**：`ItemDefinition` (ScriptableObject) 定義道具，右鍵生命週期是 `OnUseStart` → `OnUseHold` → `OnUseEnd`，由 `PlayerItemUser` 呼叫。`Inventory` 是純資料，UI 監聽 `Changed`。
- **Save**：`SaveData` 是一個存檔欄位的 JSON 內容，存在 PlayerPrefs，共 3 個欄位。每個節點 (`SavePoint`) 各有一份快照 (物品欄)，`currentNodeId` 是重生點。往前推進到節點才覆蓋快照，並刪掉它後面所有節點的快照 (新時間線，後面的關卡不能再回溯)；往回走只改重生點；地圖 (M) 點節點兩次可回溯。改格式要把 `SaveData.CurrentVersion` +1。
- **二周目**：通關點 `ClearGoal` (按 E) → 一周目時記錄玩家生成的電池位置 (`Battery.SpawnedByPlayer`，吊著的也算) 到 `SaveData.batteryRecords`、`loop = 2`、清空所有節點紀錄、從 PlayerStart 重來 (回不去一周目)。二周目開局多 `GameManager.secondLoopItems` (請輸入文本 `RecallItem`)。區段 = `MapArea` (用物件名稱當 Id，不要重複)；各區段是否用過存在節點快照的 `recallUsedAreas`，死亡會恢復。殘影 = `BatteryGhosts` (掛在 GameManager 物件)。
- **World**：節點 (隱形自動存檔)、致死區、MapArea/MapEdge (地圖分區與左右轉場)、黑洞、電池、壓力板、拉桿、門 (橫放 = 橋)、軌道平台、無人機、可破壞物件。
  - **機關**：門和軌道平台繼承 `Activatable`，拉桿 / 壓力板的 `targets` 連到它們 (壓著 = 啟動，拉桿 = 翻轉)。新機關也繼承它。
  - **無人機**：`Drone.Powered` (有電的那台，最多一台)、`Drone.Controlled` (操控中)。操控時玩家不吃移動 / E 輸入 (`PlayerController.MoveInput`)、鏡頭改跟無人機 (`GameManager.SetCameraTarget`)。操控由 `PlayerItemUser` 依選中的格子決定：選中無人機格子且有 `Drone.Powered` → 操控；切到別格 → 暫停 (懸停)；操控中右鍵沒有作用。生成位置同電池 (`SpawnCell`)。無人機本體跟玩家、電池的碰撞用 `Physics2D.IgnoreCollision` 關掉 (`Drone.All` / `Battery.All`)；吊著的電池仍跟玩家碰撞，玩家可以站上去 (被帶著水平移動)，站滿 `crashTime` 秒 → 墜機 (`Deplete`)。外觀：`openSprite` 待機、`flyFrames` 操控中、`closeSprite` + 黑白材質 = 沒電。吊著的電池是 Kinematic，不會被物理擋住，由無人機自己做投射檢查；吊起時正下方被擋住就維持原本的相對位置 (`carryLocal`)，避免把電池塞進牆裡。
  - **傳送槍**：`TeleportBullet` 被黑洞當加速度吸引 (`BlackHole.bulletPull`)；只有撞到電池才傳送 (放不下就找附近最近放得下玩家的格子)；撞到其他 Ground、停滯 `stallTime` 秒、飛超過 5 秒 → 消散 (`vanishFx`)。提示線會打到電池時變綠色 (`GetAimPath` 的 `highlight`)。射出就扣次數，飛行中右鍵取消。
  - **推電池**：不靠物理碰撞推，`PlayerController.PushBattery` 偵測前方的電池 → `Battery.Push`：電池沿腳下地面 / 前方斜坡移動 (推動中不受重力、水平速度跟玩家一致)，前面有電池就一起推，懸空時交給物理。停止推就歸零速度。玩家站在電池上時，電池被移動 (黑洞、平台、物理) 玩家會跟著水平移動 (`PlayerController.GroundCarry`，比對電池上一步的位移；吊著的電池由 `Drone.CarryRider` 處理)。
  - **固有電池**：`FixedBattery.prefab` (Battery 的變體，偏灰色)，直接擺在場景裡。`rocketProof` = 火箭尾氣不會引爆，其他行為跟一般電池一樣；不是 `SpawnedByPlayer`，所以不會被記錄進二周目。
  - **軌道平台**：`MovingPlatform` 自己搬動站在上面的東西和被推的東西；被推的東西前面的東西 (例如疊著的電池) 會一起推 (`CollectChain`)，整串推不動 → 整串被擠壓：玩家死亡、電池爆炸、無人機摧毀。
  - **電池爆炸**：`Battery.Explode` (平台擠壓、連鎖、請輸入文本重疊 → 立刻爆炸)。火箭尾氣 (`PlayerRocket.exhaustSize` 範圍) 只會點燃引信 `Battery.StartFuse`：閃紅 (`fuseColor`、`fuseFlashRate`) + 頭上 TextMesh 倒數 + 火花 `fuseFx`，`fuseTime` (1.5) 秒後爆炸。範圍內的電池延遲 `chainDelay` 連鎖爆炸；擊退 = `maxKnockback` × (1 − 距離/半徑)。玩家用 `PlayerController.AddKnockback` (外力速度，跟黑洞拉力一起疊加、`knockbackTime` 內線性衰減、不留慣性)；沒電的無人機直接加到剛體速度 (沒有時間衰減，交給重力與摩擦)。不傷害玩家；半徑內的可破壞方塊會被破壞 (隔牆也會)。
- **Player**：`PlayerController` 移動跳躍；`PlayerAnimator` 直接換 sprite 播逐格動畫 (不用 Animator)，圖來自 `Assets/Art/protagnist/pro-Sheet.png` (4 格 50x40，PPU 24，pivot 離底 19px)，空中用 `jump-Sheet.png` (7 格 50x50，依垂直速度選格，pivot 也離底 19px 讓腳對齊)；`AimLine` 瞄準提示線 (問 `ItemDefinition.GetAimPath`)；`SpawnPreview` 生成位置預覽 (道具實作 `ISpawnPreviewItem`：電池倉、無人機；位置規則在 `SpawnCell`：只能放玩家腳邊左 / 右緊鄰的那一格 (看滑鼠在哪一邊)、被擋住或隔牆就不能放、玩家在空中不能生成也不顯示預覽 `SpawnCell.CanSpawnNow`)；`PlayerRocket` 火箭狀態與噴射粒子。
- **特效**：Unity ParticleSystem，共用材質 `Assets/Art/Placeholder/Particle.mat` (URP 2D Sprite-Unlit + 圓形貼圖)。火箭 = Player 的 `ThrustFx`，黑洞 = BlackHole 的 `PullFx`。一次性特效放 `Assets/Prefabs/Fx/` (stopAction = Destroy，直接 Instantiate)：生成電池、傳送 / 子彈消散、落地、走路 (`PlayerAnimator.walkFxFrames`)、無人機墜機、請輸入文本、磚塊破壞 (`Breakable.breakFx`)；循環的：殘影光點、通關點閃光、電池引信火花 (`FuseFx`)。傳送子彈有 TrailRenderer 拖尾。
- **UI**：uGUI 的 legacy `Text`。
- **黑白效果**：`Assets/Art/Shaders/Grayscale.mat` (shader `TUGJ/Grayscale`)，UI Image 和 SpriteRenderer 共用。
- **Editor/DevContentBuilder**：產生佔位美術與測試場景，見下方。

## 擴充慣例
- **新道具**：繼承 `ItemDefinition`。成功時呼叫 `ctx.Consume()`，用 `ctx.CheckUses()` 擋掉次數為 0 的情況。資產放在 `Assets/Resources/Items/` (讀檔靠 id 找)，要開局就有的話加進 GameManager 的 `startingItems`。可覆寫 `ShowUses` (要不要顯示次數)、`IsAvailable` (false = 圖示黑白)、`GetAimPath` (瞄準提示線)。
- **提示訊息**：道具相關 (名稱、失敗原因) 用 `ItemStatusUI.Hint`；系統訊息 (已存檔) 用 `ToastUI.Show`。
- **次數規則**：次數用完道具留在格子裡，顯示 0、圖示變黑白。
- **地圖狀態不存檔**：電池、門、拉桿、平台、無人機、可破壞物件在死亡重載後都會重置；切換地圖時無人機全部消失。遊戲沒有撿道具要素。
- **圖層**：Ground (layer 8) 放地形、電池、門、平台、可破壞物件，也就是玩家能站、黑洞會貼上去的東西。無人機在 Default (黑洞會穿過)。觸發區用 layer 2 (Ignore Raycast)。
- **static 必須自己重置**：專案關閉了 Domain Reload，static 會延續到下一次 Play。新的 static 要在 `OnDestroy` 清掉，或用 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 重置，參考 `GameInput.ResetStatics`。
- **ID**：複製節點 (SavePoint) 後，要在 Inspector 執行 Regenerate ID。
- **黑洞吸力**：全部在 `BlackHole.prefab` 調：`playerPull` (玩家)、`objectPull` (電池、沒電的無人機)、`bulletPull` (傳送子彈，加速度)。呼叫端用 `BlackHole.SamplePull(位置, PullTarget, out strength)`。`playerPull` 必須小於火箭的 `riseSpeed` 和玩家的 `moveSpeed`，玩家才逃得掉。

## DevContentBuilder (Tools/TUGJ/Build Test Content)
- 會覆蓋 `Assets/Art/Placeholder`、`Assets/Prefabs`、Title/Game 場景、`pro-Sheet.png` 的切圖設定，並重新產生節點 ID，所以舊存檔會失效 (遊戲中按 F2 清除)。
- 美術換圖或手動編輯過關卡後就不要再執行。
- Tilemap 要先畫格子，再加 TilemapCollider2D 和 CompositeCollider2D，最後呼叫 `ProcessTilemapChanges()` 和 `GenerateGeometry()`。順序反了碰撞體會是空的，玩家會掉穿地板。
- `EditorSceneManager.NewScene` 之後，先前拿到的資產參照會失效，要用 `AssetDatabase.LoadAssetAtPath` 重新載入。

## Unity MCP
- 改完腳本後 Unity 會重新編譯。等 Unity 視窗標題沒有 Reloading / Compiling / Importing 再呼叫 MCP (用 PowerShell `Get-Process Unity` 看 `MainWindowTitle`)。回傳「Unity not detected」代表還在 reload。
- RunCommand 不能用 `System.Reflection`，也不能刪資產，要刪就從檔案系統刪，連同 `.meta` 一起。
- Unity 不在前景時 Play 模式不會推進，所以只能驗證結構和邏輯。手感、物理效果請使用者自己按 Play 測。
- `Capture2DScene` 的座標原點在左下角。白色的佔位 sprite (拉桿、壓力板) 在白底截圖上看不到，顏色是執行時才套上的。
- 跑 builder：先確認 `EditorSceneManager.GetActiveScene().isDirty == false` (否則會跳存檔對話框卡住)，再 `EditorApplication.ExecuteMenuItem("Tools/TUGJ/Build Test Content")`。
- 確認新程式已編譯：比對 `Library/ScriptAssemblies/Assembly-CSharp.dll` 與改過的 .cs 的修改時間，或用 `typeof(X).GetMethod("公開方法")` 檢查 (不能 using System.Reflection)。

## 尚未處理
- WebGL 上 legacy 字型顯示不了中文，需要加入 CJK 字型。
- Build 平台還是 Windows，還沒切到 WebGL。
- 通關點、請輸入文本的圖示還是佔位圖。
