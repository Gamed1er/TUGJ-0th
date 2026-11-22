using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 傳送槍。右鍵：
/// - 場上沒有子彈 → 往滑鼠方向射出 (射出就扣 1 次)
/// - 子彈飛行中 → 取消子彈 (不退次數)
/// 子彈撞到電池才會傳送過去，撞到其他東西就消失。子彈本身的參數 (速度、黑洞拉力倍率...) 在 TeleportBullet prefab 上調整。
/// </summary>
[CreateAssetMenu(menuName = "Items/Teleport Gun")]
public class TeleportGunItem : ItemDefinition
{
    public TeleportBullet bulletPrefab;
    [Tooltip("射出音效")]
    public AudioClip shootSfx;

    public override void OnUseStart(ItemUseContext ctx)
    {
        if (TeleportBullet.Current != null)
        {
            TeleportBullet.Current.Cancel();
            return;
        }
        if (bulletPrefab == null || !ctx.CheckUses()) return;

        Instantiate(bulletPrefab, ctx.player.Body.position, Quaternion.identity).Launch(ctx.AimDirection);
        ctx.Consume();
        Sfx.Play(shootSfx);
        var anim = ctx.player.GetComponent<PlayerAnimator>();
        if (anim != null) anim.PlayShot(); // 射擊動畫
    }

    /// <summary>提示線：沒有子彈在飛時，模擬子彈軌跡 (含黑洞拉力)。會打到電池 → 綠色。</summary>
    public override bool GetAimPath(ItemUseContext ctx, List<Vector3> points, out bool highlight)
    {
        highlight = false;
        if (TeleportBullet.Current != null || bulletPrefab == null || ctx.item.uses <= 0) return false;
        highlight = bulletPrefab.Simulate(ctx.player.Body.position, ctx.AimDirection, points);
        return true;
    }
}
