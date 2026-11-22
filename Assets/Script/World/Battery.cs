using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 電池：1x1、有碰撞、不會旋轉，玩家走過去就能推 (Push，可以推上 / 下斜坡、一次推一排)，也能站在上面。
/// 電池倉生成的 = Battery.prefab；關卡擺放的「固有電池」= FixedBattery.prefab (rocketProof、不會被記錄進二周目)。
/// 會被固定的黑洞吸引；可以壓住壓力板。不存檔 (死亡重載就消失)。
/// 可以被無人機吊起來 (SetCarried)：吊著時是 Kinematic、跟著無人機、不受黑洞影響。玩家可以站在吊著的電池上 (站久了無人機會墜機，見 Drone)。
/// 跟無人機永遠不碰撞 (Drone 生成時會設定，這裡處理電池比無人機晚生成的情況)。
///
/// 爆炸 (Explode)：被平台擠壓 (MovingPlatform)、二周目 transform 生成時重疊 → 立刻爆炸。
/// 被火箭尾氣噴到 (PlayerRocket，rocketProof 的不會) → 先點燃引信 (StartFuse)：閃紅 + 頭上顯示倒數 + 火花，fuseTime 秒後爆炸。
/// 範圍內的玩家、沒電的無人機、其他電池 (吊著的不會) 被往外推 (Knockback.Apply：疊在一起的會一起飛)；
/// 其他電池延遲 chainDelay 秒後連鎖爆炸。
/// 吊著的電池也會爆炸 (擠壓、火箭、連鎖)，爆炸後無人機手上變空 (Drone.ReleaseCarried)。
/// 範圍內的可破壞方塊 (Breakable) 被破壞 (隔牆也會)。不會炸死玩家。
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
    [Tooltip("勾選 = 火箭尾氣不會引爆 (固有電池)")]
    public bool rocketProof;

    [Header("引信 (火箭尾氣點燃)")]
    [Tooltip("點燃到爆炸的時間 (秒)")]
    public float fuseTime = 1.5f;
    [Tooltip("倒數時閃爍的顏色")]
    public Color fuseColor = new Color(1f, 0.2f, 0.15f, 1f);
    [Tooltip("閃爍頻率 (次/秒)：剛點燃 → 快爆炸")]
    public Vector2 fuseFlashRate = new Vector2(3f, 12f);
    [Tooltip("倒數數字在電池上方的位置")]
    public Vector2 fuseTextOffset = new Vector2(0f, 0.9f);
    [Tooltip("倒數中的循環特效 (火花，生成在電池上當子物件，爆炸時一起消失)")]
    public GameObject fuseFx;

    [Header("推動")]
    [Tooltip("會擋住推動的圖層 (Ground)")]
    public LayerMask blockMask = 1 << 8;
    [Tooltip("能被推上去的最大坡度")]
    [Range(0f, 89f)] public float maxSlopeAngle = 50f;

    bool exploded, explodeScheduled;
    float fuseLeft = -1f;    // < 0 = 沒有點燃
    float flashPhase;
    SpriteRenderer sr;
    Color baseColor;
    TextMesh fuseText;
    readonly Collider2D[] overlap = new Collider2D[32];

    Rigidbody2D rb;
    Collider2D col;
    float baseGravity;
    Vector2 lastPull;
    Vector2 knockLeft;       // 擊退還沒扣掉的部分
    float knockRate;
    int pushedStep = -1;     // 最後一次被推的物理步 (Push 由玩家呼叫)
    bool wasPushed;
    readonly RaycastHit2D[] castHits = new RaycastHit2D[8];

    public bool IsCarried { get; private set; }
    public bool IsFusing => fuseLeft >= 0f;
    /// <summary>玩家用電池倉生成的 (一周目通關時只記錄這種，關卡原本擺的不算)。</summary>
    public bool SpawnedByPlayer { get; set; }
    public Rigidbody2D Body => rb;
    public Collider2D Collider => col;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        baseGravity = rb.gravityScale;
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) baseColor = sr.color;
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
    }

    // ---------- 爆炸 ----------

    /// <summary>立刻爆炸。</summary>
    public void Explode()
    {
        if (exploded) return;
        exploded = true;
        if (IsCarried) foreach (var d in Drone.All) d.ReleaseCarried(this);

        Vector2 center = rb.position;
        if (explosionFx != null) Instantiate(explosionFx, center, Quaternion.identity);

        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.OverlapCircle(center, explosionRadius, filter, overlap);
        var done = new HashSet<Object>();
        var bodies = new List<Rigidbody2D>(); // 要被擊退的東西
        for (int i = 0; i < n; i++)
        {
            var c = overlap[i];
            var battery = c.GetComponentInParent<Battery>();
            if (battery != null)
            {
                if (battery != this && done.Add(battery))
                {
                    if (!battery.IsCarried) bodies.Add(battery.rb); // 先被炸飛
                    battery.ExplodeAfter(chainDelay);              // 再連鎖爆炸
                }
                continue;
            }
            var player = c.GetComponent<PlayerController>();
            if (player != null && done.Add(player)) { bodies.Add(player.Body); continue; }
            var drone = c.GetComponentInParent<Drone>();
            if (drone != null && !drone.IsPowered && done.Add(drone)) bodies.Add(drone.Body);
        }
        Knockback.Apply(center, explosionRadius, maxKnockback, bodies);
        Breakable.BreakAround(center, explosionRadius);

        Destroy(gameObject);
    }

    /// <summary>點燃引信：fuseTime 秒後爆炸。已經點燃 / 已經爆炸就不做事 (火箭尾氣每一步都會呼叫)。</summary>
    public void StartFuse()
    {
        if (exploded || IsFusing) return;
        fuseLeft = fuseTime;
        flashPhase = 0f;
        if (fuseFx != null) Instantiate(fuseFx, transform.position, Quaternion.identity, transform);

        // 倒數數字：世界座標的 TextMesh (只有數字，內建字型就夠)
        var go = new GameObject("FuseText");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = fuseTextOffset;
        fuseText = go.AddComponent<TextMesh>();
        var font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        fuseText.font = font;
        fuseText.fontSize = 64;
        fuseText.characterSize = 0.06f;
        fuseText.anchor = TextAnchor.MiddleCenter;
        fuseText.alignment = TextAlignment.Center;
        fuseText.color = fuseColor;
        var mr = go.GetComponent<MeshRenderer>();
        mr.sharedMaterial = font.material;
        mr.sortingOrder = (sr != null ? sr.sortingOrder : 0) + 10;
        UpdateFuseVisual();
    }

    void Update()
    {
        if (!IsFusing || exploded) return;
        fuseLeft -= Time.deltaTime;
        if (fuseLeft <= 0f) { Explode(); return; }
        UpdateFuseVisual();
    }

    void UpdateFuseVisual()
    {
        float t = fuseTime > 0f ? 1f - fuseLeft / fuseTime : 1f;
        flashPhase += Time.deltaTime * Mathf.Lerp(fuseFlashRate.x, fuseFlashRate.y, t);
        if (sr != null) sr.color = Mathf.Repeat(flashPhase, 1f) < 0.5f ? fuseColor : baseColor;
        if (fuseText != null) fuseText.text = Mathf.Max(fuseLeft, 0.1f).ToString("0.0");
    }

    /// <summary>delay 秒後爆炸 (連鎖用)。重複呼叫只算一次。</summary>
    public void ExplodeAfter(float delay)
    {
        if (exploded || explodeScheduled) return;
        explodeScheduled = true;
        Invoke(nameof(Explode), delay);
    }

    /// <summary>擊退 (爆炸)：速度一次加上去，之後在 Knockback.Time 秒內把這部分扣掉 (跟玩家一致)。</summary>
    public void AddKnockback(Vector2 velocity)
    {
        if (IsCarried || exploded) return;
        rb.linearVelocity += velocity;
        knockLeft += velocity;
        knockRate = knockLeft.magnitude / Knockback.Time;
    }

    // ---------- 推動 ----------

    int Step => Mathf.RoundToInt(Time.fixedTime / Time.fixedDeltaTime);

    /// <summary>
    /// 被往 dir (+1 / -1) 推，水平速度 = speed。沿著腳下的地面 / 前方的斜坡移動 (不受重力)，前面有電池就一起推。
    /// 推得動回傳 true；被擋住、懸空、被吊著回傳 false (懸空時交給物理，玩家照舊能把它推下去)。
    /// </summary>
    public bool Push(int dir, float speed, int depth = 0)
    {
        if (IsCarried || exploded || depth > 8) return false;
        if (pushedStep == Step) return true; // 這一步已經被推過 (一排電池)

        if (!GroundNormal(out Vector2 ground)) return false;
        Vector2 move = Tangent(ground, dir);
        float dist = Mathf.Abs(speed) * Time.fixedDeltaTime + 0.02f;

        // 前方：斜坡 → 改沿斜坡；電池 → 一起推；其他 → 擋住
        if (CastFirst(move, dist, out var hit))
        {
            var other = hit.collider.GetComponentInParent<Battery>();
            if (other != null)
            {
                if (!other.Push(dir, speed, depth + 1)) return false;
            }
            else if (Walkable(hit.normal))
            {
                move = Tangent(hit.normal, dir);
                if (CastFirst(move, dist, out var hit2) && hit2.collider.GetComponentInParent<Battery>() == null) return false;
            }
            else return false;
        }

        // 水平速度跟玩家一致，坡上的垂直速度跟著坡度
        float scale = Mathf.Abs(speed) / Mathf.Max(Mathf.Abs(move.x), 0.3f);
        rb.gravityScale = 0f;
        rb.linearVelocity = move * scale;
        lastPull = Vector2.zero;
        pushedStep = Step;
        wasPushed = true;
        return true;
    }

    bool Walkable(Vector2 normal) => normal.y >= Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

    /// <summary>沿著法線為 normal 的表面、往 dir 的方向。</summary>
    static Vector2 Tangent(Vector2 normal, int dir) => new Vector2(normal.y, -normal.x) * dir;

    bool GroundNormal(out Vector2 normal)
    {
        normal = Vector2.up;
        int n = Cast(Vector2.down, 0.08f);
        for (int i = 0; i < n; i++)
        {
            if (!Walkable(castHits[i].normal)) continue;
            normal = castHits[i].normal;
            return true;
        }
        return false;
    }

    /// <summary>往 dir 投射，回傳最近一個「迎面擋住」的東西 (貼著滑過的表面不算)。</summary>
    bool CastFirst(Vector2 dir, float distance, out RaycastHit2D hit)
    {
        hit = default;
        int n = Cast(dir, distance);
        float best = float.MaxValue;
        for (int i = 0; i < n; i++)
        {
            if (Vector2.Dot(castHits[i].normal, dir) > -0.05f) continue;
            if (castHits[i].distance < best) { best = castHits[i].distance; hit = castHits[i]; }
        }
        return best < float.MaxValue;
    }

    int Cast(Vector2 dir, float distance)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(blockMask);
        return rb.Cast(dir, filter, castHits, distance);
    }

    void FixedUpdate()
    {
        if (IsCarried) { knockLeft = Vector2.zero; return; }
        rb.linearVelocity = Knockback.Decay(ref knockLeft, knockRate, rb.linearVelocity);

        // 被推中：速度由 Push 決定；剛停止推 → 停下 (避免沿坡往上飛出去)
        if (pushedStep >= Step - 1) return;
        if (wasPushed)
        {
            wasPushed = false;
            rb.linearVelocity = Vector2.zero;
        }

        Vector2 pull = BlackHole.SamplePull(rb.position, BlackHole.PullTarget.Object, out float s);
        // 越靠近黑洞重力越小，原本的速度也被抵銷 → 會被吸過去並停在黑洞附近
        rb.gravityScale = baseGravity * (1f - s);

        Vector2 own = rb.linearVelocity - lastPull; // 去掉上一步加的引力，剩下自己的速度
        own = Vector2.MoveTowards(own, Vector2.zero, s * BlackHole.Damping * Time.fixedDeltaTime);
        rb.linearVelocity = own + pull;
        lastPull = pull;
    }
}
