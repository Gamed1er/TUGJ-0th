using UnityEngine;

/// <summary>
/// 節點 (自動存檔點，遊戲中看不見)。玩家走進範圍 → GameManager.EnterNode：
/// - 往前推進 (順序比目前節點後面) 或還沒有快照 → 寫入這個節點的快照、顯示「已存檔」
/// - 往回走 → 只把重生點改成這裡，不改快照
/// 順序 = 場景中的 x 座標 (由左到右)。範圍要蓋住整條通道高度，玩家才不會跳過。
/// IsActive = false 時不存檔、不能回溯 (預留給「消耗存檔」trick，呼叫 GameManager.DisableSavePoint)。
/// 複製後請按 ⋮ → "Regenerate ID"。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SavePoint : MonoBehaviour
{
    [SerializeField] string id;
    [SerializeField] string displayName = "節點";
    [Tooltip("玩家重生位置 (相對於節點)")]
    [SerializeField] Vector2 spawnOffset = new Vector2(0f, 1f);

    public string Id => id;
    public string DisplayName => displayName;
    public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;
    public bool IsActive { get; private set; } = true;
    public bool Discovered { get; private set; }

    public void SetState(bool discovered, bool active)
    {
        Discovered = discovered;
        IsActive = active;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null) GameManager.Instance.EnterNode(this);
    }

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) RegenerateId();
    }

    [ContextMenu("Regenerate ID")]
    void RegenerateId() => id = "save_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

    void OnDrawGizmos()
    {
        var box = GetComponent<BoxCollider2D>();
        Gizmos.color = new Color(0f, 1f, 1f, 0.6f);
        if (box != null) Gizmos.DrawWireCube(transform.TransformPoint(box.offset), Vector3.Scale(box.size, transform.lossyScale));
        Gizmos.DrawWireSphere(SpawnPosition, 0.2f);
    }
}
