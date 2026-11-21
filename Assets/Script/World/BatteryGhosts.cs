using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 二周目的電池殘影：在一周目通關時記錄的電池位置顯示半透明、沒有碰撞的電池。
/// 該區段用過「請輸入文本」後殘影消失；選中「請輸入文本」時殘影更明顯。
/// 掛在 Game 場景任意物件上 (目前在 GameManager 物件)。一周目不顯示。
/// </summary>
public class BatteryGhosts : MonoBehaviour
{
    [Tooltip("殘影的圖 (電池的 sprite)")]
    [SerializeField] Sprite sprite;
    [SerializeField] Color color = new Color(1f, 1f, 1f, 0.2f);
    [Tooltip("選中「請輸入文本」時的顏色")]
    [SerializeField] Color highlightColor = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] int sortingOrder = 5;

    struct Ghost
    {
        public SpriteRenderer renderer;
        public MapArea area;
    }

    readonly List<Ghost> ghosts = new List<Ghost>();

    void Start()
    {
        var gm = GameManager.Instance;
        if (gm == null || gm.Loop < 2) return;
        foreach (var p in gm.BatteryRecords)
        {
            var go = new GameObject("BatteryGhost");
            go.transform.SetParent(transform, false);
            go.transform.position = p;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = sortingOrder;
            ghosts.Add(new Ghost { renderer = sr, area = MapArea.Find(p) });
        }
    }

    void Update()
    {
        if (ghosts.Count == 0) return;
        var gm = GameManager.Instance;
        var selected = gm.Inventory.SelectedItem;
        bool highlight = selected != null && selected.definition is RecallItem;
        foreach (var g in ghosts)
        {
            bool show = !gm.IsRecallUsed(g.area);
            g.renderer.enabled = show;
            if (show) g.renderer.color = highlight ? highlightColor : color;
        }
    }
}
