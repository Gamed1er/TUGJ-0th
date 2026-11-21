using UnityEngine;

/// <summary>
/// 無人機。右鍵：
/// - 場上沒有有電的無人機 → 在玩家身上召喚一台新的 (扣 1 次) 並開始操控
/// - 場上有有電的無人機 → 重新操控它 (不扣次數)
/// - 操控中 → 退出操控 (由 PlayerItemUser 統一處理)
/// 無人機本身的參數 (速度、互動距離...) 在 Drone prefab 上調整。
/// </summary>
[CreateAssetMenu(menuName = "Items/Drone")]
public class DroneItem : ItemDefinition
{
    public Drone dronePrefab;

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (Drone.Powered != null)
        {
            Drone.Powered.BeginControl();
            return;
        }
        if (dronePrefab == null || !ctx.CheckUses()) return;

        var drone = Instantiate(dronePrefab, ctx.player.Body.position, Quaternion.identity);
        ctx.Consume();
        drone.BeginControl();
    }
}
