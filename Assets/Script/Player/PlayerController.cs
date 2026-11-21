using UnityEngine;

/// <summary>
/// 橫向移動 + 跳躍。使用 Rigidbody2D (Dynamic) + CapsuleCollider2D (摩擦力 0 的物理材質)。
/// 斜坡處理：站在地面時關掉重力，速度沿著地面方向設定 → 上下坡等速、靜止不下滑。
/// 飛行 (火箭)：SetFlying 開啟後沒有重力，垂直速度固定。
/// 黑洞引力：每個物理步向 BlackHole.SamplePull 取得拉力，當作「額外速度」疊加上去。
/// </summary>
[RequireComponent(typeof(Rigidbody2D), typeof(CapsuleCollider2D))]
public class PlayerController : MonoBehaviour
{
    [Header("移動")]
    public float moveSpeed = 6f;

    [Header("跳躍")]
    public float jumpVelocity = 12f;
    [Tooltip("離開平台後仍可起跳的時間")]
    public float coyoteTime = 0.1f;
    [Tooltip("落地前提早按跳躍會被記住的時間")]
    public float jumpBufferTime = 0.1f;
    [Tooltip("上升中放開跳躍鍵時，垂直速度乘上這個值 (小跳)")]
    [Range(0f, 1f)] public float jumpCutMultiplier = 0.5f;

    [Header("地面偵測")]
    public LayerMask groundMask;
    [Range(0f, 89f)] public float maxSlopeAngle = 50f;
    [Tooltip("下坡/過坡頂時往下吸附的距離，避免彈飛")]
    public float snapDistance = 0.3f;

    [Header("黑洞")]
    [Tooltip("往上的拉力超過這個值 (格/秒) 就會把玩家拉離地面")]
    public float pullLiftThreshold = 0.5f;

    [Header("外觀")]
    public SpriteRenderer sprite;

    public bool IsGrounded { get; private set; }
    public bool IsFlying { get; private set; }
    public int Facing { get; private set; } = 1;
    public Rigidbody2D Body => rb;

    Rigidbody2D rb;
    CapsuleCollider2D col;
    float defaultGravity;
    ContactFilter2D groundFilter;
    readonly RaycastHit2D[] hits = new RaycastHit2D[4];
    Vector2 groundNormal = Vector2.up;

