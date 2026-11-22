# TUGJ0th

GameJam 2D 橫向平台遊戲。Unity 6000.3.27f2，目標平台 WebGL。原始需求與企劃想法在 [NoteOld.md](NoteOld.md)，標 (X) 的項目不做；[Note.md](Note.md) 是之後追加的更改清單。[LevelDesign_README.md](LevelDesign_README.md) 是給企劃手動排關卡的說明，改到關卡相關的物件 (Tilemap、MapArea/MapEdge、機關欄位、道具規則) 時要一起更新。

## 工作方式
- 用繁體中文回覆。
- 框架保持簡單整潔，不過度優化。改完要說明改了哪些檔案、怎麼調整，方便使用者之後手動改。
- 新需求先用 grilling 對齊設計，使用者確認共識後才動手。
- Commit 在 `framework` 分支，不直接 commit 到 main。

## 架構 (Assets/Script)
- **Core**：`GameManager` 是遊戲場景的總管 (讀檔套用、進房間存檔 `EnterRoom`、讀檔 `RewindTo`、死亡重載、R 鍵回到重生點 `RestartAtNode`、地圖轉場 `TransitionTo`、通關 `ClearGame`、二周目 `Recall`)；`GameInput` 集中所有按鍵綁定，改鍵位只改這裡 (選單用的 `MenuMove` / `SubmitPressed` / `CancelPressed` 不受 `Locked` 影響)。
- **Items**：`ItemDefinition` (ScriptableObject) 定義道具，右鍵生命週期是 `OnUseStart` → `OnUseHold` → `OnUseEnd`，由 `PlayerItemUser` 呼叫。`Inventory` 是純資料，UI 監聽 `Changed`。
- **Save**：`SaveSystem` 只存在記憶體 (static，重新整理網頁 / 重按 Play 就清空)，兩個存檔：`Save1` = 一周目、`Save2` = 二周目，都是自動存檔。標題畫面的存檔視窗：new save (已有存檔要按兩次，清掉兩個重來) / save1 / save2 (存在才能選)。存檔單位是**房間** (`MapArea`，依 bounds 左邊界 x 排序 = room1, room2… `GameManager.Rooms`)：經過 MapEdge 進入房間就存檔，`RoomSnapshot` = 物品欄 + 重生位置 (轉場落點；room1 = PlayerStart) + `recallUsedAreas`。往前推進才覆蓋快照並刪掉後面房間的快照 (新時間線)。**只能往右走**：右邊房間往回的 MapEdge 不設 target → 執行時變成實心牆 (Ground)，回去只能用終端機讀檔。沒有 SavePoint 物件。
- **場景**：遊戲場景是 `Game_EGG` (`SceneNames.Game`，企劃排好的關卡)；`Game.unity` 是舊的測試場景。
- **最後的房間** (`GameManager.FinalRoom` = 最右邊的 MapArea，room 6)：轉場進來 / 讀檔重載到這裡 → `ArriveFinalRoom`：一周目 = `ClearGame` (播 `clearLoop1` → 二周目)；二周目 = 進房間那一刻 `RecordTargets` 結算 `TrueEndGate` 的條件 (名稱 `[Target]` 開頭的 Lever / PressurePlate，`On` / `Pressed` 觸發中才算) 存進 room 6 快照的 `targetResults`，全部達成 → 門打開 (`Activatable.SetActivated`)，門後的 ClearGoal = 真結局 (暫時 = BE)；沒達成 → 直接 `ClearGame` = BE。美術元件查 `TrueEndGate.Instance` (`IsRecorded` / `IsTriggeredNow` / `Changed`)。
- **二周目**：二周目通關 → `GameManager.EndingRoutine`：先播結局劇情 `EndingUI.PlayStory` (全螢幕 CG `Assets/Art/cg` + 字幕 `Ending/Root/CG/Caption`：另一個 `DialogueUI`，外觀是底部黑色漸層 `ending_gradient.png` + 白字，不用對話框；劇本在 GameManager 的 `endingStory`，單獨一行 `cg_N` = 換成名稱 CGN 的圖 (`EndingUI.cgSprites`)、cg 底下沒句子 = 只看圖按一下繼續、`【end】` = 結束) → 淡出淡入 → 播 end 動畫 `EndingUI.Play` (`Assets/Art/end`，8 fps 循環播放、第一輪播完後閃「Press any key」→ 回標題)。一周目通關 (`ClearGame`) → 記錄玩家生成的電池位置 (通關當下，`Battery.SpawnedByPlayer`，吊著的也算)，播 `clearLoop1` 劇情後建立全新的 save2 (`loop = 2`、`batteryRecords`)，切到 save2 從 PlayerStart 開始。save1 保留，之後可從標題讀 save1 重玩一周目 (再通關會覆蓋 save2)。transform = 終端機右半邊，對玩家所在房間呼叫 `GameManager.Recall` (生成位置重疊 → 爆炸)，每個房間一次，紀錄存在快照的 `recallUsedAreas`，死亡會恢復。殘影 = `BatteryGhosts` (掛在 GameManager 物件)，一直顯示到該房間用過 transform。
- **World**：致死區、MapArea/MapEdge (房間與左右轉場)、黑洞、電池、壓力板、拉桿、門 (橫放 = 橋)、軌道平台、無人機、可破壞物件。
  - **機關**：門和軌道平台繼承 `Activatable`，拉桿 / 壓力板的 `targets` 連到它們 (壓著 = 啟動，拉桿 = 翻轉)。新機關也繼承它。
  - **門**：逐格動畫 `frames` (直) / `lyingFrames` (橫，勾 `horizontal` = 橋)，圖在 `Assets/Art/Tiles/door_*` (第 1 格關、第 9 格開，PPU 16)。打開時碰撞立刻消失、動畫往後播；關閉等淨空才開碰撞、動畫往回播。長度固定 4 格，碰撞體厚 `thickness`，不用 Scale；橋的 sprite pivot 讓頂面貼齊格子頂端 (位置 y + 0.5)。
  - **無人機**：`Drone.Powered` (有電的那台，最多一台)、`Drone.Controlled` (操控中)。操控時玩家不吃移動 / E 輸入 (`PlayerController.MoveInput`)、鏡頭改跟無人機 (`GameManager.SetCameraTarget`)。操控由 `PlayerItemUser` 依選中的格子決定：選中無人機格子且有 `Drone.Powered` → 操控；切到別格 → 暫停 (懸停)；操控中右鍵沒有作用。生成位置同電池 (`SpawnCell`)。無人機本體跟玩家、電池的碰撞用 `Physics2D.IgnoreCollision` 關掉 (`Drone.All` / `Battery.All`)；吊著的電池仍跟玩家碰撞，玩家可以站上去 (被帶著水平移動)，一踩上去就開始倒數 `crashTime` 秒 (離開也不取消)：`warnFx` (`Prefabs/Fx/DroneWarnFx`，火花 + 黑煙) + 閃 `warnColor`，時間到 → `crashFx` + 墜機 (`Deplete`)。外觀：`openSprite` 待機、`flyFrames` 操控中、`closeSprite` + 黑白材質 = 沒電。吊著的電池是 Kinematic，不會被物理擋住，由無人機自己做投射檢查；吊起時電池一律移到正下方 `carryOffset`，那裡被擋住 → 無人機往上移 (最多 `maxLift` 格)，還是不行 → 提示「目前空間不能搬運電池」(`TryPickUp`)。吊著的電池一樣會爆炸，爆炸時 `Drone.ReleaseCarried` (無人機仍有電)。
  - **傳送槍**：`TeleportBullet` 被黑洞當加速度吸引 (`BlackHole.bulletPull`)；只有撞到電池才傳送 (放不下就找附近最近放得下玩家的格子)；撞到其他 Ground、停滯 `stallTime` 秒、飛超過 5 秒 → 消散 (`vanishFx`)。提示線會打到電池時變綠色 (`GetAimPath` 的 `highlight`)。射出就扣次數，飛行中右鍵取消。
  - **推電池**：不靠物理碰撞推，`PlayerController.PushBattery` 偵測前方的電池 → `Battery.Push`：電池沿腳下地面 / 前方斜坡移動 (推動中不受重力、水平速度跟玩家一致)，前面有電池就一起推，懸空時交給物理。停止推就歸零速度。玩家站在電池上時，電池被移動 (黑洞、平台、物理) 玩家會跟著水平移動 (`PlayerController.GroundCarry`，比對電池上一步的位移；吊著的電池由 `Drone.CarryRider` 處理)。
  - **固有電池**：`FixedBattery.prefab` (Battery 的變體，偏灰色)，直接擺在場景裡。`rocketProof` = 火箭尾氣不會引爆，其他行為跟一般電池一樣；不是 `SpawnedByPlayer`，所以不會被記錄進二周目。
  - **軌道平台**：`MovingPlatform` 自己搬動站在上面的東西和被推的東西；被推的東西前面的東西 (例如疊著的電池) 會一起推 (`CollectChain`)，整串推不動 → 整串被擠壓：玩家死亡、電池爆炸、無人機摧毀。
  - **電池爆炸**：`Battery.Explode` (平台擠壓、連鎖、transform 生成時重疊 → 立刻爆炸)。火箭尾氣 (`PlayerRocket.exhaustSize` 範圍) 只會點燃引信 `Battery.StartFuse`：閃紅 (`fuseColor`、`fuseFlashRate`) + 頭上 TextMesh 倒數 + 火花 `fuseFx`，`fuseTime` (1.5) 秒後爆炸。範圍內的電池先被擊退 (吊著的不會)、延遲 `chainDelay` 連鎖爆炸。擊退在 `Knockback.Apply`：上下疊在一起的 (A 站在 B 頂面) 分成一組，整組用組中心算的同一個速度 = `maxKnockback` × (1 − 距離/半徑)，才會一起飛。玩家用 `PlayerController.AddKnockback` (外力速度，跟黑洞拉力一起疊加、`knockbackTime` 內線性衰減、不留慣性；被炸飛中不吃 `GroundCarry`)；電池、沒電的無人機也是速度一次加上去、再用 `Knockback.Decay` 在同樣的 `knockbackTime` 內扣掉 (水平扣到 0 為止，撞牆不反彈)，軌跡跟玩家一致。不傷害玩家；半徑內的可破壞方塊會被破壞 (隔牆也會)。
