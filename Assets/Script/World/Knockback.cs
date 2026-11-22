using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 爆炸擊退 (Battery.Explode 呼叫)：
/// - 疊在一起的東西 (A 站在 B 的頂面上，可以串成一疊) 用同一個擊退速度，依整疊的中心計算 → 一起飛走，不會散開。
/// - 擊退速度 = maxKnockback × (1 − 距離/半徑)，方向從爆炸中心往外。
/// - 玩家用 PlayerController.AddKnockback；電池、沒電的無人機用 Add + Decay：
///   速度一次加上去，之後在 Time (= 玩家的 knockbackTime) 秒內把加上去的部分線性扣掉，跟玩家的軌跡一致。
/// </summary>
public static class Knockback
{
    /// <summary>電池 / 無人機的擊退衰減時間 = 玩家的 knockbackTime (兩者一致，疊著才會一起飛)。</summary>
    public static float Time
    {
        get
        {
            var gm = GameManager.Instance;
            return gm != null ? Mathf.Max(gm.player.knockbackTime, 0.01f) : 0.3f;
        }
    }

    /// <summary>對 bodies (玩家、電池、沒電的無人機) 施加擊退，疊在一起的分成同一組。</summary>
    public static void Apply(Vector2 center, float radius, float maxSpeed, List<Rigidbody2D> bodies)
    {
        int n = bodies.Count;
        var bounds = new Bounds[n];
        var group = new int[n];
        for (int i = 0; i < n; i++) { bounds[i] = BodyBounds(bodies[i]); group[i] = i; }

        // 上下疊在一起的併成同一組
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
                if (i != j && StandsOn(bounds[i], bounds[j])) Union(group, i, j);

        var sums = new Dictionary<int, Vector2>();
        var counts = new Dictionary<int, int>();
        for (int i = 0; i < n; i++)
        {
            int g = Find(group, i);
            sums.TryGetValue(g, out var s);
            counts.TryGetValue(g, out var c);
            sums[g] = s + (Vector2)bounds[i].center;
            counts[g] = c + 1;
        }

        for (int i = 0; i < n; i++)
        {
            int g = Find(group, i);
            Vector2 groupCenter = sums[g] / counts[g];
            Vector2 v = Velocity(center, groupCenter, radius, maxSpeed);
            var rb = bodies[i];
            var player = rb.GetComponent<PlayerController>();
            if (player != null) { player.AddKnockback(v); continue; }
            var battery = rb.GetComponent<Battery>();
            if (battery != null) { battery.AddKnockback(v); continue; }
            var drone = rb.GetComponent<Drone>();
            if (drone != null) drone.AddKnockback(v);
        }
    }

    /// <summary>從爆炸中心往外的擊退速度，越近越強。</summary>
    public static Vector2 Velocity(Vector2 center, Vector2 target, float radius, float maxSpeed)
    {
        Vector2 d = target - center;
        float dist = d.magnitude;
        Vector2 dir = dist > 0.01f ? d / dist : Vector2.up;
        return dir * (maxSpeed * Mathf.Clamp01(1f - dist / radius));
    }

    /// <summary>
    /// 每個物理步呼叫一次：把還沒扣掉的擊退 (left) 往 0 衰減，回傳扣掉之後的速度。
    /// 水平方向扣到 0 為止 (撞牆停下就不會反彈)；垂直方向照扣 (跟玩家一樣，重力另外算)。
    /// </summary>
    public static Vector2 Decay(ref Vector2 left, float rate, Vector2 velocity)
    {
        if (left == Vector2.zero) return velocity;
        Vector2 next = Vector2.MoveTowards(left, Vector2.zero, rate * UnityEngine.Time.fixedDeltaTime);
        Vector2 d = left - next;
        left = next;

        velocity.y -= d.y;
        if (d.x > 0f) velocity.x = Mathf.Max(velocity.x - d.x, Mathf.Min(velocity.x, 0f));
        else if (d.x < 0f) velocity.x = Mathf.Min(velocity.x - d.x, Mathf.Max(velocity.x, 0f));
        return velocity;
    }

    /// <summary>a 是不是站在 b 的頂面上 (底部貼著 b 的頂部、水平有重疊)。</summary>
    static bool StandsOn(Bounds a, Bounds b)
    {
        bool touching = Mathf.Abs(a.min.y - b.max.y) < 0.12f;
        float overlap = Mathf.Min(a.max.x, b.max.x) - Mathf.Max(a.min.x, b.min.x);
        return touching && overlap > 0.05f;
    }

    static Bounds BodyBounds(Rigidbody2D rb)
    {
        var cols = new List<Collider2D>();
        rb.GetAttachedColliders(cols);
        Bounds b = new Bounds(rb.position, Vector3.zero);
        bool first = true;
        foreach (var c in cols)
        {
            if (c.isTrigger) continue;
            if (first) { b = c.bounds; first = false; }
            else b.Encapsulate(c.bounds);
        }
        return b;
    }

    static int Find(int[] g, int i)
    {
        while (g[i] != i) i = g[i] = g[g[i]];
        return i;
    }

    static void Union(int[] g, int a, int b) => g[Find(g, a)] = Find(g, b);
}
