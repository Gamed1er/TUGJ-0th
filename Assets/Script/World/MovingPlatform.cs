using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 軌道平台。啟動 (見 Activatable) → 往 end 移動；關閉 → 回到起點 (body 一開始的位置)，中途切換會從當下位置折返。
/// 結構：根物件 (這個腳本，不會動) / Body (Kinematic Rigidbody2D + 碰撞體，Ground 圖層) / End (終點標記) / Track (軌道外觀，自動拉伸)。
///
/// 每個物理步：
/// - 站在平台上的東西 (玩家、電池、沒電的無人機) 跟著移動；被牆擋住就留在原地 (平台從腳下滑走)
/// - 擋在移動方向前面的東西 (玩家、電池、無人機) 被推著走；推不動 → 玩家死亡、其他東西直接摧毀
/// 黑洞不受影響 (它沒有碰撞體)。
/// </summary>
public class MovingPlatform : Activatable
{
    [SerializeField] Rigidbody2D body;
    [SerializeField] Transform end;
    [Tooltip("軌道外觀 (1x1 的 sprite，會自動拉伸到起點~終點)")]
    [SerializeField] SpriteRenderer track;
    [Tooltip("移動速度 (格/秒)")]
    public float speed = 3f;
    [Tooltip("會擋住被推物體的圖層 (地形、門、其他平台)")]
    public LayerMask blockMask;

    Vector2 startPos, endPos;
    Collider2D bodyCol;
    readonly RaycastHit2D[] hits = new RaycastHit2D[16];
    readonly RaycastHit2D[] blockHits = new RaycastHit2D[8];
    readonly Collider2D[] overlap = new Collider2D[16];
    readonly HashSet<Rigidbody2D> front = new HashSet<Rigidbody2D>();
    readonly HashSet<Rigidbody2D> riders = new HashSet<Rigidbody2D>();

    void Awake()
    {
        bodyCol = body.GetComponent<Collider2D>();
        startPos = body.position;
        endPos = end.position;
        LayoutTrack();
    }

    void FixedUpdate()
    {
        Vector2 pos = body.position;
        Vector2 target = Active ? endPos : startPos;
        Vector2 next = Vector2.MoveTowards(pos, target, speed * Time.fixedDeltaTime);
        Vector2 delta = next - pos;
        if (delta.sqrMagnitude < 1e-8f) return;

        CollectMovers(delta);

        foreach (var rb in front) Push(rb, delta, crushIfBlocked: true);
        foreach (var rb in riders) if (!front.Contains(rb)) Push(rb, delta, crushIfBlocked: false);

        body.position = next;
        body.transform.position = next;
    }

    /// <summary>找出前方會被推的東西與站在上面的東西。</summary>
    void CollectMovers(Vector2 delta)
    {
        front.Clear();
        riders.Clear();
        var filter = new ContactFilter2D { useTriggers = false };

        // 前方：把平台的形狀往移動方向投射
        int n = bodyCol.Cast(delta.normalized, filter, hits, delta.magnitude + 0.02f);
        for (int i = 0; i < n; i++)
            if (IsMovable(hits[i].collider, rider: false)) front.Add(hits[i].rigidbody);

        // 上方：貼在平台頂面的東西
        Bounds b = bodyCol.bounds;
        var center = new Vector2(b.center.x, b.max.y + 0.05f);
        var size = new Vector2(b.size.x - 0.05f, 0.1f);
        n = Physics2D.OverlapBox(center, size, 0f, filter, overlap);
        for (int i = 0; i < n; i++)
            if (IsMovable(overlap[i], rider: true)) riders.Add(overlap[i].attachedRigidbody);
    }

    /// <summary>平台會帶動 / 推動的東西。站在上面的只算「受重力影響」的 (有電的無人機懸空，不算)。</summary>
    bool IsMovable(Collider2D c, bool rider)
    {
        if (c == bodyCol || c.attachedRigidbody == null) return false;
        if (c.GetComponent<PlayerController>() != null) return true;
        if (c.GetComponentInParent<Battery>() != null) return !rider || !c.GetComponentInParent<Battery>().IsCarried;
        var drone = c.GetComponentInParent<Drone>();
        if (drone != null) return !rider || !drone.IsPowered;
        return false;
    }

    void Push(Rigidbody2D rb, Vector2 delta, bool crushIfBlocked)
    {
        if (rb == null) return;
        if (IsBlocked(rb, delta))
        {
            if (crushIfBlocked) Crush(rb);
            return;
        }
        rb.position += delta;
        rb.transform.position = rb.position;
    }

    /// <summary>這個物體往 delta 方向移動會不會撞到牆 (地形、門、其他平台)。</summary>
    bool IsBlocked(Rigidbody2D rb, Vector2 delta)
    {
        var filter = new ContactFilter2D { useTriggers = false };
        filter.SetLayerMask(blockMask);
        int n = rb.Cast(delta.normalized, filter, blockHits, delta.magnitude);
        for (int i = 0; i < n; i++)
        {
            var c = blockHits[i].collider;
            if (c == bodyCol) continue;
            if (c.GetComponentInParent<Battery>() != null || c.GetComponentInParent<Drone>() != null) continue; // 可移動物不算牆
            return true;
        }
        return false;
    }

    static void Crush(Rigidbody2D rb)
    {
        if (rb.GetComponent<PlayerController>() != null)
        {
            GameManager.Instance.KillPlayer();
            return;
        }
        var drone = rb.GetComponentInParent<Drone>();
        if (drone != null) { drone.Remove(); return; }
        Object.Destroy(rb.gameObject);
    }

    /// <summary>把軌道外觀拉成起點~終點的一條線。</summary>
    void LayoutTrack()
    {
        if (track == null) return;
        Vector2 a = startPos, b = endPos;
        Vector2 d = b - a;
        track.transform.position = (a + b) * 0.5f;
        track.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
        track.transform.localScale = new Vector3(d.magnitude + 0.2f, 0.12f, 1f);
    }

    void OnDrawGizmos()
    {
        if (body == null || end == null) return;
        Gizmos.color = Color.cyan;
        Vector3 a = Application.isPlaying ? (Vector3)startPos : body.transform.position;
        Gizmos.DrawLine(a, end.position);
        Gizmos.DrawWireSphere(end.position, 0.2f);
    }
}
