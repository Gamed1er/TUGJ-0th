using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 瞄準提示線 (掛在玩家身上)。每幀問選中的道具要不要畫 (ItemDefinition.GetAimPath)，
/// 要就用 LineRenderer 畫出路徑，並在終點顯示 endMarker。
/// 目前有提示線的道具：黑洞槍 (直線到停下的位置)、傳送槍 (模擬軌跡，受黑洞影響)。
/// 操控無人機時不顯示。
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(Inventory))]
public class AimLine : MonoBehaviour
{
    [SerializeField] LineRenderer line;
    [Tooltip("終點標記 (小圓圈)")]
    [SerializeField] Transform endMarker;

    PlayerController player;
    Inventory inventory;
    readonly List<Vector3> points = new List<Vector3>();

    void Awake()
    {
        player = GetComponent<PlayerController>();
        inventory = GetComponent<Inventory>();
    }

    void LateUpdate()
    {
        points.Clear();
        var item = inventory.SelectedItem;
        bool show = item != null && Drone.Controlled == null &&!GameInput.Locked &&
                    item.definition.GetAimPath(new ItemUseContext
                    {
                        player = player,
                        inventory = inventory,
                        item = item,
                        aimWorld = GameInput.MouseWorld,
                    }, points) && points.Count >= 2;

        if (line != null)
        {
            line.enabled = show;
            if (show)
            {
                line.positionCount = points.Count;
                line.SetPositions(points.ToArray());
            }
        }
        if (endMarker != null)
        {
            endMarker.gameObject.SetActive(show);
            if (show) endMarker.position = points[points.Count - 1];
        }
    }
}
