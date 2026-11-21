using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 傳送槍射出的子彈。場上同時只會有一顆 (TeleportBullet.Current)。
/// 飛行：朝發射方向前進，被固定的黑洞當作「加速度」吸引而拐彎 (靠近中心也會減速，可能被吸住)。
/// 結束條件：
/// - 撞到電池 (含固有電池、被吊著的) → 玩家傳送到撞擊點旁
/// - 撞到 hitMask 的其他東西 (地形、門、平台、可破壞物件)、停滯 (stallTime 秒內移動不到 stallDistance，例如被黑洞吸住)、
///   飛超過 maxFlightTime 秒 → 子彈消散 (vanishFx)，不傳送
/// 傳送位置放不下玩家時，改傳送到附近最近、放得下玩家的格子 (可能因此穿牆，當成特性)。
/// 飛行模擬 (Step) 與瞄準提示線 (Simulate) 共用，所以提示線跟實際軌跡一致。
/// </summary>
public class TeleportBullet : MonoBehaviour
{
    public static TeleportBullet Current { get; private set; }

    [Header("飛行")]
    [Tooltip("初速 (格/秒)")]
    public float speed = 15f;
    [Tooltip("飛行超過這個秒數還沒撞到東西就消失")]
    public float maxFlightTime = 5f;
    [Tooltip("碰撞判定半徑")]
    public float hitRadius = 0.15f;
    [Tooltip("撞到就傳送的圖層 (Ground)")]
    public LayerMask hitMask;
    [Tooltip("黑洞拉力的加速度倍率 (拉力 = BlackHole.SamplePull × 這個值，單位 格/秒²)")]
    public float pullAcceleration = 18f;

    [Header("停滯")]
    [Tooltip("這段時間內移動不到 stallDistance 就算停滯")]
    public float stallTime = 1f;
    public float stallDistance = 1f;

    [Header("傳送")]
    [Tooltip("撞擊點放不下玩家時，往外找空位的最大距離 (格)")]
    public int searchRadius = 6;
    [Tooltip("傳送特效 (出發點、到達點各播一次)")]
    public GameObject teleportFx;
    [Tooltip("沒打到電池、子彈消失時的特效")]
    public GameObject vanishFx;

    Vector2 velocity;
    float flightTime;
    Vector2 stallAnchor;
    float stallTimer;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    static readonly Collider2D[] overlap = new Collider2D[4];

    void Awake()
    {
        if (Current != null) Destroy(Current.gameObject); // 保險：永遠只有一顆
        Current = this;
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    public void Launch(Vector2 dir)
    {
        velocity = dir.normalized * speed;
        flightTime = 0f;
        stallAnchor = transform.position;
        stallTimer = 0f;
    }

    /// <summary>取消 (直接消失，不退次數)。</summary>
    public void Cancel() => Destroy(gameObject);

    void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;
        flightTime += dt;
        if (flightTime > maxFlightTime) { Vanish(); return; }

        Vector2 pos = transform.position;
        if (Step(ref pos, ref velocity, dt, out var hit))
        {
            if (IsBattery(hit)) TeleportPlayer(hit.centroid, hit.normal);
            else { transform.position = hit.centroid; Vanish(); }
            return;
        }
        transform.position = pos;

        if (Stalled(pos, dt, ref stallAnchor, ref stallTimer)) Vanish();
    }

    static bool IsBattery(RaycastHit2D hit) => hit.collider.GetComponentInParent<Battery>() != null;