- **Player**：`PlayerController` 移動跳躍；`PlayerAnimator` 直接換 sprite 播逐格動畫 (不用 Animator)，圖來自 `Assets/Art/protagnist/pro-Sheet.png` (4 格 50x40，PPU 24，pivot 離底 19px)，空中用 `jump-Sheet.png` (7 格 50x50，依垂直速度選格，pivot 也離底 19px 讓腳對齊)；拿槍：選中傳送槍 / 黑洞槍 → 播 `gun1-Sheet` / `gun2-Sheet` (拿槍，9 格) 一次後停在最後一格，射擊成功時道具呼叫 `PlayerAnimator.PlayShot` 播 `gun*-shot-Sheet` (4 格)，拿槍站著 / 射擊時面向滑鼠；拿槍走路、拿槍在空中 → 循環 `gun*-walk-Sheet` (4 格，面向移動方向，不播拿槍動畫)；操控無人機 → `pad-take-Sheet` (6 格) 一次 → 循環 `pad-using-Sheet` (4 格)。沒拿槍時空中用 jump。優先順序：空中 (沒拿槍) > 平板 > 射擊 > 拿槍走路 / 拿槍空中 > 走路 > 拿槍 > 站立。這些 50x50 的 sheet 都是 PPU 24、pivot (0.5, 0.38)；`AimLine` 瞄準提示線 (問 `ItemDefinition.GetAimPath`)；`SpawnPreview` 生成位置預覽 (道具實作 `ISpawnPreviewItem`：電池倉、無人機；位置規則在 `SpawnCell`：只能放玩家腳邊左 / 右緊鄰的那一格 (看滑鼠在哪一邊)、被擋住或隔牆就不能放、玩家在空中不能生成也不顯示預覽 `SpawnCell.CanSpawnNow`)；`PlayerRocket` 火箭狀態與噴射粒子。
- **特效**：Unity ParticleSystem，共用材質 `Assets/Art/Placeholder/Particle.mat` (URP 2D Sprite-Unlit + 圓形貼圖)。火箭 = Player 的 `ThrustFx`，黑洞 = BlackHole 的 `PullFx`。一次性特效放 `Assets/Prefabs/Fx/` (stopAction = Destroy，直接 Instantiate)：生成電池、傳送 / 子彈消散、落地、走路 (`PlayerAnimator.walkFxFrames`)、無人機墜機、transform 生成 (`RecallSpawnFx`)、磚塊破壞 (`Breakable.breakFx`)；循環的：殘影光點、通關點閃光、電池引信火花 (`FuseFx`)。傳送子彈有 TrailRenderer 拖尾。
- **UI**：uGUI 的 legacy `Text`，字型一律用 `Assets/Art/Font/Cubic_11.ttf` (像素字型，含中文；Rendering Mode = Hinted Raster)。新增文字記得換字型 (`TerminalUI` / `TitleMenu` 的 `font` 欄位、`Battery.fuseFont` 也是)。
  - **工作列** (`Canvas/Taskbar`，圖 `Assets/Art/UI_item/UI.png`，貼齊底部、`AspectRatioFitter` 保持比例)：子物件用 anchors 對齊圖上的像素 (422x18)。最左的 `TerminalButton` 也是一格 `InventorySlotUI` (物品欄第 1 格 = 電腦道具 `TerminalItem`，`Resources/Items/Terminal.asset`：數字鍵 1、右鍵打開終端機；同物件上的 Button 讓左鍵點擊 = 選取 + 打開；`Pinned` = 固定不能拖曳交換；圖畫在工作列底圖上所以 icon 留空)；`Slot1~5` = 一般道具 (數字鍵 2~6)。`InventoryUI.slots` 直接拖場景裡的格子 (不再用 prefab 生成)，選中框 = `UI_select.png`。物品欄 6 格 (`Inventory.slotCount`，在 Player.prefab)，`startingItems` 第一個是 Terminal。次數 `Uses` = 字級 34 + 黑色 `Outline`。
  - **終端機** (`Canvas/Terminal` 上的 `TerminalUI`，取代舊的 M 地圖，背景 `SavePage.png`)：M 鍵 / 工作列圖示開關，開著時暫停。Panel 1677x1053、背景 alpha 0.97、字級 42 / 行高 60。左上 `[settings]` → 音量 (`GameAudio`) / exit / back。文字用英文 (WebGL 字型問題)。
  - **綠字選單** `TerminalMenu` (純 C# 類別，終端機和標題共用)：`Clear` → `Add` 數行 → `EndBuild`，每行是程式產生的 `TerminalRow` (Text)，放進 VerticalLayoutGroup；滑鼠 hover / click、WASD + Enter (`HandleInput`)。
  - **標題** (`TitleMenu`)：背景 `UIFrameAnimation` 循環播 `Assets/Art/main` (8 fps，開始的字畫在背景上，沒有另外的提示文字)，上面疊角色 `Background/Character` (另一個 `UIFrameAnimation`，循環 `main_character_0001~0016`)；點擊 / Enter / Space → `SavePopup` (SavePage 視窗，尺寸 / 字級跟終端機一致)：new save (已有存檔按兩次) / save1 / save2，× 或 Esc 關閉。選好存檔 → 關視窗、角色換播 `TitleMenu.departFrames` (`main_character2_0001~0009`) 一次 + `departSfx` → 播完才淡出進遊戲。
  - **劇情對話** (`Canvas/Dialogue` 上的 `DialogueUI`，框 `dialogue.png` ×5 放在畫面下方)：打字機 + 每個字播 `chat.wav` (隨機音高)；Space / Enter / E / 左鍵 = 打字中顯示整句、否則下一句。文本在 GameManager (一行一句)：`introLoop1` = 一周目開頭，`ApplySaveData` 走到「沒有快照」的一周目新遊戲才播 (`IntroRoutine`)，讀檔、死亡重載不播，播放中 `GameInput.Locked` (世界照跑) 且 `Busy` (不能開終端機)；`clearLoop1` = 一周目通關按 E 時原地播 (`ClearLoop1Routine`，玩家停住)，播完才重載進二周目；二周目開頭沒有劇情。
  - **逐格 UI 動畫** `UIFrameAnimation` (Image 換 sprite，unscaled time，loop 或播一次 `Finished`，一輪秒數 `Duration`)。
- **Audio**：`GameAudio` (static 音量 0~10，只在記憶體)、`MusicPlayer` (場景根物件 `Music` 上、指定 clip；BGM 在 `Assets/Audios/BGM/`：Title = `menu`、Game_EGG / Game = `inGame`。跨場景保留：下個場景同一首就繼續播 (死亡重載不會重播)，不同首才換)、`Sfx.Play(clip)`。音檔放 `Assets/Audios/SFX/`。音效一律是元件 / 道具上的 `AudioClip xxxSfx` 欄位 + `Sfx.Play`：跳躍 `PlayerController.jumpSfx`、走路 `PlayerAnimator.walkSfx` (跟 `walkFxFrames` 同一格，`Sfx.PlayPitched` 隨機音高 `walkSfxPitch`)、死亡 `GameManager.dieSfx`、爆炸 / 引信倒數 `Battery.explosionSfx` / `fuseTickSfx` (每 `fuseTickInterval` 秒一聲)、射擊 `TeleportGunItem` / `BlackHoleGunItem.shootSfx`、放電池 `BatteryItem.spawnSfx`、傳送 `TeleportBullet.teleportSfx`、拉桿 `Lever.pullSfx`、選單按下 `TerminalUI` / `TitleMenu.clickSfx` (傳給 `TerminalMenu`)、對話 `DialogueUI.voice`。持續的音效用 `LoopSfx` 元件 (循環、跟著音量設定、`Time.timeScale = 0` 時暫停、可依跟玩家的距離衰減 `fullVolumeDistance` / `silentDistance`)：火箭飛行中 `PlayerRocket.thrustSfx` (Player 上)、黑洞固定後 `BlackHole.pullSfx` (BlackHole.prefab 上)。
- **黑白效果**：`Assets/Art/Shaders/Grayscale.mat` (shader `TUGJ/Grayscale`)，UI Image 和 SpriteRenderer 共用。
- **Editor/DevContentBuilder**：產生佔位美術與測試場景，見下方。

## 擴充慣例
- **新道具**：繼承 `ItemDefinition`。成功時呼叫 `ctx.Consume()`，用 `ctx.CheckUses()` 擋掉次數為 0 的情況。資產放在 `Assets/Resources/Items/` (讀檔靠 id 找)，要開局就有的話加進 GameManager 的 `startingItems`。可覆寫 `ShowUses` (要不要顯示次數)、`IsAvailable` (false = 圖示黑白)、`Pinned` (固定在格子裡不能拖曳交換)、`GetAimPath` (瞄準提示線)。
- **提示訊息**：道具相關 (名稱、失敗原因) 用 `ItemStatusUI.Hint`；系統訊息 (已存檔) 用 `ToastUI.Show`。
- **次數規則**：次數用完道具留在格子裡，顯示 0、圖示變黑白。
- **地圖狀態不存檔**：電池、門、拉桿、平台、無人機、可破壞物件在死亡重載後都會重置；切換地圖時無人機全部消失。遊戲沒有撿道具要素。
- **圖層**：Ground (layer 8) 放地形、電池、門、平台、可破壞物件，也就是玩家能站、黑洞會貼上去的東西。無人機在 Default (黑洞會穿過)。觸發區用 layer 2 (Ignore Raycast)。
- **static 必須自己重置**：專案關閉了 Domain Reload，static 會延續到下一次 Play。新的 static 要在 `OnDestroy` 清掉，或用 `[RuntimeInitializeOnLoadMethod(SubsystemRegistration)]` 重置，參考 `GameInput.ResetStatics`。
- **斜坡 tile**：Collider Type = Sprite，sprite 要設精確的三角形 Custom Physics Shape (`floor_03` / `floor_05` 已設)；Grid 會變方形，自動描邊會是階梯。
- **黑洞吸力**：全部在 `BlackHole.prefab` 調：`playerPull` (玩家)、`objectPull` (電池、沒電的無人機)、`bulletPull` (傳送子彈，加速度)。呼叫端用 `BlackHole.SamplePull(位置, PullTarget, out strength)`。`playerPull` 必須小於火箭的 `riseSpeed` 和玩家的 `moveSpeed`，玩家才逃得掉。

## DevContentBuilder (Tools/TUGJ/Build Test Content)
- 會覆蓋 `Assets/Art/Placeholder`、`Assets/Prefabs`、Title/Game 場景、`pro-Sheet.png` 的切圖設定。
- **已過時，不要再執行**：關卡已手動編輯，而且它沒有跟著更新終端機、工作列、房間存檔 (只維持能編譯)。
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
- Build 平台還是 Windows，還沒切到 WebGL。
- 通關點的圖示還是佔位圖。
- 機關 / 無人機 / 落地等音效還沒做 (`Sfx` 已就緒)。
