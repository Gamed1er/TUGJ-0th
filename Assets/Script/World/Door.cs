using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 門 / 橋。「關」= 有碰撞 (擋路 / 橋在)，「開」= 沒碰撞、半透明 (通行 / 橋消失)。
/// 控制來源二選一 (不要混用)：
/// - PressurePlate：任一塊板被壓著就開，全部放開就關
/// - Lever：每拉一次，開關狀態翻轉 (startOpen 決定初始狀態，可做「A 開 B 關」的交替門)
/// 消失 (開) 是立即的，站在橋上的東西會掉下去；出現 (關) 時如果位置有東西 (玩家、電池)，會等淨空才出現，不會夾住。
/// 橫放當橋：把 Scale X 拉長 (或轉 90 度) 即可。放在 Ground 圖層 (玩家可以站、黑洞會貼上去)。
/// </summary>
public class Door : MonoBehaviour
{
    [SerializeField] BoxCollider2D solid;
    [SerializeField] SpriteRenderer visual;
    [SerializeField] float openAlpha = 0.2f;
    [Tooltip("初始是開的 (橋 = 一開始不存在)。只給拉桿控制的門用")]
    [SerializeField] bool startOpen;

    readonly HashSet<PressurePlate> pressedBy = new HashSet<PressurePlate>();
    readonly Collider2D[] overlap = new Collider2D[4];
    bool wantOpen;

    public bool IsOpen => !solid.enabled;

    void Awake()
    {
        wantOpen = startOpen;
        SetOpen(startOpen);
    }

    /// <summary>壓力板呼叫。</summary>
    public void SetPressed(PressurePlate plate, bool pressed)
    {
        if (pressed) pressedBy.Add(plate);
        else pressedBy.Remove(plate);
        wantOpen = pressedBy.Count > 0;
    }

    /// <summary>拉桿呼叫：翻轉開關。</summary>
    public void Toggle() => wantOpen = !wantOpen;

    void Update()
    {
        if (wantOpen && !IsOpen) SetOpen(true);
        else if (!wantOpen && IsOpen && !IsBlocked()) SetOpen(false); // 淨空了才出現
    }

    bool IsBlocked()
    {
        var filter = new ContactFilter2D { useTriggers = false };
        Vector2 center = solid.transform.TransformPoint(solid.offset);
        Vector2 size = Vector2.Scale(solid.size, solid.transform.lossyScale) - Vector2.one * 0.1f; // 縮一點，避免碰到旁邊的地形
        int n = Physics2D.OverlapBox(center, size, solid.transform.eulerAngles.z, filter, overlap);
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
