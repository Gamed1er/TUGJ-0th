using UnityEngine;

/// <summary>
/// 玩家的逐格動畫 (直接換 sprite，不用 Animator Controller)。
/// - 在地上移動 → 依序播 walkFrames，每格 frameTime 秒；站著不動 → idleFrame
/// - 離地 (跳躍、掉落、火箭飛行) → 依垂直速度從 jumpFrames 選一格：
///   速度 ≥ +jumpSpeedRange = 第一格 (剛起跳)，≤ -jumpSpeedRange = 最後一格 (下落)，中間線性對應 (接近最高點 = 中間的格)
/// 拿槍 (傳送槍 / 黑洞槍)：選中那一格 → 播拿槍動畫 (draw) 一次，之後停在最後一格；射擊成功 (PlayShot) → 播射擊動畫一次。
///   站著 / 射擊時角色面向滑鼠。拿槍走路 → 循環播 teleportWalk / blackHoleWalk (面向移動方向)，不播拿槍動畫；
///   停下來時直接停在拿槍動畫的最後一格。拿槍時空中也循環播拿槍走路的圖 (沒拿槍才用 jumpFrames)。
/// 操控無人機 (Drone.Controlled)：播 padTake 一次 → 循環 padUsing。
/// 優先順序：空中 (沒拿槍) > 無人機平板 > 射擊 > 拿槍走路 / 拿槍空中 > 走路 > 拿槍 > 站立。
/// 特效：落地 (landFx)、走路落腳 (walkFx + walkSfx，walkFxFrames 指定哪幾格)。
/// 左右翻轉平常由 PlayerController 處理 (sprite.flipX)，拿槍站著時這裡改成面向滑鼠。
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

    [Header("拿槍 (第 1 組 = 傳送槍、第 2 組 = 黑洞槍)")]
    [Tooltip("傳送槍：拿出來的動畫 (最後一格 = 拿著的樣子)")]
    [SerializeField] Sprite[] teleportDraw;
    [Tooltip("傳送槍：射擊動畫")]
    [SerializeField] Sprite[] teleportShot;
    [Tooltip("傳送槍：拿槍走路 (循環，速度同 frameTime)")]
    [SerializeField] Sprite[] teleportWalk;
    [Tooltip("黑洞槍：拿出來的動畫 (最後一格 = 拿著的樣子)")]
    [SerializeField] Sprite[] blackHoleDraw;
    [Tooltip("黑洞槍：射擊動畫")]
    [SerializeField] Sprite[] blackHoleShot;
    [Tooltip("黑洞槍：拿槍走路 (循環，速度同 frameTime)")]
    [SerializeField] Sprite[] blackHoleWalk;
    [Tooltip("拿槍動畫每秒幾格")]
    [SerializeField] float drawFps = 18f;
    [Tooltip("射擊動畫每秒幾格")]
    [SerializeField] float shotFps = 16f;

    [Header("無人機平板")]
    [Tooltip("開始操控時拿出平板 (播一次)")]
    [SerializeField] Sprite[] padTake;
    [Tooltip("操控中 (循環)")]
    [SerializeField] Sprite[] padUsing;
    [SerializeField] float padFps = 12f;

    [Header("特效")]
    [Tooltip("落地揚起的灰塵 (自己播完自己消失)")]
    [SerializeField] GameObject landFx;
    [Tooltip("在空中超過這個秒數再落地才播灰塵")]
    [SerializeField] float landFxMinAirTime = 0.25f;
    [Tooltip("走路的灰塵 (自己播完自己消失)")]
    [SerializeField] GameObject walkFx;
    [Tooltip("走路動畫播到這幾格 (從 0 開始) 時噴灰塵 = 落腳的格子")]
    [SerializeField] int[] walkFxFrames = { 0, 2 };
    [Tooltip("走路落腳的音效 (跟灰塵同一格播)")]
    [SerializeField] AudioClip walkSfx;
    [SerializeField, Range(0f, 1f)] float walkSfxVolume = 0.5f;
    [Tooltip("每一步隨機的音高範圍 (1 = 原音高)")]
    [SerializeField] Vector2 walkSfxPitch = new Vector2(0.85f, 1.15f);

    PlayerController player;
    Inventory inventory;
    float timer;
    ItemDefinition lastSelected; // 換格子 → 重播拿槍動畫
    float drawTime;              // 拿槍動畫播了多久
    float shotTime = -1f;        // 射擊動畫播了多久 (< 0 = 沒在播)
    bool wasControlling;
    float padTime;
    float airTime;
    int lastWalkFrame = -1;

    Collider2D body;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Collider2D>();
        inventory = GetComponent<Inventory>();
    }

    /// <summary>槍射擊成功時由道具呼叫，播一次射擊動畫。</summary>
    public void PlayShot() => shotTime = 0f;

    /// <summary>目前選中的槍的動畫 (沒拿槍回傳 false)。</summary>
    bool GunFrames(out Sprite[] draw, out Sprite[] shot, out Sprite[] walk)
    {
        var def = inventory != null && inventory.SelectedItem != null ? inventory.SelectedItem.definition : null;
        draw = shot = walk = null;
        if (def is TeleportGunItem) { draw = teleportDraw; shot = teleportShot; walk = teleportWalk; }
        else if (def is BlackHoleGunItem) { draw = blackHoleDraw; shot = blackHoleShot; walk = blackHoleWalk; }
        return draw != null && draw.Length > 0;
    }

    static Sprite Frame(Sprite[] frames, float time, float fps, bool loop)
    {
        int i = Mathf.FloorToInt(time * fps);
        i = loop ? i % frames.Length : Mathf.Min(i, frames.Length - 1);
        return frames[i];
    }

    void FaceMouse() => sprite.flipX = GameInput.MouseWorld.x < player.Body.position.x;

    void LateUpdate()
    {
        if (sprite == null) return;

        // 計時：換格子重播拿槍；射擊；無人機平板
        var selected = inventory != null && inventory.SelectedItem != null ? inventory.SelectedItem.definition : null;
        if (selected != lastSelected) { lastSelected = selected; drawTime = 0f; shotTime = -1f; }
        drawTime += Time.deltaTime;
        if (shotTime >= 0f) shotTime += Time.deltaTime;
        bool controlling = Drone.Controlled != null;
        if (controlling && !wasControlling) padTime = 0f;
        wasControlling = controlling;
        padTime += Time.deltaTime;
        bool hasGun = GunFrames(out var draw, out var shot, out var gunWalk);
        bool shooting = hasGun && shot != null && shot.Length > 0 && shotTime >= 0f && shotTime < shot.Length / shotFps;

        if (player.IsGrounded && airTime > landFxMinAirTime && landFx != null)
            Instantiate(landFx, new Vector3(body.bounds.center.x, body.bounds.min.y), Quaternion.identity);
        airTime = player.IsGrounded ? 0f : airTime + Time.deltaTime;
        // 拿槍時空中也用拿槍走路的圖 (不然槍會消失)，交給下面的走路處理
        bool gunAir = airTime > airDelay && hasGun && gunWalk != null && gunWalk.Length > 0;
        if (airTime > airDelay && !gunAir && jumpFrames != null && jumpFrames.Length > 0)
        {
            timer = 0f;
            float vy = player.Body.linearVelocity.y;
            float t = Mathf.InverseLerp(jumpSpeedRange, -jumpSpeedRange, vy); // 0 = 上升最快、1 = 下落最快
            int frame = Mathf.Clamp(Mathf.FloorToInt(t * jumpFrames.Length), 0, jumpFrames.Length - 1);
            sprite.sprite = jumpFrames[frame];
            return;
        }

        // 操控無人機：拿出平板 → 循環操作
        if (controlling && padTake != null && padTake.Length > 0)
        {
            timer = 0f;
            float takeLen = padTake.Length / padFps;
            sprite.sprite = padTime < takeLen || padUsing == null || padUsing.Length == 0
                ? Frame(padTake, padTime, padFps, false)
                : Frame(padUsing, padTime - takeLen, padFps, true);
            return;
        }

        // 射擊 (走路中射擊也會停下來播)
        if (shooting)
        {
            timer = 0f;
            drawTime = 1000f; // 射擊後就算拿好槍了 (直接停在最後一格)
            sprite.sprite = Frame(shot, shotTime, shotFps, false);
            FaceMouse();
            return;
        }

        bool walking = (gunAir || player.IsGrounded && Mathf.Abs(player.MoveInput) > 0.01f) && walkFrames != null && walkFrames.Length > 0;
        if (!walking)
        {
            timer = 0f;
            lastWalkFrame = -1;
            if (hasGun)
            {
                sprite.sprite = Frame(draw, drawTime, drawFps, false); // 播完停在最後一格 = 拿著槍
                FaceMouse();
            }
            else if (idleFrame != null) sprite.sprite = idleFrame;
            return;
        }

        // 走路 (拿槍 → 拿槍走路的圖；面向由 PlayerController 依移動方向決定)
        var frames = walkFrames;
        if (hasGun && gunWalk != null && gunWalk.Length > 0)
        {
            frames = gunWalk;
            drawTime = 1000f; // 走路中不播拿槍動畫，停下來直接是拿好槍的樣子
        }
        timer += Time.deltaTime;
        int walkFrame = (int)(timer / frameTime) % frames.Length;
        sprite.sprite = frames[walkFrame];
        if (walkFrame != lastWalkFrame)
        {
            lastWalkFrame = walkFrame;
            if (player.IsGrounded && System.Array.IndexOf(walkFxFrames, walkFrame) >= 0)
            {
                if (walkFx != null) Instantiate(walkFx, new Vector3(body.bounds.center.x, body.bounds.min.y), Quaternion.identity);
                Sfx.PlayPitched(walkSfx, walkSfxVolume, walkSfxPitch);
            }
        }
    }
}
