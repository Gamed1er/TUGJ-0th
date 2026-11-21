using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 門：任一個連到它的 PressurePlate 被壓著就打開 (關掉碰撞、外觀變半透明)。
/// 要關門時如果門的位置有東西 (玩家、電池)，會等到淨空才關，不會夾住。
/// 門要放在 Ground 圖層 (玩家可以站在上面、黑洞會貼上去)。美術之後可把 SetVisual 換成動畫。
/// </summary>
public class Door : MonoBehaviour
{
    [SerializeField] BoxCollider2D solid;
    [SerializeField] SpriteRenderer visual;
    [SerializeField] float openAlpha = 0.2f;

    readonly HashSet<PressurePlate> pressedBy = new HashSet<PressurePlate>();
    readonly Collider2D[] overlap = new Collider2D[4];
    bool wantOpen;

    public bool IsOpen => !solid.enabled;

    public void SetPressed(PressurePlate plate, bool pressed)
    {
        if (pressed) pressedBy.Add(plate);
        else pressedBy.Remove(plate);
        wantOpen = pressedBy.Count > 0;
        if (wantOpen) SetOpen(true);
    }

    void Update()
    {
        // 想關門但還開著 → 淨空了才關
        if (!wantOpen && IsOpen && !IsBlocked()) SetOpen(false);
    }

    bool IsBlocked()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        Vector2 center = solid.transform.TransformPoint(solid.offset);
        Vector2 size = Vector2.Scale(solid.size, solid.transform.lossyScale) - Vector2.one * 0.1f; // 縮一點，避免碰到旁邊的地形
        int n = Physics2D.OverlapBox(center, size, 0f, filter, overlap);
        for (int i = 0; i < n; i++)
            if (overlap[i] != solid && overlap[i].GetComponent<UnityEngine.Tilemaps.TilemapCollider2D>() == null
                && overlap[i].GetComponent<CompositeCollider2D>() == null) return true;
        return false;
    }

    void SetOpen(bool open)
    {
        solid.enabled = !open;
        if (visual != null)
        {
            var c = visual.color;
            c.a = open ? openAlpha : 1f;
            visual.color = c;
        }
    }
}
