using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// 遊戲場景的總管 (每個 Game 場景一個)。負責：
/// - 進場時讀檔並套用 (玩家位置 = 目前節點、物品欄 = 目前節點的快照、節點狀態)
/// - 自動存檔 (EnterNode)、地圖回溯 (RewindTo)
/// - 死亡 → 淡出 → 重新載入場景 (= 回到目前節點的快照)
/// 其他腳本透過 GameManager.Instance 呼叫。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("場景參照")]
    public PlayerController player;
    [Tooltip("新遊戲 (還沒存過檔) 的出生點")]
    public Transform playerStart;
    [Tooltip("地圖 UI 用來計算世界範圍的 Tilemap")]
    public Tilemap boundsTilemap;
    public ScreenFader fader;

    [Header("道具")]
    [Tooltip("開新遊戲時的物品欄 (依序放進第 1、2、3... 格)")]
    public List<ItemDefinition> startingItems = new List<ItemDefinition>();

    [Header("規則")]
    [Tooltip("勾選 = 死亡後道具恢復成目前節點的快照 (A)\n取消 = 死亡後道具維持已消耗，並寫回目前節點的快照 (B)")]
    public bool restoreItemsOnDeath = true;

    /// <summary>除錯用：無敵。</summary>
    public bool Invincible { get; set; }

    /// <summary>存檔點被發現、存檔、停用時觸發 (地圖 UI 監聽)。</summary>
    public event Action StateChanged;

    public IReadOnlyList<SavePoint> SavePoints => savePoints;
    public string CurrentSavePointId => data.currentNodeId;
    public Inventory Inventory { get; private set; }

    readonly List<SavePoint> savePoints = new List<SavePoint>();
    SaveData data;
    bool dying;
    bool transitioning;

    void Awake()
    {
        Instance = this;
        Inventory = player.GetComponent<Inventory>();

        var ids = new HashSet<string>();
        foreach (var sp in FindObjectsByType<SavePoint>(FindObjectsSortMode.None))
        {
            if (!ids.Add(sp.Id)) Debug.LogError($"存檔點 ID 重複: {sp.Id} ({sp.name})，請 Regenerate ID", sp);
            savePoints.Add(sp);
        }
        savePoints.Sort((a, b) => a.transform.position.x.CompareTo(b.transform.position.x));
    }

    void Start()
    {
        GameInput.Locked = false;
        data = SaveSystem.Load(SaveSystem.CurrentSlot) ?? new SaveData();
        ApplySaveData();
    }

    void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void ApplySaveData()
    {
        // 節點狀態
        foreach (var sp in savePoints)
            sp.SetState(data.discoveredSavePoints.Contains(sp.Id), !data.disabledSavePoints.Contains(sp.Id));

        // 玩家位置
        var node = FindSavePoint(data.currentNodeId);
        player.Teleport(node != null ? node.SpawnPosition : (Vector2)playerStart.position);

        // 物品欄：目前節點的快照；沒有 (新遊戲) → 發開局道具
        var snapshot = node != null ? data.FindSnapshot(node.Id) : null;
        if (snapshot == null || snapshot.items.Count == 0) Inventory.SetItems(startingItems);
        else Inventory.ReadFrom(snapshot);
        StateChanged?.Invoke();
    }

    public SavePoint FindSavePoint(string id) => savePoints.Find(sp => sp.Id == id);

    /// <summary>節點的前後順序 (savePoints 已依 x 排序)；找不到回傳 -1。</summary>
    int NodeOrder(string id) => savePoints.FindIndex(sp => sp.Id == id);

    public bool HasSnapshot(SavePoint sp) => data.FindSnapshot(sp.Id) != null;

    // ---------- 節點 (自動存檔) ----------

    /// <summary>玩家走進節點 (SavePoint 呼叫)。</summary>
    public void EnterNode(SavePoint sp)
    {
        if (dying || transitioning || !sp.IsActive || sp.Id == data.currentNodeId) return;

        if (!data.discoveredSavePoints.Contains(sp.Id))
        {
            data.discoveredSavePoints.Add(sp.Id);
            sp.SetState(true, sp.IsActive);
        }

        // 往前推進 (或這個節點還沒有快照) → 覆蓋快照；往回走 → 只更新重生點
        bool forward = NodeOrder(sp.Id) > NodeOrder(data.currentNodeId) || !HasSnapshot(sp);
        data.currentNodeId = sp.Id;
        if (forward) Inventory.WriteTo(data.GetOrCreateSnapshot(sp.Id));
        SaveSystem.Save(SaveSystem.CurrentSlot, data);
        if (forward) ToastUI.Show("已存檔");
        StateChanged?.Invoke();
    }

    /// <summary>可以從地圖回溯到這個節點嗎 (去過、沒被停用、有快照)。</summary>
    public bool CanRewindTo(SavePoint sp) => sp.Discovered && sp.IsActive && HasSnapshot(sp);

    /// <summary>回溯：重生點改成該節點 → 重載場景 (物品欄回到該節點的快照)。後面節點的快照保留。</summary>
    public void RewindTo(SavePoint sp)
    {
        if (!CanRewindTo(sp) || dying || transitioning) return;
        data.currentNodeId = sp.Id;
        SaveSystem.Save(SaveSystem.CurrentSlot, data);
        StartCoroutine(LoadSceneRoutine(SceneManager.GetActiveScene().name));
    }

    /// <summary>停用 (消耗) 一個節點。預留給企劃的 trick 用，會立即寫入存檔。</summary>
    public void DisableSavePoint(SavePoint sp)
    {
        if (!data.disabledSavePoints.Contains(sp.Id)) data.disabledSavePoints.Add(sp.Id);
        sp.SetState(sp.Discovered, false);
        SaveSystem.Save(SaveSystem.CurrentSlot, data);
        StateChanged?.Invoke();
    }

    // ---------- 死亡 / 轉場 ----------

    public void KillPlayer()
    {
        if (dying || transitioning || Invincible) return;
        StartCoroutine(DeathRoutine());
    }

    /// <summary>地圖轉場：黑畫面 → 把玩家移到 position → 淡入。由 MapEdge 呼叫。</summary>
    public void TransitionTo(Vector2 position)
    {
        if (dying || transitioning) return;
        StartCoroutine(TransitionRoutine(position));
    }

    IEnumerator TransitionRoutine(Vector2 position)
    {
        transitioning = true;
        GameInput.Locked = true;
        player.SetFrozen(true);
        if (fader != null) yield return fader.FadeOut();

        player.Teleport(position);
        yield return null; // 讓 CameraAreaFollower 切換鏡頭範圍並定位

        player.SetFrozen(false);
        GameInput.Locked = false;
        transitioning = false;
        if (fader != null) yield return fader.FadeIn();
    }

    IEnumerator DeathRoutine()
    {
        dying = true;
        GameInput.Locked = true;
        player.SetFrozen(true);
        if (fader != null) yield return fader.FadeOut();

        if (!restoreItemsOnDeath && !string.IsNullOrEmpty(data.currentNodeId))
        {
            // 規則 B：把目前 (已消耗的) 物品欄寫回目前節點的快照，位置仍是目前節點
            Inventory.WriteTo(data.GetOrCreateSnapshot(data.currentNodeId));
            SaveSystem.Save(SaveSystem.CurrentSlot, data);
        }

        // 重新載入場景 = 所有東西回到存檔狀態
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ReturnToTitle()
    {
        StartCoroutine(LoadSceneRoutine(SceneNames.Title));
    }

    IEnumerator LoadSceneRoutine(string scene)
    {
        GameInput.Locked = true;
        if (fader != null) yield return fader.FadeOut();
        Time.timeScale = 1f;
        SceneManager.LoadScene(scene);
    }

    /// <summary>世界範圍 (地圖 UI 用)。有 MapArea 時 = 所有區域的聯集，否則用 boundsTilemap。</summary>
    public Bounds WorldBounds
    {
        get
        {
            if (MapArea.All.Count > 0)
            {
                var all = MapArea.All[0].Bounds.bounds;
                foreach (var area in MapArea.All) all.Encapsulate(area.Bounds.bounds);
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
