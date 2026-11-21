using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瞄準提示線 (掛在玩家身上)。每幀問選中的道具要不要畫 (ItemDefinition.GetAimPath)，
/// 要就用 LineRenderer 畫出路徑，並在終點顯示 endMarker。道具回傳 highlight 時畫成 highlightColor。
/// 目前有提示線的道具：黑洞槍 (直線到停下的位置)、傳送槍 (模擬軌跡，受黑洞影響；會打到電池時變綠色)。
/// 操控無人機時不顯示。
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(Inventory))]
public class AimLine : MonoBehaviour
{
    [SerializeField] LineRenderer line;
    [Tooltip("終點標記 (小圓圈)")]
    [SerializeField] Transform endMarker;
    [SerializeField] Color normalColor = new Color(1f, 0.15f, 0.15f, 0.8f);
    [Tooltip("道具說「這一發有效」時的顏色 (傳送槍會打到電池)")]
    [SerializeField] Color highlightColor = new Color(0.2f, 1f, 0.3f, 0.9f);

    PlayerController player;
    Inventory inventory;
    SpriteRenderer marker;
    readonly List<Vector3> points = new List<Vector3>();

    void Awake()
    {
        player = GetComponent<PlayerController>();
        inventory = GetComponent<Inventory>();
        if (endMarker != null) marker = endMarker.GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        points.Clear();
        var item = inventory.SelectedItem;
        bool highlight = false;
        bool show = item != null && Drone.Controlled == null && !GameInput.Locked &&
                    item.definition.GetAimPath(new ItemUseContext
                    {
                        player = player,
                        inventory = inventory,
                        item = item,
                        aimWorld = GameInput.MouseWorld,
                    }, points, out highlight) && points.Count >= 2;
        Color color = highlight ? highlightColor : normalColor;

        if (line != null)
        {
            line.enabled = show;
            if (show)
            {
                line.startColor = line.endColor = color;
                line.positionCount = points.Count;
                line.SetPositions(points.ToArray());
            }
        }
        if (endMarker != null)
        {
            endMarker.gameObject.SetActive(show);
            if (show)
            {
                endMarker.position = points[points.Count - 1];
                if (marker != null) marker.color = color;
            }
        }
    }
}