    float lastGroundedTime = -99f;
    float lastJumpPressedTime = -99f;
    bool jumping;
    bool jumpCutDone;
    float flyRiseSpeed;
    float lastPullY; // 上一步疊加的垂直拉力 (空中時要先扣掉，才不會越加越快)

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<CapsuleCollider2D>();
        defaultGravity = rb.gravityScale;
        groundFilter.SetLayerMask(groundMask);
        groundFilter.useTriggers = false;
    }

    void Update()
    {
        if (GameInput.JumpPressed) lastJumpPressedTime = Time.time;

        float x = GameInput.Move;
        if (x > 0.01f) Facing = 1;
        else if (x < -0.01f) Facing = -1;
        if (sprite != null) sprite.flipX = Facing < 0;
    }

    void FixedUpdate()
    {
        float x = GameInput.Move;
        Vector2 pull = BlackHole.SamplePull(rb.position, out float pullStrength);

        // 火箭飛行：沒有重力、垂直速度固定，左右照常
        if (IsFlying)
        {
            IsGrounded = false;
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(x * moveSpeed + pull.x, flyRiseSpeed + pull.y);
            lastPullY = pull.y;
            return;
        }

        bool wasGrounded = IsGrounded;
        CheckGround(wasGrounded, pull);

        Vector2 v = rb.linearVelocity;

        // 起跳
        bool canJump = !jumping && Time.time - lastGroundedTime <= coyoteTime;
        if (canJump && Time.time - lastJumpPressedTime <= jumpBufferTime)
        {
            StartJump(allowCut: true);
            rb.linearVelocity = new Vector2(x * moveSpeed, jumpVelocity);
            lastPullY = 0f;
            return;
        }

        if (IsGrounded)
        {
            rb.gravityScale = 0f;
            Vector2 along = new Vector2(groundNormal.y, -groundNormal.x); // 沿地面往右的方向
            v = along * (x * moveSpeed) + new Vector2(pull.x, 0f); // 地上只吃水平拉力；往上夠大會在 CheckGround 離地
            lastPullY = 0f;
        }
        else
        {
            // 越靠近黑洞重力越小，自己的垂直速度也被抵銷 → 會被吸過去並停在黑洞附近
            rb.gravityScale = defaultGravity * (1f - pullStrength);
            float ownY = v.y - lastPullY;
            ownY = Mathf.MoveTowards(ownY, 0f, pullStrength * BlackHole.Damping * Time.fixedDeltaTime);

            // 小跳：上升中放開跳躍鍵
            if (jumping && !jumpCutDone && ownY > 0f && !GameInput.JumpHeld)
            {
                ownY *= jumpCutMultiplier;
                jumpCutDone = true;
            }

            v = new Vector2(x * moveSpeed + pull.x, ownY + pull.y);
            lastPullY = pull.y;
        }

        rb.linearVelocity = v;
    }

    void CheckGround(bool wasGrounded, Vector2 pull)
    {
        IsGrounded = false;

        // 跳躍上升中不算著地
        if (jumping && rb.linearVelocity.y > 0.01f) return;
        // 黑洞往上拉得夠大 → 離地
        if (pull.y > pullLiftThreshold) return;

        // 剛剛還在地上 (沒在跳) → 用較長距離偵測並吸附，避免過坡頂/下坡時飛出去
        float distance = wasGrounded && !jumping ? snapDistance : 0.05f;
        int count = col.Cast(Vector2.down, groundFilter, hits, distance);
        float minNormalY = Mathf.Cos(maxSlopeAngle * Mathf.Deg2Rad);

        for (int i = 0; i < count; i++)
        {
            if (hits[i].normal.y < minNormalY) continue; // 牆壁或太陡
            IsGrounded = true;
            groundNormal = hits[i].normal;
            if (hits[i].distance > 0.02f) rb.position += Vector2.down * (hits[i].distance - 0.01f);
            break;
        }

        if (IsGrounded)
        {
            lastGroundedTime = Time.time;
            jumping = false;
        }
        else groundNormal = Vector2.up;
    }

    void StartJump(bool allowCut)
    {
        jumping = true;
        jumpCutDone = !allowCut;
        IsGrounded = false;
        lastGroundedTime = -99f;
        lastJumpPressedTime = -99f;
        rb.gravityScale = defaultGravity;
    }

    // ---------- 給道具 / GameManager 用的 API ----------

    /// <summary>強制往上彈 (道具用)，空中也有效，不受「放開跳躍鍵小跳」影響。</summary>
    public void ForceJump(float velocity)
    {
        StartJump(allowCut: false);
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, velocity);
    }

    /// <summary>開始/結束飛行 (火箭用)。結束後照一般重力落下，沒有 coyote 跳。</summary>
    public void SetFlying(bool flying, float riseSpeed = 0f)
    {
        if (IsFlying == flying) { flyRiseSpeed = riseSpeed; return; }
        IsFlying = flying;
        flyRiseSpeed = riseSpeed;
        jumping = false;
        jumpCutDone = true;
        lastGroundedTime = -99f;
        if (!flying)
        {
            rb.gravityScale = defaultGravity;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            lastPullY = 0f;
        }
    }

    public void Teleport(Vector2 position)
    {
        rb.position = position;
        transform.position = position;
        rb.linearVelocity = Vector2.zero;
        lastPullY = 0f;
    }

    /// <summary>凍結/解凍玩家 (死亡、轉場時)。</summary>
    public void SetFrozen(bool frozen)
    {
        rb.linearVelocity = Vector2.zero;
        rb.simulated = !frozen;
    }
}
