using UnityEngine;

/// <summary>
/// 電池倉：往滑鼠方向生成一顆電池 (規則見 SpawnCell：最遠 maxDistance 格、對齊格子、被擋住就往玩家的方向退、不能隔牆)。
/// 懸空也可以放 (會掉下去)。完全找不到就不生成、不扣次數。生成位置的預覽由 SpawnPreview 顯示。
/// </summary>
[CreateAssetMenu(menuName = "Items/Battery")]
public class BatteryItem : ItemDefinition, ISpawnPreviewItem
{
    public GameObject batteryPrefab;
    [Tooltip("離玩家最遠幾格")]
    public float maxDistance = 2f;
    [Tooltip("生成時的特效 (自己播完自己消失)")]
    public GameObject spawnFx;
    [Tooltip("擋住視線的圖層 (不能隔著它生成)")]
    public LayerMask wallMask = 1 << 8;

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (batteryPrefab == null || !ctx.CheckUses()) return;
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
        SpawnCell.Find(ctx, maxDistance, Vector2.one * 0.9f, wallMask, out cell);

    public bool ShowPreview(ItemUseContext ctx) => ctx.item.uses > 0;

    public Sprite PreviewSprite => batteryPrefab != null ? batteryPrefab.GetComponent<SpriteRenderer>().sprite : null;
}
