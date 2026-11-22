using System.Collections.Generic;
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

        Instantiate(blackHolePrefab, ctx.player.Body.position, Quaternion.identity).Launch(ctx.AimDirection);
        ctx.Consume();
        var anim = ctx.player.GetComponent<PlayerAnimator>();
        if (anim != null) anim.PlayShot(); // 射擊動畫
    }

    /// <summary>提示線：場上沒有黑洞 (下一次右鍵 = 發射) 時，直線畫到黑洞會停下的位置。</summary>
    public override bool GetAimPath(ItemUseContext ctx, List<Vector3> points, out bool highlight)
    {
        highlight = false;
        if (BlackHole.Current != null || blackHolePrefab == null || ctx.item.uses <= 0) return false;
        Vector2 origin = ctx.player.Body.position;
        points.Add(origin);
        points.Add(blackHolePrefab.PredictStop(origin, ctx.AimDirection));
        return true;
    }
}
