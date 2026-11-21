using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 黑洞槍射出的黑洞。場上同時只會有一顆 (BlackHole.Current)。
/// 飛行中：直線前進 → 撞到地形/門 = 貼在表面固定；撞到 Breakable = 破壞它周圍 breakRadius 內所有 Breakable 並消失；玩家、電池、觸發區直接穿過。
/// 固定後：對範圍內的玩家、物體 (電池、沒電的無人機)、傳送槍子彈產生引力 (由它們自己呼叫 SamplePull 取得)，並播放往中心吸的粒子。
/// 三種對象的吸力分開調整 (Inspector 的「引力」區塊)。
/// </summary>
public class BlackHole : MonoBehaviour
{
    public static BlackHole Current { get; private set; }

    [Header("飛行")]
    [Tooltip("飛行速度 (格/秒)")]
    public float speed = 15f;
    [Tooltip("飛行超過這個秒數還沒撞到東西就消失 (防關卡漏洞)")]
    public float maxFlightTime = 10f;
    [Tooltip("碰撞判定半徑")]
    public float hitRadius = 0.25f;
    [Tooltip("會讓黑洞停下的圖層 (地形、門、可破壞物件)")]
    public LayerMask hitMask;

    [Header("破壞")]
    [Tooltip("撞到可破壞方塊時，以該方塊為中心，這個距離 (格) 內的可破壞方塊全部破壞 (隔牆也會)")]
    public float breakRadius = 2f;

    [Header("引力")]
    [Tooltip("作用半徑 (格)")]
    public float pullRadius = 4f;
    [Tooltip("對玩家：中心處的拉力 (格/秒，疊加在玩家速度上)。必須小於玩家走路速度 (moveSpeed) 與火箭上升速度 (riseSpeed)，玩家才逃得掉")]
    [FormerlySerializedAs("maxPullSpeed")]
    public float playerPull = 3.5f;
    [Tooltip("對物體 (電池、沒電的無人機)：中心處的拉力 (格/秒)")]
    public float objectPull = 3.5f;
    [Tooltip("對傳送槍子彈：中心處的加速度 (格/秒²，會讓子彈轉彎；子彈初速 15)")]
    public float bulletPull = 63f;
    [Tooltip("靠近中心時，抵銷原本速度的強度 (讓物體可以停在黑洞附近)")]
    public float damping = 15f;

    [Header("外觀")]
    [Tooltip("固定後才顯示的引力範圍圈")]
    public GameObject rangeIndicator;
    [Tooltip("固定後播放的吸入粒子 (圓形外圈往中心)。半徑與壽命會依 pullRadius 自動設定")]
    public ParticleSystem pullFx;
    [Tooltip("吸入粒子往中心移動的速度 (格/秒)")]
    public float pullFxSpeed = 2.5f;

    public bool IsFlying { get; private set; }

    Vector2 direction;
    float flightTime;
    readonly RaycastHit2D[] hits = new RaycastHit2D[8];

    void Awake()
    {
        if (Current != null) Destroy(Current.gameObject); // 保險：永遠只有一顆
        Current = this;
        if (rangeIndicator != null) rangeIndicator.transform.localScale = Vector3.one * pullRadius * 2f;
        SetIndicator(false);
        SetupPullFx();
    }

    void OnDestroy()
    {
        if (Current == this) Current = null;
    }

    public void Launch(Vector2 dir)
    {
        direction = dir.normalized;
        IsFlying = true;
        flightTime = 0f;
    }

    /// <summary>在目前位置固定。</summary>
    public void Fix()
    {
        IsFlying = false;
        SetIndicator(true);
        if (pullFx != null) pullFx.Play();
    }

    /// <summary>收回 (直接消失，不退次數)。</summary>
    public void Retrieve() => Destroy(gameObject);

    void FixedUpdate()
    {
        if (!IsFlying) return;

        flightTime += Time.fixedDeltaTime;
        if (flightTime > maxFlightTime) { Destroy(gameObject); return; }

        Vector2 pos = transform.position;
        float step = speed * Time.fixedDeltaTime;
        if (!CastFlight(pos, direction, step, out var hit))
        {
            transform.position = pos + direction * step;
            return;
        }

        var breakable = hit.collider.GetComponentInParent<Breakable>();
        if (breakable != null)
        {
            Breakable.BreakAround(breakable.transform.position, breakRadius);
            Destroy(gameObject);
            return;
        }

        // 貼在表面 (centroid = 圓形剛碰到時的圓心，不會陷進牆裡)
        transform.position = hit.centroid;
        Fix();
    }

    /// <summary>沿 dir 投射 distance 距離，找最近的有效碰撞 (略過觸發區與電池)。飛行與瞄準提示線共用。</summary>
    bool CastFlight(Vector2 from, Vector2 dir, float distance, out RaycastHit2D hit)
    {
        hit = default;
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(hitMask);
        int count = Physics2D.CircleCast(from, hitRadius, dir, filter, hits, distance);
        int best = -1;
        for (int i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (c.isTrigger || c.GetComponentInParent<Battery>() != null) continue;
            if (best < 0 || hits[i].distance < hits[best].distance) best = i;
        }
        if (best < 0) return false;
        hit = hits[best];
        return true;
    }

    /// <summary>預測從 origin 往 dir 射出後停下的位置 (瞄準提示線用，對 prefab 呼叫即可)。打不到東西 = 飛到最大距離的位置。</summary>
    public Vector2 PredictStop(Vector2 origin, Vector2 dir)
    {
        float maxDistance = speed * maxFlightTime;
        return CastFlight(origin, dir, maxDistance, out var hit) ? hit.centroid : origin + dir * maxDistance;
    }

    void SetupPullFx()
    {
        if (pullFx == null) return;
        pullFx.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var shape = pullFx.shape;
        shape.radius = pullRadius;
        var main = pullFx.main;
        main.startSpeed = -pullFxSpeed; // 負速度 = 從外圈往中心
        main.startLifetime = pullRadius / Mathf.Max(pullFxSpeed, 0.01f); // 剛好在中心消失
    }

    void SetIndicator(bool on)
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(on);
    }

    // ---------- 引力 ----------

    public enum PullTarget { Player, Object, Bullet }

    /// <summary>
    /// 取得某位置受到的引力 (朝黑洞)。大小 = 該對象的吸力 (playerPull / objectPull / bulletPull) × 強度；
    /// strength = 0 (範圍外) ~ 1 (中心)。只有「已固定」的黑洞有引力。
    /// </summary>
    public static Vector2 SamplePull(Vector2 position, PullTarget target, out float strength)
    {
        strength = 0f;
        var hole = Current;
        if (hole == null || hole.IsFlying) return Vector2.zero;

        Vector2 toHole = (Vector2)hole.transform.position - position;
        float dist = toHole.magnitude;
        if (dist >= hole.pullRadius) return Vector2.zero;

        strength = 1f - dist / hole.pullRadius;
        // 非常靠近中心時拉力漸弱，避免來回抖動
        float nearFade = Mathf.Clamp01(dist / 0.3f);
        float pull = target == PullTarget.Player ? hole.playerPull : target == PullTarget.Object ? hole.objectPull : hole.bulletPull;
        return toHole / Mathf.Max(dist, 0.0001f) * (pull * strength * nearFade);
    }

    /// <summary>目前黑洞的阻尼 (沒有黑洞時 0)。</summary>
    public static float Damping => Current != null ? Current.damping : 0f;
}
