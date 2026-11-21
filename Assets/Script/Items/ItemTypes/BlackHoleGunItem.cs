using UnityEngine;

/// <summary>
/// 黑洞槍。右鍵：
/// - 場上沒有黑洞 → 往滑鼠方向射出 (扣 1 次)
/// - 黑洞飛行中 → 固定 (這個由 PlayerItemUser 統一處理，選哪一格都有效)
/// - 黑洞已固定 → 收回 (不退次數)
/// 黑洞本身的參數 (速度、引力...) 在 BlackHole prefab 上調整。
/// </summary>
[CreateAssetMenu(menuName = "Items/Black Hole Gun")]
public class BlackHoleGunItem : ItemDefinition
{
    public BlackHole blackHolePrefab;

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (BlackHole.Current != null)
        {
            BlackHole.Current.Retrieve();
            return;
        }
        if (blackHolePrefab == null || !ctx.CheckUses()) return;

        Vector2 origin = ctx.player.Body.position;
        Vector2 dir = ctx.aimWorld - origin;
        if (dir.sqrMagnitude < 0.0001f) dir = Vector2.right * ctx.player.Facing;

        Instantiate(blackHolePrefab, origin, Quaternion.identity).Launch(dir.normalized);
        ctx.Consume();
    }
}
