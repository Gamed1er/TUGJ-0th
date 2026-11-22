using UnityEngine;

/// <summary>
/// 電池倉：在玩家腳邊左 / 右緊鄰的格子生成一顆電池 (滑鼠在哪一邊就放哪一邊，規則見 SpawnCell)。
/// 玩家在空中不能用。完全找不到就不生成、不扣次數。生成位置的預覽由 SpawnPreview 顯示。
/// </summary>
[CreateAssetMenu(menuName = "Items/Battery")]
public class BatteryItem : ItemDefinition, ISpawnPreviewItem
{
    public GameObject batteryPrefab;
    [Tooltip("生成時的特效 (自己播完自己消失)")]
    public GameObject spawnFx;
    [Tooltip("擋住視線的圖層 (不能隔著它生成)")]
    public LayerMask wallMask = 1 << 8;

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (batteryPrefab == null || !ctx.CheckUses()) return;
        if (!SpawnCell.CanSpawnNow(ctx))
        {
            ItemStatusUI.Hint("空中無法生成電池");
            return;
        }
        if (!FindSpawnCell(ctx, out Vector2 cell))
        {
            ItemStatusUI.Hint("這裡無法生成電池");
            return;
        }

        Instantiate(batteryPrefab, cell, Quaternion.identity).GetComponent<Battery>().SpawnedByPlayer = true;
        if (spawnFx != null) Instantiate(spawnFx, cell, Quaternion.identity);
        ctx.Consume();
    }

    public bool FindSpawnCell(ItemUseContext ctx, out Vector2 cell) =>
        SpawnCell.Find(ctx, Vector2.one * 0.9f, wallMask, out cell);

    public bool ShowPreview(ItemUseContext ctx) => ctx.item.uses > 0 && SpawnCell.CanSpawnNow(ctx);

    public Sprite PreviewSprite => batteryPrefab != null ? batteryPrefab.GetComponent<SpriteRenderer>().sprite : null;
}
