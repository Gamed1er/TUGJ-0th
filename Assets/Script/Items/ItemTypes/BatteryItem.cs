using UnityEngine;

/// <summary>電池倉：在玩家面向的前方一格 (對齊格子、腳邊高度) 生成一顆電池。被擋住就不生成、不扣次數。</summary>
[CreateAssetMenu(menuName = "Items/Battery")]
public class BatteryItem : ItemDefinition
{
    public GameObject batteryPrefab;

    static readonly Collider2D[] overlapBuffer = new Collider2D[1];

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (batteryPrefab == null || !ctx.CheckUses()) return;

        Bounds body = ctx.player.GetComponent<Collider2D>().bounds;
        int facing = ctx.player.Facing;
        // 玩家前緣外面第一個完整的格子
        float frontEdge = facing > 0 ? body.max.x : body.min.x;
        int cellX = facing > 0 ? Mathf.CeilToInt(frontEdge) : Mathf.FloorToInt(frontEdge) - 1;
        int cellY = Mathf.FloorToInt(body.min.y + 0.05f);
        Vector2 cell = new Vector2(cellX + 0.5f, cellY + 0.5f);

        // 有實體擋著 (地形、門、其他電池...) 就不能放；觸發區不算
        var filter = new ContactFilter2D { useTriggers = false };
        if (Physics2D.OverlapBox(cell, Vector2.one * 0.9f, 0f, filter, overlapBuffer) > 0)
        {
            ItemStatusUI.Hint("前方無法生成電池");
            return;
        }

        Instantiate(batteryPrefab, cell, Quaternion.identity);
        ctx.Consume();
    }
}
