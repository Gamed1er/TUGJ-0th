using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 二周目的電池殘影：在一周目通關時記錄的電池位置顯示半透明、沒有碰撞的電池。
/// 該房間用過 transform (終端機) 後殘影消失，在那之前一直顯示。
/// 掛在 Game 場景任意物件上 (目前在 GameManager 物件)。一周目不顯示。
/// </summary>
public class BatteryGhosts : MonoBehaviour
{
    [Tooltip("殘影的圖 (電池的 sprite)")]
    [SerializeField] Sprite sprite;
    [SerializeField] Color color = new Color(1f, 1f, 1f, 0.6f);
    [SerializeField] int sortingOrder = 5;
    [Tooltip("殘影上持續的微弱光點 (循環播放的粒子，殘影消失時一起關掉)")]
    [SerializeField] GameObject ghostFx;

    struct Ghost
    {
        public SpriteRenderer renderer;
        public MapArea area;
        public GameObject fx;
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
            var fx = ghostFx != null ? Instantiate(ghostFx, go.transform) : null;
            ghosts.Add(new Ghost { renderer = sr, area = MapArea.Find(p), fx = fx });
        }
    }

    void Update()
    {
        if (ghosts.Count == 0) return;
        var gm = GameManager.Instance;
        foreach (var g in ghosts)
        {
            bool show = !gm.IsRecallUsed(g.area);
            g.renderer.enabled = show;
            if (g.fx != null && g.fx.activeSelf != show) g.fx.SetActive(show);
            if (show) g.renderer.color = color;
        }
    }
}
