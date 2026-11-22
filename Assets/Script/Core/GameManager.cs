using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 遊戲場景的總管 (每個 Game 場景一個)。負責：
/// - 進場時讀檔並套用 (玩家位置 = 目前房間的重生點、物品欄 = 目前房間的快照)
/// - 自動存檔：每進入一個房間 (MapArea，經過 MapEdge 轉場) 就存檔 (EnterRoom)；讀檔 = 終端機點房間 (RewindTo)
/// - 死亡 → 淡出 → 重新載入場景 (= 回到目前房間的快照)；R 鍵 (RestartAtNode) 也一樣
/// - 通關 (ClearGame)：一周目 → 記錄電池位置、建立 save2、從頭開始二周目；二周目 → 播 end 動畫 → 回標題
/// - 最後的房間 (room 6，FinalRoom)：一周目進來再往右走 loop1ClearDistance 格 = 通關 (播 clearLoop1)；二周目進來 = 結算 [Target] 條件 (TrueEndGate)，
///   全部達成 → 開門 (門後的 ClearGoal = 真結局，暫時跟 BE 一樣)，沒達成 → 直接播 BE
/// - 二周目 transform (終端機右半邊)：在目前房間生成一周目記錄的電池，每個房間一次，使用紀錄跟著快照存
/// 其他腳本透過 GameManager.Instance 呼叫。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("場景參照")]
    public PlayerController player;
    [Tooltip("新遊戲 (還沒存過檔) 的出生點，要放在 room1 裡")]
    public Transform playerStart;
    [Tooltip("沒有 MapArea 時用來計算世界範圍的 Tilemap")]
    public Tilemap boundsTilemap;
    public ScreenFader fader;
    [Tooltip("跟隨玩家的鏡頭 (操控無人機時改跟無人機)")]
    public CinemachineCamera vcam;
    [Tooltip("二周目通關時播的 end 動畫")]
    public EndingUI ending;
    [Tooltip("最後房間 (room 6) 的真結局門。沒指定就沒有條件判斷 (二周目進最後的房間一律 BE)")]
    public TrueEndGate trueEndGate;

    [Header("道具")]
    [Tooltip("開新遊戲時的物品欄 (依序放進第 1、2、3... 格)")]
    public List<ItemDefinition> startingItems = new List<ItemDefinition>();

    [Header("二周目 transform")]
    [Tooltip("transform 生成的電池")]
    public GameObject recallBatteryPrefab;
    [Tooltip("每顆電池生成時的特效 (自己播完自己消失)")]
    public GameObject recallSpawnFx;
    [Tooltip("重疊判定的大小 (比電池小一點，剛好貼著牆不算)。跟實體重疊的電池生成後立刻爆炸")]
    public Vector2 recallOverlapSize = new Vector2(0.8f, 0.8f);

    [Header("劇情")]
    [Tooltip("開場對話框")]
    public DialogueUI dialogue;
    [Tooltip("一周目開頭的劇情，一行 = 一句。只在新遊戲第一次進場時播，讀檔 / 死亡重載不播")]
    [TextArea(3, 15)] public string introLoop1;
    [Tooltip("一周目通關 (進最後的房間後再往前走 loop1ClearDistance 格) 時播的劇情，一行 = 一句。播完才進二周目")]
    [UnityEngine.Serialization.FormerlySerializedAs("introLoop2")]
    [TextArea(3, 15)] public string clearLoop1;
    [Tooltip("二周目開頭 (新的 save2 第一次進場) 播的系統訊息，一行 = 一句。讀檔 / 死亡重載不播")]
    [TextArea(2, 10)] public string startLoop2 = "【系統】終端已解鎖新功能，打開即可查看";
    [Tooltip("一周目：進最後的房間後，玩家要再往右走幾格才播 clearLoop1 並進二周目 (從進房間的落點算)")]
    public float loop1ClearDistance = 5f;
    [Tooltip("淡入完成後等幾秒才開始對話")]
    public float introDelay = 0.6f;
    [Tooltip("二周目通關後的結局劇情 (播完才播 end 動畫)。一行 = 一句；單獨一行 cg_3 = 換成 CG3 的圖 (圖拖在 EndingUI.cgSprites)；cg 底下沒有句子 = 只看圖按一下繼續；【end】= 結束")]
    [TextArea(5, 30)] public string endingStory;

    [Header("音效")]
    public AudioClip dieSfx;

    [Header("規則")]
    [Tooltip("勾選 = 死亡後道具恢復成目前房間的快照 (A)\n取消 = 死亡後道具維持已消耗，並寫回目前房間的快照 (B)")]
    public bool restoreItemsOnDeath = true;

    /// <summary>除錯用：無敵。</summary>
    public bool Invincible { get; set; }

    /// <summary>存檔、讀檔、transform 時觸發 (終端機監聽)。</summary>
    public event Action StateChanged;

    /// <summary>所有房間，依 x 座標由左到右 (= room1, room2...)。</summary>
    public IReadOnlyList<MapArea> Rooms => rooms;
    public string CurrentRoomId => data.currentRoomId;
    /// <summary>最後的房間 (最右邊 = room 6)：一周目進來往前走一段就通關，二周目進來結算真結局條件。</summary>
    public MapArea FinalRoom => rooms.Count > 0 ? rooms[rooms.Count - 1] : null;
    public Inventory Inventory { get; private set; }
    /// <summary>第幾周目 (1 / 2)。</summary>
    public int Loop => data.loop;
    /// <summary>一周目通關時記錄的電池位置。</summary>
    public IReadOnlyList<Vector2> BatteryRecords => data.batteryRecords;
    /// <summary>死亡、轉場、切場景、劇情對話中 (不能開終端機)。</summary>
    public bool Busy => dying || transitioning || talking;

    readonly List<MapArea> rooms = new List<MapArea>();
    /// <summary>目前這條時間線上已經用過 transform 的房間 (跟物品欄一起存進房間快照)。</summary>
    readonly HashSet<string> recallUsedAreas = new HashSet<string>();
    static readonly Collider2D[] overlap = new Collider2D[1];
    SaveData data;
    bool dying;
    bool transitioning;
    bool talking;

    void Awake()
    {
        Instance = this;
        Inventory = player.GetComponent<Inventory>();
        data = SaveSystem.Load(SaveSystem.CurrentSlot) ?? new SaveData(); // Awake 就讀，其他腳本的 Start 才拿得到 Loop 等資料

        rooms.AddRange(FindObjectsByType<MapArea>(FindObjectsSortMode.None));
        rooms.Sort((a, b) => a.Bounds.bounds.min.x.CompareTo(b.Bounds.bounds.min.x));
        var ids = new HashSet<string>();
        foreach (var r in rooms)
            if (!ids.Add(r.Id)) Debug.LogError($"MapArea 名稱重複: {r.Id}，請改名", r);
    }

    void Start()
    {
        GameInput.Locked = false;
        ApplySaveData();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Update()
    {
        if (GameInput.RestartPressed) RestartAtNode();
    }

    /// <summary>R 鍵：立刻回到目前的重生點 (= 對目前房間讀檔)。</summary>
    public void RestartAtNode()
    {
        if (Busy) return;
        transitioning = true;
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
    }

    void ApplySaveData()
    {
        var snapshot = data.FindSnapshot(data.currentRoomId);
        if (snapshot == null)
        {
            // 新遊戲 (或周目開始)：PlayerStart + 開局道具，立刻在 room1 存檔
            player.Teleport(playerStart.position);
            Inventory.SetItems(startingItems);
            recallUsedAreas.Clear();
            var area = MapArea.Find(playerStart.position);
            if (area != null) EnterRoom(area, playerStart.position, silent: true);
            StartCoroutine(IntroRoutine(data.loop < 2 ? introLoop1 : startLoop2)); // 二周目開頭 = 系統訊息 (劇情在一周目通關時已經播過 clearLoop1)
        }
        else
        {
            player.Teleport(snapshot.spawn);
            Inventory.ReadFrom(snapshot);
            recallUsedAreas.Clear();
            recallUsedAreas.UnionWith(snapshot.recallUsedAreas);
            if (FinalRoom != null && snapshot.roomId == FinalRoom.Id)
            {
                // 讀檔 / 死亡重載到最後的房間：套用當初進房間時的結算，等淡入完再處理 (一周目 = 再通關一次、二周目沒達成 = BE)
                if (trueEndGate != null && data.loop >= 2) trueEndGate.ApplyRecord(snapshot.targetResults);
                StartCoroutine(ArriveFinalRoomAfterFade());
            }
        }
        StateChanged?.Invoke();
    }

    /// <summary>物品欄 + transform 的使用紀錄 + 重生位置 → 寫進房間快照。</summary>
    void WriteSnapshot(string roomId, Vector2 spawn)
    {
        var snapshot = data.GetOrCreateSnapshot(roomId);
        snapshot.spawn = spawn;
        Inventory.WriteTo(snapshot);
        snapshot.recallUsedAreas = new List<string>(recallUsedAreas);
    }

    void Save() => SaveSystem.Save(SaveSystem.CurrentSlot, data);

    /// <summary>一周目開頭的劇情：等淡入完 → 鎖住玩家輸入 (世界照跑) → 播對話 → 解鎖。</summary>
    IEnumerator IntroRoutine(string text)
    {
        if (dialogue == null || string.IsNullOrWhiteSpace(text)) yield break;
        var lines = SplitLines(text);

        talking = true;
        GameInput.Locked = true;
        yield return new WaitForSecondsRealtime((fader != null ? fader.duration : 0f) + introDelay);
        yield return dialogue.Play(lines);
        GameInput.Locked = false; // 協程在 Update 之後才跑，最後一下按鍵不會在同一幀被當成跳躍 / 互動
        talking = false;
    }

    /// <summary>劇情文本 → 一行一句 (去掉空行)。</summary>
    static List<string> SplitLines(string text)
    {
        var lines = new List<string>();
        foreach (var line in text.Split('\n'))
            if (line.Trim().Length > 0) lines.Add(line.Trim());
        return lines;
    }

    // ---------- 房間 (自動存檔) ----------

    /// <summary>房間的順序 (0 = room1)；找不到回傳 -1。</summary>
    public int RoomIndex(string id) => rooms.FindIndex(r => r.Id == id);

    public MapArea FindRoom(string id) => rooms.Find(r => r.Id == id);

    public bool IsDiscovered(MapArea room) => data.discoveredRooms.Contains(room.Id);

    public bool HasSnapshot(MapArea room) => data.FindSnapshot(room.Id) != null;

    /// <summary>
    /// 進入房間 (轉場落地後呼叫)。往前推進 (或這個房間還沒有快照) → 覆蓋快照、刪掉後面房間的快照 (新的時間線)；
    /// 往回走 → 只把重生房間改成這裡。
    /// </summary>
    /// leftBatteries = 剛離開的房間裡的玩家電池位置 (轉場開始時抓的，PlayerBatteriesIn)，一周目累計進新房間快照的 batteries。
    void EnterRoom(MapArea room, Vector2 spawn, bool silent = false, List<Vector2> leftBatteries = null)
    {
        if (room == null || room.Id == data.currentRoomId) return;
        if (!data.discoveredRooms.Contains(room.Id)) data.discoveredRooms.Add(room.Id);

        bool forward = RoomIndex(room.Id) > RoomIndex(data.currentRoomId) || !HasSnapshot(room);
        var batteries = forward ? AccumulateBatteries(leftBatteries) : null; // 要在 currentRoomId 改掉之前讀上個房間的快照
        data.currentRoomId = room.Id;
        if (forward)
        {
            WriteSnapshot(room.Id, spawn);
            data.FindSnapshot(room.Id).batteries = batteries;
            int order = RoomIndex(room.Id);
            data.snapshots.RemoveAll(s => RoomIndex(s.roomId) > order);
        }
        Save();
        if (forward && !silent) ToastUI.Show("已存檔");
        StateChanged?.Invoke();
    }

    /// <summary>一周目電池紀錄：目前房間快照的累積紀錄 + 剛離開的房間的電池。二周目不累計 (回傳空的)。</summary>
    List<Vector2> AccumulateBatteries(List<Vector2> leftBatteries)
    {
        var list = new List<Vector2>();
        if (data.loop >= 2) return list;
        var current = data.FindSnapshot(data.currentRoomId);
        if (current != null) list.AddRange(current.batteries);
        if (leftBatteries != null) list.AddRange(leftBatteries);
        return list;
    }

    /// <summary>room 裡現有的玩家電池位置 (SpawnedByPlayer，無人機吊著的也算)。要在無人機被移除前呼叫，否則吊著的電池會先掉下去。</summary>
    static List<Vector2> PlayerBatteriesIn(MapArea room)
    {
        var list = new List<Vector2>();
        if (room == null) return list;
        foreach (var b in Battery.All)
            if (b.SpawnedByPlayer && room.Contains(b.Body.position)) list.Add(b.Body.position);
        return list;
    }

    /// <summary>可以從終端機讀這個房間嗎 (有快照)。</summary>
    public bool CanRewindTo(MapArea room) => room != null && HasSnapshot(room);

    /// <summary>讀檔：重生房間改成該房間 → 重載場景 (物品欄回到該房間的快照)。後面房間的快照先保留，等玩家往前推進時才作廢。</summary>
    public void RewindTo(MapArea room)
    {
        if (!CanRewindTo(room) || Busy) return;
        transitioning = true;
        data.currentRoomId = room.Id;
        Save();
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
    }

    // ---------- 二周目 ----------

    public bool IsRecallUsed(MapArea area) => area != null && recallUsedAreas.Contains(area.Id);

    /// <summary>這個房間有沒有一周目留下的電池紀錄。</summary>
    public bool HasRecords(MapArea area)
    {
        if (area == null) return false;
        foreach (var p in data.batteryRecords) if (area.Contains(p)) return true;
        return false;
    }

    /// <summary>玩家目前所在的房間。</summary>
    public MapArea PlayerRoom => MapArea.Find(player.Body.position);

    /// <summary>
    /// transform：在 area 生成一周目記錄的電池 (只能對玩家目前所在的房間用，每個房間一次)。
    /// 生成位置跟實體 (地形、門、平台、其他電池、玩家、無人機) 重疊 → 那顆立刻爆炸。
    /// </summary>
    public bool Recall(MapArea area)
    {
        if (data.loop < 2 || area == null || area != PlayerRoom || IsRecallUsed(area) || recallBatteryPrefab == null) return false;
        var spots = new List<Vector2>();
        foreach (var p in data.batteryRecords) if (area.Contains(p)) spots.Add(p);
        if (spots.Count == 0) return false;

        recallUsedAreas.Add(area.Id);
        var filter = new ContactFilter2D { useTriggers = false };
        foreach (var p in spots)
        {
            bool blocked = Physics2D.OverlapBox(p, recallOverlapSize, 0f, filter, overlap) > 0;
            var battery = Instantiate(recallBatteryPrefab, p, Quaternion.identity).GetComponent<Battery>();
            if (recallSpawnFx != null) Instantiate(recallSpawnFx, p, Quaternion.identity);
            if (blocked) battery.Explode();
        }
        StateChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 通關 (一周目：進入最後的房間；二周目：最後房間條件沒達成，或門後的 ClearGoal)。一周目：記錄玩家生成的電池位置 → 建立全新的 save2 (舊的 save2 被覆蓋) → 從頭開始二周目。
    /// save1 保留通關當下的狀態，之後可以從標題畫面讀 save1 回去重玩一周目。二周目：播 end 動畫，按任意鍵回標題。
    /// </summary>
    public void ClearGame()
    {
        if (Busy) return;
        if (data.loop >= 2)
        {
            StartCoroutine(EndingRoutine());
            return;
        }

        Save(); // save1 留著
        var next = new SaveData { loop = 2 };
        // 電池紀錄 = 進最後房間時快照累積的 (前面每個房間離開時記下的)，死亡重載過也不會掉
        var snapshot = data.FindSnapshot(data.currentRoomId);
        if (snapshot != null) next.batteryRecords.AddRange(snapshot.batteries);
        SaveSystem.Save(SaveSystem.Save2, next);
        SaveSystem.CurrentSlot = SaveSystem.Save2;

        transitioning = true; // 播劇情期間不會死亡、不能開終端機
        StartCoroutine(ClearLoop1Routine());
    }

    /// <summary>一周目通關：原地播 clearLoop1 (玩家停住、輸入鎖住) → 重新載入場景進二周目。</summary>
    IEnumerator ClearLoop1Routine()
    {
        Drone.RemoveAll();
        GameInput.Locked = true;
        player.SetFrozen(true);
        if (dialogue != null && !string.IsNullOrWhiteSpace(clearLoop1))
            yield return dialogue.Play(SplitLines(clearLoop1));
        ToastUI.Show("一周目通關");
        yield return LoadSceneRoutine(SceneManager.GetActiveScene().name);
    }

    IEnumerator EndingRoutine()
    {
        transitioning = true;
        Drone.RemoveAll();
        GameInput.Locked = true;
        player.SetFrozen(true);
        if (ending == null)
        {
            ToastUI.Show("通關");
            yield return new WaitForSecondsRealtime(2f);
        }
        else
        {
            if (fader != null) yield return fader.FadeOut();
            Time.timeScale = 0f; // 劇情 / 動畫期間遊戲停住

            if (!string.IsNullOrWhiteSpace(endingStory))
            {
                var story = StartCoroutine(ending.PlayStory(endingStory)); // 先換成 CG，再從黑畫面淡入
                if (fader != null) StartCoroutine(fader.FadeIn());
                yield return story;
                if (fader != null) yield return fader.FadeOut();
            }

            var play = StartCoroutine(ending.Play()); // 立刻顯示 end 畫面，再從黑畫面淡入
            if (fader != null) StartCoroutine(fader.FadeIn());
            yield return play;
        }
        yield return LoadSceneRoutine(SceneNames.Title);
    }

    // ---------- 死亡 / 轉場 ----------

    public void KillPlayer()
    {
        if (Busy || Invincible) return;
        StartCoroutine(DeathRoutine());
    }

    /// <summary>地圖轉場：黑畫面 → 把玩家移到 position → 存檔 (進入新房間) → 淡入。由 MapEdge 呼叫。</summary>
    public void TransitionTo(Vector2 position)
    {
        if (Busy) return;
        StartCoroutine(TransitionRoutine(position));
    }

    /// <summary>鏡頭改跟隨 target (操控無人機時用)。</summary>
    public void SetCameraTarget(Transform target)
    {
        if (vcam != null) vcam.Follow = target;
    }

    IEnumerator TransitionRoutine(Vector2 position)
    {
        transitioning = true;
        // 電池紀錄：在無人機消失 (吊著的電池被放開、往下掉) 之前先抓位置
        var leftBatteries = PlayerBatteriesIn(PlayerRoom);
        Drone.RemoveAll(); // 切換地圖：所有無人機消失
        GameInput.Locked = true;
        player.SetFrozen(true);
        if (fader != null) yield return fader.FadeOut();

        player.Teleport(position);
        var room = MapArea.Find(position);
        EnterRoom(room, position, leftBatteries: leftBatteries);
        bool final = room != null && room == FinalRoom;
        if (final) RecordTargets(room);
        yield return null; // 讓 CameraAreaFollower 切換鏡頭範圍並定位

        player.SetFrozen(false);
        GameInput.Locked = false;
        transitioning = false;
        if (fader != null) yield return fader.FadeIn();
        if (final) ArriveFinalRoom();
    }

    // ---------- 最後的房間 (room 6) ----------

    /// <summary>二周目剛進最後的房間：結算 [Target] 條件 → 存進這個房間的快照 → 開門 (全部達成時)。</summary>
    void RecordTargets(MapArea room)
    {
        if (data.loop < 2 || trueEndGate == null) return;
        var results = trueEndGate.Evaluate();
        var snapshot = data.FindSnapshot(room.Id);
        if (snapshot != null) snapshot.targetResults = results;
        Save();
        trueEndGate.ApplyRecord(results);
    }

    /// <summary>到達最後的房間：一周目 = 往右走 loop1ClearDistance 格後通關 (clearLoop1 → 二周目)；二周目條件沒達成 = 播 BE。達成的話門已經開了，門後的 ClearGoal 才是真結局。</summary>
    void ArriveFinalRoom()
    {
        if (data.loop < 2) StartCoroutine(Loop1ClearAfterWalk());
        else if (trueEndGate == null || !trueEndGate.AllRecorded) ClearGame(); // 二周目的 ClearGame = BE (真結局還沒做，暫時共用)
    }

    /// <summary>一周目最後的房間：等玩家從現在的位置往右走 loop1ClearDistance 格 → 通關。死亡重載會重新等。</summary>
    IEnumerator Loop1ClearAfterWalk()
    {
        float targetX = player.Body.position.x + loop1ClearDistance;
        yield return new WaitUntil(() => !Busy && player.Body.position.x >= targetX);
        ClearGame();
    }

    IEnumerator ArriveFinalRoomAfterFade()
    {
        yield return new WaitForSecondsRealtime(fader != null ? fader.duration : 0f);
        ArriveFinalRoom();
    }

    IEnumerator DeathRoutine()
    {
        dying = true;
        Sfx.Play(dieSfx);
        GameInput.Locked = true;
        player.SetFrozen(true);
        if (fader != null) yield return fader.FadeOut();

        var snapshot = data.FindSnapshot(data.currentRoomId);
        if (!restoreItemsOnDeath && snapshot != null)
        {
            // 規則 B：把目前 (已消耗的) 物品欄寫回目前房間的快照，重生位置不變
            WriteSnapshot(data.currentRoomId, snapshot.spawn);
            Save();
        }

        // 重新載入場景 = 所有東西回到存檔狀態
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToTitle()
    {
        if (transitioning) return;
        transitioning = true;
        StartCoroutine(LoadSceneRoutine(SceneNames.Title));
    }

    IEnumerator LoadSceneRoutine(string scene)
    {
        GameInput.Locked = true;
        if (fader != null) yield return fader.FadeOut();
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    /// <summary>世界範圍。有 MapArea 時 = 所有區域的聯集，否則用 boundsTilemap。</summary>
    public Bounds WorldBounds
    {
        get
        {
            if (rooms.Count > 0)
            {
                var all = rooms[0].Bounds.bounds;
                foreach (var area in rooms) all.Encapsulate(area.Bounds.bounds);
                return all;
            }
            if (boundsTilemap == null) return new Bounds(Vector3.zero, Vector3.one * 50f);
            boundsTilemap.CompressBounds();
            var b = boundsTilemap.localBounds;
            b.center = boundsTilemap.transform.TransformPoint(b.center);
            return b;
        }
    }
}
