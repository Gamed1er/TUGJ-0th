using UnityEngine;

/// <summary>
/// 門 / 橋。「關」= 有碰撞 (擋路 / 橋在)，「開」= 沒碰撞 (通行 / 橋消失)。
/// 控制 (見 Activatable)：壓力板壓著 = 開；拉桿每拉一次翻轉 (startOpen 決定初始狀態，可做「A 開 B 關」的交替門)。
/// 打開是立即的 (碰撞馬上消失，站在橋上的東西會掉下去)；關閉時如果位置有東西 (玩家、電池)，會等淨空才關，不會夾住。
///
/// 外觀：逐格動畫，frames[0] = 關閉、最後一格 = 打開。打開時往後播、關閉時往前播。
/// 勾 horizontal = 橋 (用 lyingFrames，橫的)。長度固定 4 格 (美術尺寸)，不要用 Scale 拉長。
/// 位置：直的門中心 = (格子中心 x, 地面 + 2)；橋的頂面 = 位置 y + 0.5 (跟擺放格子的頂端對齊)。
/// 放在 Ground 圖層 (玩家可以站、黑洞會貼上去)。
/// </summary>
public class Door : Activatable
{
    [SerializeField] BoxCollider2D solid;
    [SerializeField] SpriteRenderer visual;
    [Tooltip("初始是開的 (橋 = 一開始不存在)。只給拉桿控制的門用")]
    [SerializeField] bool startOpen;
    [Tooltip("勾選 = 橋 (橫的)")]
    [SerializeField] bool horizontal;

    [Header("動畫")]
    [Tooltip("直的門：第 1 格 = 關閉、最後一格 = 打開")]
    [SerializeField] Sprite[] frames;
    [Tooltip("橋 (橫的)：第 1 格 = 關閉、最後一格 = 打開")]
    [SerializeField] Sprite[] lyingFrames;
    [Tooltip("每秒播幾格")]
    [SerializeField] float frameRate = 18f;
    [Tooltip("碰撞體的厚度 (格)，長度固定 4 格")]
    [SerializeField] float thickness = 0.375f;

    readonly Collider2D[] overlap = new Collider2D[4];
    float frame; // 目前播到第幾格 (0 = 關閉)

    public bool IsOpen => !solid.enabled;
    Sprite[] Frames => horizontal ? lyingFrames : frames;
    int LastFrame => Frames != null && Frames.Length > 0 ? Frames.Length - 1 : 0;

    void Awake()
    {
        ApplyShape();
        Active = startOpen;
        solid.enabled = !startOpen;
        frame = startOpen ? LastFrame : 0;
        ShowFrame();
    }

    void Update()
    {
        if (Active && !IsOpen) solid.enabled = false;
        else if (!Active && IsOpen && !IsBlocked()) solid.enabled = true; // 淨空了才關

        // 動畫跟著碰撞狀態：開 → 播到最後一格；關 → 播回第 1 格
        float target = IsOpen ? LastFrame : 0;
        frame = Mathf.MoveTowards(frame, target, frameRate * Time.deltaTime);
        ShowFrame();
    }

    void ShowFrame()
    {
        var f = Frames;
        if (visual == null || f == null || f.Length == 0) return;
        visual.sprite = f[Mathf.Clamp(Mathf.RoundToInt(frame), 0, f.Length - 1)];
    }

    /// <summary>依直 / 橫設定碰撞體大小 (4 格長、thickness 厚)。</summary>
    void ApplyShape()
    {
        if (solid == null) return;
        if (horizontal)
        {
            solid.size = new Vector2(4f, thickness);
            solid.offset = new Vector2(0f, 0.5f - thickness * 0.5f); // 頂面貼齊格子頂端
        }
        else
        {
            solid.size = new Vector2(thickness, 4f);
            solid.offset = Vector2.zero;
        }
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

#if UNITY_EDITOR
    // 編輯器：勾 horizontal / startOpen 時馬上更新外觀與碰撞體
    // (OnValidate 裡不能直接換 sprite，會跳警告，所以延到下一個編輯器更新)
    void OnValidate()
    {
        ApplyShape();
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this == null || Application.isPlaying) return;
            var f = Frames;
            if (visual != null && f != null && f.Length > 0) visual.sprite = startOpen ? f[f.Length - 1] : f[0];
        };
    }
#endif
}
