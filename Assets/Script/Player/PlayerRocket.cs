using UnityEngine;

/// <summary>
/// 火箭推進器的執行狀態 (掛在玩家身上)。RocketItem 只負責轉發右鍵，參數也放在 RocketItem 資產上。
/// 流程：按住 → 蓄力 chargeTime 秒 (期間放開 = 誤觸，什麼都不發生) → 扣 1 次並起飛
///      → 放開 / 時間用完 / 切換格子 → 結束飛行。
/// ItemStatusUI 讀 State 與 Fill 來畫藍條。
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerRocket : MonoBehaviour
{
    public enum RocketState { Idle, Charging, Flying }

    [Tooltip("飛行時的噴射粒子 (玩家腳下、往下噴)")]
    [SerializeField] ParticleSystem thrustFx;
    [Tooltip("尾氣判定範圍 (玩家腳底往下的長方形，寬 x 高)。飛行中碰到的電池會被點燃 (Battery.fuseTime 秒後爆炸)")]
    [SerializeField] Vector2 exhaustSize = new Vector2(1f, 2f);

    readonly Collider2D[] exhaustHits = new Collider2D[8];
    Collider2D body;

    public RocketState State { get; private set; }
    /// <summary>藍條比例：蓄力中 = 蓄力進度；飛行中 = 剩餘時間。</summary>
    public float Fill { get; private set; }

    PlayerController player;
    RocketItem config;
    Inventory inventory;
    ItemInstance item;
    float timer;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        body = GetComponent<Collider2D>();
    }

    void FixedUpdate()
    {
        if (State != RocketState.Flying) return;

        // 尾氣：腳底往下的長方形範圍內的電池被點燃 (倒數後爆炸)
        Bounds b = body.bounds;
        var center = new Vector2(b.center.x, b.min.y - exhaustSize.y * 0.5f);
        var filter = new ContactFilter2D { useTriggers = false };
        int n = Physics2D.OverlapBox(center, exhaustSize, 0f, filter, exhaustHits);
        for (int i = 0; i < n; i++)
        {
            var battery = exhaustHits[i].GetComponentInParent<Battery>();
            if (battery != null && !battery.rocketProof) battery.StartFuse(); // 點燃引信，倒數後爆炸
        }
    }

    void OnDrawGizmosSelected()
    {
        var col = GetComponent<Collider2D>();
        if (col == null) return;
        Bounds b = col.bounds;
        Gizmos.color = new Color(1f, 0.6f, 0.2f, 0.8f);
        Gizmos.DrawWireCube(new Vector3(b.center.x, b.min.y - exhaustSize.y * 0.5f), exhaustSize);
    }

    public void BeginCharge(RocketItem config, Inventory inventory, ItemInstance item)
    {
        if (State != RocketState.Idle) return;
        this.config = config;
        this.inventory = inventory;
        this.item = item;
        timer = 0f;
        State = RocketState.Charging;
    }

    public void Stop()
    {
        if (State == RocketState.Flying) player.SetFlying(false);
        if (thrustFx != null) thrustFx.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        State = RocketState.Idle;
        Fill = 0f;
    }

    void Update()
    {
        switch (State)
        {
            case RocketState.Charging:
                timer += Time.deltaTime;
                Fill = config.chargeTime > 0f ? Mathf.Clamp01(timer / config.chargeTime) : 1f;
                if (timer >= config.chargeTime)
                {
                    inventory.Consume(item); // 生效時無論飛多久都只扣一次
                    State = RocketState.Flying;
                    timer = config.flightDuration;
                    player.SetFlying(true, config.riseSpeed);
                    if (thrustFx != null) thrustFx.Play();
                }
                break;

            case RocketState.Flying:
                timer -= Time.deltaTime;
                Fill = config.flightDuration > 0f ? Mathf.Clamp01(timer / config.flightDuration) : 0f;
                if (timer <= 0f) Stop();
                break;
        }
    }

    void OnDisable() => Stop();
}
