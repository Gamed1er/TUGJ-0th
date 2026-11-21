using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 壓力板：上面有玩家、電池或沒電的無人機時算「壓著」，會啟動 targets 裡的所有機關 (門、軌道平台)；離開就放開。
/// 需要一個 isTrigger 的 Collider2D 當偵測範圍 (放在板子表面上方一點)。
/// 連到壓力板的機關不要再接拉桿，門也不要勾 startOpen。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [FormerlySerializedAs("doors")]
    [SerializeField] Activatable[] targets;
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Color idleColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] Color pressedColor = new Color(0.3f, 1f, 0.4f);

    readonly HashSet<Collider2D> inside = new HashSet<Collider2D>();
    bool pressed;

    void Start() => Refresh(force: true);

    // 無人機會在範圍內「沒電」，所以每幀重新判斷
    void Update() => Refresh(force: false);

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.isTrigger) inside.Add(other);
    }

    void OnTriggerExit2D(Collider2D other) => inside.Remove(other);

    static bool CanPress(Collider2D c)
    {
        if (c.GetComponent<PlayerController>() != null) return true;
        if (c.GetComponentInParent<Battery>() != null) return true;
        var drone = c.GetComponentInParent<Drone>();
        return drone != null && !drone.IsPowered;
    }

    void Refresh(bool force)
    {
        inside.RemoveWhere(c => c == null); // 被刪掉的物件不會觸發 Exit
        bool now = false;
        foreach (var c in inside)
            if (CanPress(c)) { now = true; break; }
        if (now == pressed && !force) return;
        pressed = now;
        if (visual != null) visual.color = now ? pressedColor : idleColor;
        foreach (var t in targets) if (t != null) t.SetPressed(this, pressed);
    }

    void OnDrawGizmos()
    {
        if (targets == null) return;
        Gizmos.color = Color.green;
        foreach (var t in targets) if (t != null) Gizmos.DrawLine(transform.position, t.transform.position);
    }
}
