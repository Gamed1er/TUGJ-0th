using UnityEngine;

/// <summary>
/// 地圖邊界的轉場觸發區。玩家碰到 → 黑畫面 → 移動到 target 的入口，並保持相對高度。
/// 只能往右走：左邊房間的 Edge 設 target = 右邊房間的 Edge；右邊房間往回的 Edge 不設 target
/// → 執行時變成一道看不見的實心牆 (Ground 圖層)，要回去只能用終端機讀檔。
/// 相對高度 = 玩家 y - 這個物件的 y，套用到 target 的入口上；兩邊的物件請放在「對應的高度基準」(例如兩邊的地面高度)。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class MapEdge : MonoBehaviour
{
    [Tooltip("轉場目的地。不設 = 單向的另一端，變成實心牆")]
    [SerializeField] MapEdge target;
    [Tooltip("從別張地圖進入這裡時，玩家出現的位置 (相對於此物件)。要放在觸發區外面，避免一進來又被傳走")]
    [SerializeField] Vector2 entryOffset = new Vector2(1.5f, 0f);

    public Vector2 EntryPoint => (Vector2)transform.position + entryOffset;

    void Awake()
    {
        if (target != null) return;
        // 沒有目的地 = 牆
        GetComponent<Collider2D>().isTrigger = false;
        gameObject.layer = LayerMask.NameToLayer("Ground");
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (target == null) return;
        var player = other.GetComponent<PlayerController>();
        if (player == null) return;

        float relativeY = player.transform.position.y - transform.position.y;
        GameManager.Instance.TransitionTo(target.EntryPoint + Vector2.up * relativeY);
    }

    void OnDrawGizmos()
    {
        Gizmos.color = target != null ? Color.magenta : Color.gray;
        Gizmos.DrawWireSphere(EntryPoint, 0.3f);
        if (target != null) Gizmos.DrawLine(transform.position, target.transform.position);
    }
}
