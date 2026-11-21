using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 壓力板：上面有玩家或電池時算「壓著」，會打開 doors 裡的所有門；離開就放開。
/// 需要一個 isTrigger 的 Collider2D 當偵測範圍 (放在板子表面上方一點)。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PressurePlate : MonoBehaviour
{
    [SerializeField] Door[] doors;
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Color idleColor = new Color(0.6f, 0.6f, 0.6f);
    [SerializeField] Color pressedColor = new Color(0.3f, 1f, 0.4f);

    readonly HashSet<Collider2D> inside = new HashSet<Collider2D>();
    bool pressed;

    void Start() => Refresh();

    void OnTriggerEnter2D(Collider2D other)
    {
        if (CanPress(other)) { inside.Add(other); Refresh(); }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (inside.Remove(other)) Refresh();
    }

    static bool CanPress(Collider2D c) =>
        !c.isTrigger && (c.GetComponent<PlayerController>() != null || c.GetComponentInParent<Battery>() != null);

    void Refresh()
    {
        inside.RemoveWhere(c => c == null); // 被刪掉的物件不會觸發 Exit
        bool now = inside.Count > 0;
        if (visual != null) visual.color = now ? pressedColor : idleColor;
        if (now == pressed) return;
        pressed = now;
        foreach (var d in doors) if (d != null) d.SetPressed(this, pressed);
    }

    void OnDrawGizmos()
    {
        if (doors == null) return;
        Gizmos.color = Color.green;
        foreach (var d in doors) if (d != null) Gizmos.DrawLine(transform.position, d.transform.position);
    }
}
