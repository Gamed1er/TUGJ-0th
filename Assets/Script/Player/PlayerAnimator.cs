using UnityEngine;

/// <summary>
/// 玩家的逐格動畫 (直接換 sprite，不用 Animator Controller)。
/// - 在地上移動 → 依序播 walkFrames，每格 frameTime 秒；站著不動 → idleFrame
/// - 離地 (跳躍、掉落、火箭飛行) → 依垂直速度從 jumpFrames 選一格：
///   速度 ≥ +jumpSpeedRange = 第一格 (剛起跳)，≤ -jumpSpeedRange = 最後一格 (下落)，中間線性對應 (接近最高點 = 中間的格)
/// 左右翻轉由 PlayerController 處理 (sprite.flipX)。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [Tooltip("走路動畫 (依序播放)")]
    [SerializeField] Sprite[] walkFrames;
    [Tooltip("站立時顯示的圖")]
    [SerializeField] Sprite idleFrame;
    [SerializeField] float frameTime = 0.15f;
    [Tooltip("空中的圖 (上升 → 最高點 → 下落)")]
    [SerializeField] Sprite[] jumpFrames;
    [Tooltip("垂直速度 (格/秒) 在 ±這個值之間對應到 jumpFrames 的第一格 ~ 最後一格")]
    [SerializeField] float jumpSpeedRange = 8f;

    [Tooltip("離地超過這個秒數才換成空中的圖 (避免走過小坎時閃一下)")]
    [SerializeField] float airDelay = 0.06f;

    PlayerController player;
    float timer;
    float airTime;

    void Awake() => player = GetComponent<PlayerController>();

    void LateUpdate()
    {
        if (sprite == null) return;

        airTime = player.IsGrounded ? 0f : airTime + Time.deltaTime;
        if (airTime > airDelay && jumpFrames != null && jumpFrames.Length > 0)
        {
            timer = 0f;
            float vy = player.Body.linearVelocity.y;
            float t = Mathf.InverseLerp(jumpSpeedRange, -jumpSpeedRange, vy); // 0 = 上升最快、1 = 下落最快
            int frame = Mathf.Clamp(Mathf.FloorToInt(t * jumpFrames.Length), 0, jumpFrames.Length - 1);
            sprite.sprite = jumpFrames[frame];
            return;
        }

        bool walking = player.IsGrounded && Mathf.Abs(player.MoveInput) > 0.01f && walkFrames != null && walkFrames.Length > 0;
        if (!walking)
        {
            timer = 0f;
            if (idleFrame != null) sprite.sprite = idleFrame;
            return;
        }

        timer += Time.deltaTime;
        int walkFrame = (int)(timer / frameTime) % walkFrames.Length;
        sprite.sprite = walkFrames[walkFrame];
    }
}
