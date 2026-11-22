using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 一張地圖 (區域) 的範圍。bounds 是一個 isTrigger 的 Collider2D (通常是 PolygonCollider2D)，
/// 同時當作鏡頭的活動範圍 (CameraAreaFollower 會自動切換) 與地圖 UI 的世界範圍。
/// 也是「房間」：進入房間就自動存檔，順序 = x 座標 (room1, room2...，見 GameManager.Rooms)；二周目的 transform 每個房間各能用一次。
/// </summary>
public class MapArea : MonoBehaviour
{
    public static readonly List<MapArea> All = new List<MapArea>();

    [SerializeField] string displayName = "區域";
    [SerializeField] Collider2D bounds;

    public string DisplayName => displayName;
    /// <summary>存檔用的識別 = 物件名稱 (各區域的物件名稱不要重複)。</summary>
    public string Id => name;
    public Collider2D Bounds => bounds;

    public bool Contains(Vector2 point) => bounds != null && bounds.OverlapPoint(point);

    /// <summary>找出包含這個點的區域，找不到回傳 null。</summary>
    public static MapArea Find(Vector2 point) => All.Find(a => a.Contains(point));

    void OnEnable() => All.Add(this);
    void OnDisable() => All.Remove(this);
}
