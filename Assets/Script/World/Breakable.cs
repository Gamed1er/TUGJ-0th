using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 可破壞物件 (1x1)。被飛行中的黑洞撞到時，以被撞的方塊為中心、BlackHole.breakRadius 內的全部會被破壞。
/// 被破壞時播 breakFx。放在 Ground 圖層。不存檔 (死亡重載會恢復)。要做大面牆就排很多個。
/// </summary>
public class Breakable : MonoBehaviour
{
    public static readonly List<Breakable> All = new List<Breakable>();

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);

    [Tooltip("被破壞時的碎片特效 (自己播完自己消失)")]
    [SerializeField] GameObject breakFx;

    public void Break()
    {
        if (breakFx != null) Instantiate(breakFx, transform.position, Quaternion.identity);
        Destroy(gameObject);
    }

    /// <summary>破壞中心距離 center 在 radius 以內的所有 Breakable (隔牆也會)。</summary>
    public static void BreakAround(Vector2 center, float radius)
    {
        float r2 = radius * radius + 0.001f;
        foreach (var b in All.ToArray())
            if (((Vector2)b.transform.position - center).sqrMagnitude <= r2) b.Break();
    }
}
