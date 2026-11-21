using UnityEngine;

/// <summary>
/// 「請輸入文本」(二周目才有)。每個區段 (MapArea) 能用一次：
/// 在一周目通關時記錄的電池位置 (GameManager.BatteryRecords) 中，屬於目前區段的全部生成電池。
/// 生成位置跟實體 (地形、門、平台、其他電池、玩家、無人機) 重疊 → 那顆立刻爆炸。
/// 不顯示次數；圖示彩色 = 目前區段還沒用過、黑白 = 用過了。使用紀錄跟著節點快照存檔 (死亡重載會恢復)。
/// 電池殘影由 BatteryGhosts 顯示。
/// </summary>
[CreateAssetMenu(menuName = "Items/Recall")]
public class RecallItem : ItemDefinition
{
    public GameObject batteryPrefab;
    [Tooltip("重疊判定的大小 (比電池小一點，剛好貼著牆不算)")]
    public Vector2 overlapSize = new Vector2(0.8f, 0.8f);
    [Tooltip("每顆電池生成時的特效 (自己播完自己消失)")]
    public GameObject spawnFx;

    static readonly Collider2D[] overlap = new Collider2D[1];

    public override bool ShowUses => false;

    public override bool IsAvailable(ItemInstance item)
    {
        var gm = GameManager.Instance;
        if (gm == null) return false;
        var area = MapArea.Find(gm.player.Body.position);
        return area != null && !gm.IsRecallUsed(area);
    }

    public override void OnUseStart(ItemUseContext ctx)
    {
        var gm = GameManager.Instance;
        var area = MapArea.Find(ctx.player.Body.position);
        if (gm == null || area == null || batteryPrefab == null) return;
        if (gm.IsRecallUsed(area))
        {
            ItemStatusUI.Hint("這個區域已經用過了");
            return;
        }

        var spots = new System.Collections.Generic.List<Vector2>();
        foreach (var p in gm.BatteryRecords) if (area.Contains(p)) spots.Add(p);
        if (spots.Count == 0)
        {
            ItemStatusUI.Hint("這裡沒有記憶");
            return;
        }

        gm.MarkRecallUsed(area);
        var filter = new ContactFilter2D { useTriggers = false };
        foreach (var p in spots)
        {
            bool blocked = Physics2D.OverlapBox(p, overlapSize, 0f, filter, overlap) > 0;
            var battery = Instantiate(batteryPrefab, p, Quaternion.identity).GetComponent<Battery>();
            if (spawnFx != null) Instantiate(spawnFx, p, Quaternion.identity);
            if (blocked) battery.Explode();
        }
    }
}
