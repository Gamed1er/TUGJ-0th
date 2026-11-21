using UnityEngine;

/// <summary>
/// 存檔點。走進範圍 = 「去過」(地圖亮起)；站在上面按 E = 存檔。
/// IsActive = false 時不能存檔 (預留給「消耗存檔」trick，呼叫 GameManager.DisableSavePoint)。
/// 複製後請按 ⋮ → "Regenerate ID"。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SavePoint : MonoBehaviour, IInteractable
{
    [SerializeField] string id;
    [SerializeField] string displayName = "存檔點";
    [Tooltip("玩家重生位置 (相對於存檔點)")]
    [SerializeField] Vector2 spawnOffset = new Vector2(0f, 1f);

    [Header("外觀 (美術可替換)")]
    [SerializeField] SpriteRenderer visual;
    [SerializeField] Color undiscoveredColor = new Color(0.5f, 0.5f, 0.5f);
    [SerializeField] Color discoveredColor = new Color(0.3f, 0.8f, 1f);
    [SerializeField] Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);

    public string Id => id;
    public string DisplayName => displayName;
    public Vector2 SpawnPosition => (Vector2)transform.position + spawnOffset;
    public bool IsActive { get; private set; } = true;
    public bool Discovered { get; private set; }

    public Vector3 PromptPosition => transform.position + Vector3.up * 2f;

    public string GetPrompt(PlayerInteractor who) => IsActive ? "E 存檔" : null;

    public void Interact(PlayerInteractor who) => GameManager.Instance.SaveAt(this);

    public void SetState(bool discovered, bool active)
    {
        Discovered = discovered;
        IsActive = active;
        if (visual != null)
            visual.color = !active ? disabledColor : discovered ? discoveredColor : undiscoveredColor;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.GetComponent<PlayerController>() != null) GameManager.Instance.Discover(this);
    }

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) RegenerateId();
    }

    [ContextMenu("Regenerate ID")]
    void RegenerateId() => id = "save_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(SpawnPosition, 0.2f);
    }
}
