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

    public RocketState State { get; private set; }
    /// <summary>藍條比例：蓄力中 = 蓄力進度；飛行中 = 剩餘時間。</summary>
    public float Fill { get; private set; }

    PlayerController player;
    RocketItem config;
    Inventory inventory;
    ItemInstance item;
    float timer;

    void Awake() => player = GetComponent<PlayerController>();

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
