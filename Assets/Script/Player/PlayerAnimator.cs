using UnityEngine;

/// <summary>
/// 玩家的逐格動畫 (直接換 sprite，不用 Animator Controller)。
/// 在地上移動 → 依序播 walkFrames，每格 frameTime 秒；站著不動或在空中 → 顯示 idleFrame。
/// 左右翻轉由 PlayerController 處理 (sprite.flipX)。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerAnimator : MonoBehaviour
{
    [SerializeField] SpriteRenderer sprite;
    [Tooltip("走路動畫 (依序播放)")]
    [SerializeField] Sprite[] walkFrames;
    [Tooltip("站立 / 空中顯示的圖")]
    [SerializeField] Sprite idleFrame;
    [SerializeField] float frameTime = 0.15f;

    PlayerController player;
    float timer;

    void Awake() => player = GetComponent<PlayerController>();

    void LateUpdate()
    {
        if (sprite == null) return;

        bool walking = player.IsGrounded && Mathf.Abs(player.MoveInput) > 0.01f && walkFrames != null && walkFrames.Length > 0;
        if (!walking)
        {
            timer = 0f;
            if (idleFrame != null) sprite.sprite = idleFrame;
            return;
        }

        timer += Time.deltaTime;
        int frame = (int)(timer / frameTime) % walkFrames.Length;
        sprite.sprite = walkFrames[frame];
    }
}