    void Vanish()
    {
        if (vanishFx != null) Instantiate(vanishFx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    /// <summary>前進一個物理步。撞到東西回傳 true (pos 不變)。</summary>
    bool Step(ref Vector2 pos, ref Vector2 vel, float dt, out RaycastHit2D hit)
    {
        hit = default;
        Vector2 pull = BlackHole.SamplePull(pos, out float s);
        vel += pull * pullAcceleration * dt;
        vel = Vector2.MoveTowards(vel, Vector2.zero, s * BlackHole.Damping * dt);

        float distance = vel.magnitude * dt;
        if (distance > 1e-5f)
        {
            var filter = new ContactFilter2D { useTriggers = false };
            filter.SetLayerMask(hitMask);
            int count = Physics2D.CircleCast(pos, hitRadius, vel / vel.magnitude, filter, hits, distance);
            int best = -1;
            for (int i = 0; i < count; i++)
                if (best < 0 || hits[i].distance < hits[best].distance) best = i;
            if (best >= 0) { hit = hits[best]; return true; }
        }
        pos += vel * dt;
        return false;
    }

    bool Stalled(Vector2 pos, float dt, ref Vector2 anchor, ref float timer)
    {
        if ((pos - anchor).sqrMagnitude > stallDistance * stallDistance)
        {
            anchor = pos;
            timer = 0f;
            return false;
        }
        timer += dt;
        return timer >= stallTime;
    }

    /// <summary>預測軌跡 (瞄準提示線用，對 prefab 呼叫即可)。點會加進 points；會打到電池 (= 會傳送) 回傳 true。</summary>
    public bool Simulate(Vector2 origin, Vector2 dir, List<Vector3> points)
    {
        float dt = Time.fixedDeltaTime;
        Vector2 pos = origin, vel = dir.normalized * speed, anchor = origin;
        float timer = 0f;
        points.Add(pos);
        int steps = Mathf.CeilToInt(maxFlightTime / dt);
        for (int i = 0; i < steps; i++)
        {
            if (Step(ref pos, ref vel, dt, out var hit)) { points.Add(hit.centroid); return IsBattery(hit); }
            if (i % 2 == 0) points.Add(pos);
            if (Stalled(pos, dt, ref anchor, ref timer)) break;
        }
        points.Add(pos);
        return false;
    }

    // ---------- 傳送 ----------

    void TeleportPlayer(Vector2 point, Vector2 normal)
    {
        var gm = GameManager.Instance;
        if (gm != null)
        {
            Vector2 from = gm.player.Body.position;
            Vector2 to = FindSpot(gm.player, point, normal);
            gm.player.Teleport(to);
            if (teleportFx != null)
            {
                Instantiate(teleportFx, from, Quaternion.identity);
                Instantiate(teleportFx, to, Quaternion.identity);
            }
        }
        Destroy(gameObject);
    }

    /// <summary>
    /// 玩家要傳送到的位置 (rigidbody 位置)。先試「撞擊點沿法線往外推半個身體」，
    /// 放不下就找附近最近、放得下玩家的格子 (腳踩在格子底部)。
    /// </summary>
    Vector2 FindSpot(PlayerController player, Vector2 point, Vector2 normal)
    {
        var col = player.GetComponent<CapsuleCollider2D>();
        Vector2 size = col.size * (Vector2)player.transform.lossyScale;
        Vector2 offset = col.offset * (Vector2)player.transform.lossyScale;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(player.groundMask);

        bool Fits(Vector2 center) =>
            Physics2D.OverlapCapsule(center, size - Vector2.one * 0.02f, col.direction, 0f, filter, overlap) == 0;

        Vector2 first = point + new Vector2(normal.x * (size.x * 0.5f + 0.02f), normal.y * (size.y * 0.5f + 0.02f));
        if (Fits(first)) return first - offset;

        // 由近到遠一圈一圈找空格
        int cx = Mathf.FloorToInt(point.x), cy = Mathf.FloorToInt(point.y);
        for (int r = 0; r <= searchRadius; r++)
        {
            bool found = false;
            Vector2 best = Vector2.zero;
            float bestD = float.MaxValue;
            for (int x = cx - r; x <= cx + r; x++)
            for (int y = cy - r; y <= cy + r; y++)
            {
                if (Mathf.Abs(x - cx) != r && Mathf.Abs(y - cy) != r) continue; // 只看這一圈
                var center = new Vector2(x + 0.5f, y + size.y * 0.5f + 0.01f);
                if (!Fits(center)) continue;
                float d = (center - point).sqrMagnitude;
                if (d < bestD) { bestD = d; best = center; found = true; }
            }
            if (found) return best - offset;
        }
        return first - offset; // 真的找不到就照原位置
    }
}
