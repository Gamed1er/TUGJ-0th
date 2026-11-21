using UnityEngine;

/// <summary>
/// 火箭推進器：長按右鍵飛行。實際的飛行邏輯在玩家身上的 PlayerRocket，這裡只放參數並轉發輸入。
/// </summary>
[CreateAssetMenu(menuName = "Items/Rocket")]
public class RocketItem : ItemDefinition
{
    [Tooltip("要按住多久才算啟動 (短按視為誤觸，不扣次數)")]
    public float chargeTime = 0.15f;
    [Tooltip("一次最多飛幾秒 (藍條長度)")]
    public float flightDuration = 5f;
    [Tooltip("上升速度 (格/秒)")]
    public float riseSpeed = 4f;

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (!ctx.CheckUses()) return;
        ctx.player.GetComponent<PlayerRocket>().BeginCharge(this, ctx.inventory, ctx.item);
    }

    public override void OnUseEnd(ItemUseContext ctx)
    {
        ctx.player.GetComponent<PlayerRocket>().Stop();
    }
}
