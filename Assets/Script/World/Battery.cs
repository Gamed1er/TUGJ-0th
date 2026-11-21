using UnityEngine;

/// <summary>
/// 電池倉生成的電池：1x1、有碰撞、不會旋轉，玩家走過去就能推，也能站在上面。
/// 會被固定的黑洞吸引；可以壓住壓力板。不存檔 (死亡重載就消失)。
/// </summary>
[RequireComponent(typeof(Rigidbody2D))]
public class Battery : MonoBehaviour
{
    Rigidbody2D rb;
    float baseGravity;
    Vector2 lastPull;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        baseGravity = rb.gravityScale;
    }

    void FixedUpdate()
    {
        Vector2 pull = BlackHole.SamplePull(rb.position, out float s);
        // 越靠近黑洞重力越小，原本的速度也被抵銷 → 會被吸過去並停在黑洞附近
        rb.gravityScale = baseGravity * (1f - s);

        Vector2 own = rb.linearVelocity - lastPull; // 去掉上一步加的引力，剩下自己的速度
        own = Vector2.MoveTowards(own, Vector2.zero, s * BlackHole.Damping * Time.fixedDeltaTime);
        rb.linearVelocity = own + pull;
        lastPull = pull;
    }
}
