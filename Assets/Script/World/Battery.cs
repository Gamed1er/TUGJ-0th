using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 電池倉生成的電池：1x1、有碰撞、不會旋轉，玩家走過去就能推，也能站在上面。
/// 會被固定的黑洞吸引；可以壓住壓力板。不存檔 (死亡重載就消失)。
/// 可以被無人機吊起來 (SetCarried)：吊著時是 Kinematic、跟著無人機、不受黑洞影響、不跟玩家碰撞。
/// 跟無人機永遠不碰撞 (Drone 生成時會設定，這裡處理電池比無人機晚生成的情況)。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Battery : MonoBehaviour
{
    public static readonly List<Battery> All = new List<Battery>();

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
