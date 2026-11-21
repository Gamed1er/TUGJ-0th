using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 無人機 (DroneItem 召喚)。兩種狀態：
/// - 有電 (IsPowered)：無視重力與黑洞，懸停；被操控時 WASD 移動、E 互動 (拉桿 / 吊起或放下電池)。
///   不能飛出召喚時所在的 MapArea。場上同時最多一台 (Drone.Powered)。
/// - 沒電：受重力、會被黑洞吸引、可以壓壓力板、站在軌道平台上會被載著走。
/// 拉一次拉桿、或放下吊著的電池 → 立刻沒電 (不另外扣次數)。
/// 跟玩家、電池永遠不碰撞；跟地形、門、平台會碰撞。切換地圖 / 死亡重載時全部消失。
/// 操控的開始與結束 (鏡頭、物品欄鎖定) 在 BeginControl / EndControl。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class Drone : MonoBehaviour
{
    public static readonly List<Drone> All = new List<Drone>();
    /// <summary>目前有電的無人機 (最多一台)，沒有就是 null。</summary>
    public static Drone Powered { get; private set; }
    /// <summary>正在被操控的無人機，沒有就是 null。玩家此時不吃移動 / 互動輸入、物品欄鎖定。</summary>
    public static Drone Controlled { get; private set; }

    [Header("移動")]
    [Tooltip("操控時的速度 (格/秒)")]
    public float speed = 5f;
    [Tooltip("沒電後的重力")]
    public float fallGravity = 3f;
    [Tooltip("會擋住吊著的電池的圖層 (地形、門、平台)")]
    public LayerMask blockMask;

    [Header("互動")]
    [Tooltip("可以吊起電池的距離 (格)")]
    public float batteryRange = 1.2f;
    [Tooltip("吊著的電池相對於無人機的位置 (吊起時那裡被擋住，就維持電池原本的相對位置)")]
    public Vector2 carryOffset = new Vector2(0f, -0.8f);

    [Header("外觀 (美術可替換)")]
    public SpriteRenderer visual;
    public Color poweredColor = new Color(0.3f, 0.9f, 1f);
    public Color depletedColor = new Color(0.35f, 0.35f, 0.4f);

    public bool IsPowered { get; private set; } = true;

    Rigidbody2D rb;
    Collider2D col;
    MapArea area;
    Battery carried;
    Vector2 carryLocal; // 這次吊起時實際使用的相對位置
    Vector2 lastPull;
    readonly List<Lever> nearLevers = new List<Lever>();
    readonly Collider2D[] overlap = new Collider2D[8];
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        rb.gravityScale = 0f;
        area = MapArea.Find(transform.position);
        if (Powered != null) Powered.Deplete(); // 保險：永遠只有一台有電
        Powered = this;
        SetVisual();
    }

    void OnEnable()
    {
        All.Add(this);
        foreach (var b in Battery.All) IgnoreCollisionWith(b.Collider);
        var gm = GameManager.Instance;
        if (gm != null) IgnoreCollisionWith(gm.player.GetComponent<Collider2D>());
    }

    void OnDisable() => All.Remove(this);

    void OnDestroy()
    {
        if (Controlled == this) EndControl();
        if (Powered == this) Powered = null;
        if (carried != null) carried.SetCarried(false);
    }

    public void IgnoreCollisionWith(Collider2D other)
    {
        if (other != null) Physics2D.IgnoreCollision(col, other, true);
    }

    // ---------- 操控 ----------

    public void BeginControl()
    {
        if (!IsPowered) return;
        Controlled = this;
        var gm = GameManager.Instance;
        gm.Inventory.Locked = true;
        gm.SetCameraTarget(transform);
    }

    public void EndControl()
    {
        if (Controlled != this) return;
        Controlled = null;
        rb.linearVelocity = Vector2.zero;
        InteractPromptUI.Hide();
        var gm = GameManager.Instance;
        if (gm == null) return; // 場景卸載中
        gm.Inventory.Locked = false;
        gm.SetCameraTarget(gm.player.transform);
    }

    /// <summary>沒電：受重力、不能再操控。</summary>
    public void Deplete()
    {
        if (!IsPowered) return;
        if (carried != null) { carried.SetCarried(false); carried = null; }
        EndControl();
        IsPowered = false;
        if (Powered == this) Powered = null;
        rb.gravityScale = fallGravity;
        SetVisual();
    }

    /// <summary>移除這台 (被平台擠壞、切換地圖)。不扣次數。</summary>
    public void Remove()
    {
        EndControl();
        Destroy(gameObject);
    }

    /// <summary>擊退 (電池爆炸)。只對沒電的無人機有效 (有電的不受外力影響)。</summary>
    public void AddKnockback(Vector2 velocity)
    {
        if (!IsPowered) rb.linearVelocity += velocity;
    }

    public static void RemoveAll()
    {
        foreach (var d in All.ToArray()) d.Remove();
    }

    // ---------- 每幀 ----------

    void Update()
    {
        if (Controlled != this) return;

        string prompt = null;
        Lever lever = NearestLever();
        Battery battery = carried == null && lever == null ? NearestBattery() : null;
        if (carried != null) prompt = "E 放下電池";
        else if (lever != null) prompt = "E 拉桿";
        else if (battery != null) prompt = "E 吊起電池";

        if (prompt != null) InteractPromptUI.Show(prompt, transform.position + Vector3.up * 1.2f);
        else InteractPromptUI.Hide();

        if (!GameInput.InteractPressed) return;
        if (carried != null)
        {
            Deplete(); // 放下電池 → 沒電
        }
        else if (lever != null)
        {
            lever.Pull();
            Deplete();
        }
        else if (battery != null)
        {
            carried = battery;
            carried.SetCarried(true);
            // 正下方有空間才把電池移過去，否則維持原本的相對位置 (避免把電池塞進牆裡)
            carryLocal = CarrySpotFree(rb.position + carryOffset) ? carryOffset : carried.Body.position - rb.position;
        }
    }

    void FixedUpdate()
    {
        if (IsPowered) PoweredMove();
        else DepletedMove();
    }

    void PoweredMove()
    {
        Vector2 v = Controlled == this ? new Vector2(GameInput.Move, GameInput.Vertical) * speed : Vector2.zero;
        float dt = Time.fixedDeltaTime;

        // 吊著電池：電池撞牆的方向不能走 (電池是 Kinematic，不會被物理擋住，所以自己檢查)
        if (carried != null)
        {
            if (v.x != 0f && CarriedBlocked(new Vector2(Mathf.Sign(v.x), 0f), Mathf.Abs(v.x) * dt)) v.x = 0f;
            if (v.y != 0f && CarriedBlocked(new Vector2(0f, Mathf.Sign(v.y)), Mathf.Abs(v.y) * dt)) v.y = 0f;
        }

        // 不能飛出地圖區域
        if (area != null)
        {
            Bounds b = area.Bounds.bounds;
            Vector2 half = col.bounds.extents;
            Vector2 next = rb.position + v * dt;
            next.x = Mathf.Clamp(next.x, b.min.x + half.x, b.max.x - half.x);
            next.y = Mathf.Clamp(next.y, b.min.y + half.y, b.max.y - half.y);
            v = (next - rb.position) / dt;
        }

        rb.linearVelocity = v;
        if (carried != null) carried.Body.MovePosition(rb.position + v * dt + carryLocal);
    }

    bool CarriedBlocked(Vector2 dir, float distance)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(blockMask);
        int n = carried.Body.Cast(dir, filter, hits, distance + 0.02f);
        for (int i = 0; i < n; i++)
        {
            var c = hits[i].collider;
            if (c == col || c.GetComponentInParent<Battery>() != null) continue;
            // 已經陷進去的碰撞體不算擋路 (讓它可以往外移，才不會卡死)；只是貼著的照樣擋
            var d = carried.Collider.Distance(c);
            if (d.isOverlapped && d.distance < -0.02f) continue;
            return true;
        }
        return false;
    }

    /// <summary>電池放在 position 會不會跟地形 / 門 / 平台重疊。</summary>
    bool CarrySpotFree(Vector2 position)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(blockMask);
        Vector2 size = (Vector2)carried.Collider.bounds.size - Vector2.one * 0.05f;
        int n = Physics2D.OverlapBox(position, size, 0f, filter, overlap);
        for (int i = 0; i < n; i++)
        {
            var c = overlap[i];
            if (c == col || c.GetComponentInParent<Battery>() != null) continue;
            return false;
        }
        return true;
    }

    void DepletedMove()
    {
        // 跟電池一樣：越靠近黑洞重力越小，原本的速度也被抵銷
        Vector2 pull = BlackHole.SamplePull(rb.position, out float s);
        rb.gravityScale = fallGravity * (1f - s);
        Vector2 own = rb.linearVelocity - lastPull;
        own = Vector2.MoveTowards(own, Vector2.zero, s * BlackHole.Damping * Time.fixedDeltaTime);
        rb.linearVelocity = own + pull;
        lastPull = pull;

        // 掉出地圖底部
        if (area != null && rb.position.y < area.Bounds.bounds.min.y - 2f) Destroy(gameObject);
    }

    // ---------- 互動目標 ----------

    void OnTriggerEnter2D(Collider2D other)
    {
        var lever = other.GetComponentInParent<Lever>();
        if (lever != null && !nearLevers.Contains(lever)) nearLevers.Add(lever);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        var lever = other.GetComponentInParent<Lever>();
        if (lever != null) nearLevers.Remove(lever);
    }

    Lever NearestLever()
    {
        nearLevers.RemoveAll(l => l == null);
        Lever best = null;
        float bestD = float.MaxValue;
        foreach (var l in nearLevers)
        {
            float d = ((Vector2)(l.transform.position - transform.position)).sqrMagnitude;
            if (d < bestD) { bestD = d; best = l; }
        }
        return best;
    }

    Battery NearestBattery()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.OverlapCircle(rb.position, batteryRange, filter, overlap);
        Battery best = null;
        float bestD = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            var b = overlap[i].GetComponentInParent<Battery>();
            if (b == null || b.IsCarried) continue;
            float d = (b.Body.position - rb.position).sqrMagnitude;
            if (d < bestD) { bestD = d; best = b; }
        }
        return best;
    }

    void SetVisual()
    {
        if (visual != null) visual.color = IsPowered ? poweredColor : depletedColor;
    }
}
