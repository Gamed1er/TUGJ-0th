using UnityEngine;

/// <summary>
/// 碰到就死。可掛在尖刺 Tilemap (TilemapCollider2D 勾 isTrigger) 或地圖底部的掉落區 (BoxCollider2D isTrigger)。
/// 非 trigger 的碰撞體也有效。
/// </summary>
public class Hazard : MonoBehaviour
{
    void OnTriggerEnter2D(Collider2D other) => TryKill(other);
    void OnCollisionEnter2D(Collision2D collision) => TryKill(collision.collider);

    static void TryKill(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null) GameManager.Instance.KillPlayer();
    }
}
