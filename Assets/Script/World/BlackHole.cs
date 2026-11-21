using UnityEngine;

/// <summary>
/// 黑洞槍射出的黑洞。場上同時只會有一顆 (BlackHole.Current)。
/// 飛行中：直線前進 → 撞到地形/門 = 貼在表面固定；撞到 Breakable = 破壞它並消失；玩家、電池、觸發區直接穿過。
/// 固定後：對範圍內的玩家與電池產生引力 (由它們自己呼叫 SamplePull 取得)。
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

    [Header("引力")]
    [Tooltip("作用半徑 (格)")]
    public float pullRadius = 4f;
    [Tooltip("中心處的最大拉力 (格/秒)。必須小於玩家走路速度與火箭上升速度，玩家才逃得掉")]
    public float maxPullSpeed = 3.5f;
    [Tooltip("靠近中心時，抵銷原本速度的強度 (讓物體可以停在黑洞附近)")]
    public float damping = 15f;

    [Header("外觀")]
    [Tooltip("固定後才顯示的引力範圍圈")]
    public GameObject rangeIndicator;

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
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(hitMask);
        int count = Physics2D.CircleCast(pos, hitRadius, direction, filter, hits, step);

        // 找最近的有效碰撞 (略過觸發區與電池)
        int best = -1;
        for (int i = 0; i < count; i++)
        {
            var c = hits[i].collider;
            if (c.isTrigger || c.GetComponentInParent<Battery>() != null) continue;
            if (best < 0 || hits[i].distance < hits[best].distance) best = i;
        }

        if (best < 0)
        {
            transform.position = pos + direction * step;
            return;
        }

        var breakable = hits[best].collider.GetComponentInParent<Breakable>();
        if (breakable != null)
        {
            breakable.Break();
            Destroy(gameObject);
            return;
        }

        // 貼在表面 (centroid = 圓形剛碰到時的圓心，不會陷進牆裡)
        transform.position = hits[best].centroid;
        Fix();
    }

    void SetIndicator(bool on)
    {
        if (rangeIndicator != null) rangeIndicator.SetActive(on);
    }

    // ---------- 引力 ----------

    /// <summary>
    /// 取得某位置受到的引力。回傳朝黑洞的速度 (格/秒)；strength = 0 (範圍外) ~ 1 (中心)。
    /// 只有「已固定」的黑洞有引力。
    /// </summary>
    public static Vector2 SamplePull(Vector2 position, out float strength)
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
        return toHole / Mathf.Max(dist, 0.0001f) * (hole.maxPullSpeed * strength * nearFade);
    }

    /// <summary>目前黑洞的阻尼 (沒有黑洞時 0)。</summary>
    public static float Damping => Current != null ? Current.damping : 0f;
}
