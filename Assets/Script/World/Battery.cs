using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 電池倉生成的電池：1x1、有碰撞、不會旋轉，玩家走過去就能推，也能站在上面。
/// 會被固定的黑洞吸引；可以壓住壓力板。不存檔 (死亡重載就消失)。
/// 可以被無人機吊起來 (SetCarried)：吊著時是 Kinematic、跟著無人機、不受黑洞影響、不跟玩家碰撞。
/// 跟無人機永遠不碰撞 (Drone 生成時會設定，這裡處理電池比無人機晚生成的情況)。
///
/// 爆炸 (Explode)：被火箭尾氣噴到 (PlayerRocket)、被平台擠壓 (MovingPlatform) 時觸發。
/// 範圍內的玩家、沒電的無人機被往外推；範圍內的其他電池延遲 chainDelay 秒後連鎖爆炸。
/// 不會炸死玩家、不會炸壞可破壞方塊。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Battery : MonoBehaviour
{
    public static readonly List<Battery> All = new List<Battery>();

    [Header("爆炸")]
    [Tooltip("爆炸半徑 (格)")]
    public float explosionRadius = 2.5f;
    [Tooltip("中心處的擊退速度 (格/秒)，到範圍邊緣遞減為 0")]
    public float maxKnockback = 36f;
    [Tooltip("連鎖爆炸的延遲 (秒)")]
    public float chainDelay = 0.1f;
    [Tooltip("爆炸特效 (生成在電池位置，自己播完自己消失)")]
    public GameObject explosionFx;

    bool exploded, explodeScheduled;
    readonly Collider2D[] overlap = new Collider2D[32];

    Rigidbody2D rb;
    Collider2D col;
    float baseGravity;
    Vector2 lastPull;

    public bool IsCarried { get; private set; }
    public Rigidbody2D Body => rb;
    public Collider2D Collider => col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        baseGravity = rb.gravityScale;
    }

    void OnEnable()
    {
        All.Add(this);
        foreach (var d in Drone.All) d.IgnoreCollisionWith(col);
    }

    void OnDisable() => All.Remove(this);

    /// <summary>無人機吊起 / 放下。</summary>
    public void SetCarried(bool carried)
    {
        IsCarried = carried;
        rb.bodyType = carried ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
        rb.linearVelocity = Vector2.zero;
        lastPull = Vector2.zero;
        var player = GameManager.Instance != null ? GameManager.Instance.player : null;
        if (player != null) Physics2D.IgnoreCollision(col, player.GetComponent<Collider2D>(), carried);
    }

    // ---------- 爆炸 ----------

    /// <summary>立刻爆炸。</summary>
    public void Explode()
    {
        if (exploded) return;
        exploded = true;

        Vector2 center = rb.position;
        if (explosionFx != null) Instantiate(explosionFx, center, Quaternion.identity);

        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.OverlapCircle(center, explosionRadius, filter, overlap);
        var done = new HashSet<Object>();
        for (int i = 0; i < n; i++)
        {
            var c = overlap[i];
            var battery = c.GetComponentInParent<Battery>();
            if (battery != null)
            {
                if (battery != this && done.Add(battery)) battery.ExplodeAfter(chainDelay); // 連鎖
                continue;
            }
            var player = c.GetComponent<PlayerController>();
            if (player != null && done.Add(player)) { player.AddKnockback(Knockback(center, player.Body.position)); continue; }
            var drone = c.GetComponentInParent<Drone>();
            if (drone != null && !drone.IsPowered && done.Add(drone)) drone.AddKnockback(Knockback(center, drone.transform.position));
        }

        Destroy(gameObject);
    }

    /// <summary>delay 秒後爆炸 (連鎖用)。重複呼叫只算一次。</summary>
    public void ExplodeAfter(float delay)
    {
        if (exploded || explodeScheduled) return;
        explodeScheduled = true;
        Invoke(nameof(Explode), delay);
    }

    /// <summary>從爆炸中心往外的擊退速度，越近越強。</summary>
    Vector2 Knockback(Vector2 center, Vector2 target)
    {
        Vector2 d = target - center;
        float dist = d.magnitude;
        Vector2 dir = dist > 0.01f ? d / dist : Vector2.up;
        return dir * (maxKnockback * Mathf.Clamp01(1f - dist / explosionRadius));
    }

    void FixedUpdate()
    {
        if (IsCarried) return;

        Vector2 pull = BlackHole.SamplePull(rb.position, out float s);
        // 越靠近黑洞重力越小，原本的速度也被抵銷 → 會被吸過去並停在黑洞附近
        rb.gravityScale = baseGravity * (1f - s);

        Vector2 own = rb.linearVelocity - lastPull; // 去掉上一步加的引力，剩下自己的速度
        own = Vector2.MoveTowards(own, Vector2.zero, s * BlackHole.Damping * Time.fixedDeltaTime);
        rb.linearVelocity = own + pull;
        lastPull = pull;
    }
}
