using UnityEngine;

/// <summary>
/// 「往滑鼠方向生成東西」的共用規則 (電池倉、無人機)：
/// 從玩家往滑鼠方向最遠 maxDistance 格，對齊格子中心；那一格被擋住或跟玩家之間隔著牆 → 往玩家的方向退，找第一個可以放的格子。
/// </summary>
public static class SpawnCell
{
    static readonly Collider2D[] overlap = new Collider2D[1];

    /// <summary>找到回傳 true；找不到回傳 false，此時 cell = 滑鼠方向最遠的那一格 (預覽畫紅色用)。</summary>
    public static bool Find(ItemUseContext ctx, float maxDistance, Vector2 size, LayerMask wallMask, out Vector2 cell)
    {
        Vector2 origin = ctx.player.Body.position;
        Vector2 dir = ctx.AimDirection;
        float far = Mathf.Min((ctx.aimWorld - origin).magnitude, maxDistance);

        cell = Snap(origin + dir * far);
        Vector2 last = new Vector2(float.NaN, float.NaN);
        // 從最遠往玩家退，每次退 0.25 格，檢查經過的每一格
        for (float d = far; d >= 0f; d -= 0.25f)
        {
            Vector2 c = Snap(origin + dir * d);
            if (c == last) continue;
            last = c;
            if (CanPlace(origin, c, size, wallMask)) { cell = c; return true; }
        }
        return false;
    }

    static Vector2 Snap(Vector2 p) => new Vector2(Mathf.Floor(p.x) + 0.5f, Mathf.Floor(p.y) + 0.5f);

    static bool CanPlace(Vector2 origin, Vector2 cell, Vector2 size, LayerMask wallMask)
    {
        // 有實體擋著 (地形、門、電池、玩家...) 不能放；觸發區不算
        var filter = new ContactFilter2D { useTriggers = false };
        if (Physics2D.OverlapBox(cell, size, 0f, filter, overlap) > 0) return false;
        // 跟玩家之間不能隔著牆
        return !Physics2D.Linecast(origin, cell, wallMask);
    }
}

/// <summary>有生成位置預覽的道具 (SpawnPreview 用)。</summary>
public interface ISpawnPreviewItem
{
    /// <summary>現在要不要顯示預覽 (例如還有次數、場上沒有有電的無人機)。</summary>
    bool ShowPreview(ItemUseContext ctx);
    bool FindSpawnCell(ItemUseContext ctx, out Vector2 cell);
    Sprite PreviewSprite { get; }
}
