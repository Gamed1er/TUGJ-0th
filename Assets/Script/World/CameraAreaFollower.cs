using Unity.Cinemachine;
using UnityEngine;

/// <summary>
/// 掛在 CinemachineCamera 上。追蹤目標換到別的 MapArea 時 (轉場、重生、除錯瞬移)，
/// 自動把鏡頭的活動範圍換成新區域，並直接定位 (不會從舊位置滑過去)。
/// </summary>
[RequireComponent(typeof(CinemachineCamera), typeof(CinemachineConfiner2D))]
public class CameraAreaFollower : MonoBehaviour
{
    CinemachineCamera vcam;
    CinemachineConfiner2D confiner;
    MapArea current;

    public MapArea Current => current;

    void Awake()
    {
        vcam = GetComponent<CinemachineCamera>();
        confiner = GetComponent<CinemachineConfiner2D>();
    }

    void LateUpdate()
    {
        var target = vcam.Follow;
        if (target == null) return;
        if (current != null && current.Contains(target.position)) return;

        var area = MapArea.Find(target.position);
        if (area == null || area == current) return;

        current = area;
        confiner.BoundingShape2D = area.Bounds;
        confiner.InvalidateBoundingShapeCache();
        vcam.PreviousStateIsValid = false; // 直接定位，不做平滑移動
    }
}
