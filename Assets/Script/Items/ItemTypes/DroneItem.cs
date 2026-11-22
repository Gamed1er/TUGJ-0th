using UnityEngine;

/// <summary>
/// 無人機。選中這一格時：
/// - 場上有有電的無人機 → 自動操控 (由 PlayerItemUser 處理)；切到別格 = 暫停操控 (無人機原地懸停)
/// - 場上沒有有電的無人機 → 右鍵在玩家腳邊左 / 右緊鄰的格子生成一台 (規則同電池倉，見 SpawnCell；空中不能生成；扣 1 次) 並開始操控
/// - 操控中右鍵沒有作用 (不能在無人機格子上停止操控)
/// 無人機本身的參數 (速度、互動距離...) 在 Drone prefab 上調整。生成位置的預覽由 SpawnPreview 顯示。
/// </summary>
[CreateAssetMenu(menuName = "Items/Drone")]
public class DroneItem : ItemDefinition, ISpawnPreviewItem
{
    public Drone dronePrefab;
    [Tooltip("擋住視線的圖層 (不能隔著它生成)")]
    public LayerMask wallMask = 1 << 8;

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (Drone.Powered != null)
        {
            Drone.Powered.BeginControl();
            return;
        }
        if (dronePrefab == null || !ctx.CheckUses()) return;
        if (!SpawnCell.CanSpawnNow(ctx))
        {
            ItemStatusUI.Hint("空中無法生成無人機");
            return;
        }
        if (!FindSpawnCell(ctx, out Vector2 cell))
        {
            ItemStatusUI.Hint("這裡無法生成無人機");
            return;
        }

        var drone = Instantiate(dronePrefab, cell, Quaternion.identity);
        ctx.Consume();
        drone.BeginControl();
    }

    public bool FindSpawnCell(ItemUseContext ctx, out Vector2 cell) =>
        SpawnCell.Find(ctx, Vector2.one * 0.9f, wallMask, out cell);

    public bool ShowPreview(ItemUseContext ctx) => Drone.Powered == null && ctx.item.uses > 0 && SpawnCell.CanSpawnNow(ctx);

    public Sprite PreviewSprite => dronePrefab != null ? dronePrefab.openSprite : null;
}
