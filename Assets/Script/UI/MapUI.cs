using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 地圖 (按 M 開關)。依節點的世界座標自動投影到 nodeArea 上，並顯示玩家位置。
/// 回溯：點一個去過的節點 = 選取，再點一次 = 確認 → GameManager.RewindTo。
/// 之後要換美術畫的地圖底圖：把底圖放在 nodeArea 裡，範圍對齊 GameManager.boundsTilemap 即可。
/// </summary>
public class MapUI : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] RectTransform nodeArea;
    [SerializeField] MapNodeUI nodePrefab;
    [SerializeField] RectTransform playerMarker;
    [Tooltip("操作提示 (回溯確認訊息也顯示在這)")]
    [SerializeField] Text hintLabel;
    [Tooltip("開地圖時暫停遊戲")]
    [SerializeField] bool pauseWhileOpen = true;

    const string DefaultHint = "點擊節點回溯到當時的狀態";

    readonly List<MapNodeUI> nodes = new List<MapNodeUI>();
    Bounds world;
    MapNodeUI selected;

    public bool IsOpen => panel.activeSelf;

    void Start()
    {
        panel.SetActive(false);
        var gm = GameManager.Instance;
        world = gm.WorldBounds;
        foreach (var sp in gm.SavePoints)
        {
            var node = Instantiate(nodePrefab, nodeArea);
            node.Bind(sp, OnNodeClicked);
            node.RectTransform.anchoredPosition = WorldToMap(sp.transform.position);
            nodes.Add(node);
        }
        gm.StateChanged += Refresh;
    }

    void OnDestroy()
    {
        if (GameManager.Instance != null) GameManager.Instance.StateChanged -= Refresh;
        if (IsOpen && pauseWhileOpen) Time.timeScale = 1f;
    }

    void Update()
    {
        if (GameInput.MapPressed) SetOpen(!IsOpen);
        if (IsOpen && playerMarker != null)
            playerMarker.anchoredPosition = WorldToMap(GameManager.Instance.player.transform.position);
    }

    public void SetOpen(bool open)
    {
        panel.SetActive(open);
        if (pauseWhileOpen) Time.timeScale = open ? 0f : 1f;
        GameInput.Locked = open; // 開地圖時不能操作角色
        selected = null;
        SetHint(DefaultHint);
        if (open) Refresh();
    }

    void OnNodeClicked(MapNodeUI node)
    {
        var gm = GameManager.Instance;
        if (!gm.CanRewindTo(node.SavePoint))
        {
            selected = null;
            SetHint(node.SavePoint.Discovered ? "這個節點無法回溯" : "還沒去過這個節點");
        }
        else if (selected == node)
        {
            SetHint($"回到「{node.SavePoint.DisplayName}」…");
            gm.RewindTo(node.SavePoint); // 會重載場景
        }
        else
        {
            selected = node;
            SetHint($"再點一次回到「{node.SavePoint.DisplayName}」(物品欄會回到當時的狀態)");
        }
        Refresh();
    }

    void SetHint(string text)
    {
        if (hintLabel != null) hintLabel.text = text;
    }

    void Refresh()
    {
        string current = GameManager.Instance.CurrentSavePointId;
        foreach (var n in nodes) n.Refresh(current, n == selected);
    }

    /// <summary>世界座標 → nodeArea 內的座標 (保持長寬比、置中)。nodeArea 的 pivot 需為 (0.5, 0.5)。</summary>
    Vector2 WorldToMap(Vector3 worldPos)
    {
        Vector2 area = nodeArea.rect.size;
        float scale = Mathf.Min(area.x / Mathf.Max(world.size.x, 0.01f), area.y / Mathf.Max(world.size.y, 0.01f));
        return (Vector2)(worldPos - world.center) * scale;
    }
}
