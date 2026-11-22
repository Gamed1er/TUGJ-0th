using UnityEngine;

/// <summary>
/// 生成東西的共用規則 (電池倉、無人機)：
/// 只能放在玩家腳邊緊鄰的那一格 (跟腳同一排)，滑鼠在玩家左邊 = 左邊那格、右邊 = 右邊那格。
/// 那一格被擋住、跟玩家之間隔著牆、或玩家在空中 → 不能放。
/// </summary>
public static class SpawnCell
{
    static readonly Collider2D[] overlap = new Collider2D[1];

    /// <summary>玩家在空中時不能生成 (預覽也不顯示)。</summary>
    public static bool CanSpawnNow(ItemUseContext ctx) => ctx.player.IsGrounded;

    /// <summary>找到回傳 true；找不到回傳 false，此時 cell = 那一格 (預覽畫紅色用)。</summary>
    public static bool Find(ItemUseContext ctx, Vector2 size, LayerMask wallMask, out Vector2 cell)
    {
        Vector2 origin = ctx.player.Body.position;
        Bounds b = ctx.player.GetComponent<Collider2D>().bounds;
        int side = ctx.aimWorld.x >= origin.x ? 1 : -1;

        // 緊貼玩家碰撞體外側的格子；y = 腳底所在的那一排
        float x = side > 0 ? Mathf.Ceil(b.max.x - 0.01f) + 0.5f : Mathf.Floor(b.min.x + 0.01f) - 0.5f;
        float y = Mathf.Floor(b.min.y + 0.05f) + 0.5f;
        cell = new Vector2(x, y);

        if (!CanSpawnNow(ctx)) return false;
        return CanPlace(new Vector2(origin.x, cell.y), cell, size, wallMask); // 隔牆判定沿著腳那一排
    }

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
