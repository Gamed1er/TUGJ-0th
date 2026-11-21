using UnityEngine;

/// <summary>
/// 場景上的道具補給：站在範圍內按 E → 對應道具 +amount 次 (物品欄沒有這個道具就放進空格)。
/// 撿走後 ID 會記進存檔，讀檔不會重生。
/// 複製 (Ctrl+D) 這個物件後請按右上角 ⋮ → "Regenerate ID"，避免 ID 重複。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class ItemPickup : MonoBehaviour, IInteractable
{
    [SerializeField] string id;
    [SerializeField] ItemDefinition item;
    [Min(1)] [SerializeField] int amount = 1;
    [SerializeField] SpriteRenderer iconRenderer;

    public string Id => id;

    public Vector3 PromptPosition => transform.position + Vector3.up * 0.8f;

    public string GetPrompt(PlayerInteractor who)
    {
        if (item == null) return null;
        if (who.Inventory.Find(item) == null && who.Inventory.IsFull) return "物品欄已滿";
        return $"E 撿起 {item.displayName} +{amount}";
    }

    public void Interact(PlayerInteractor who)
    {
        if (item == null || !who.Inventory.AddUses(item, amount)) return;
        GameManager.Instance.MarkPickedUp(id);
        Destroy(gameObject);
    }

    void Start()
    {
        // 圖示自動用道具資產的 icon
        if (iconRenderer != null && item != null && item.icon != null) iconRenderer.sprite = item.icon;
    }

    void OnValidate()
    {
        if (string.IsNullOrEmpty(id)) RegenerateId();
    }

    [ContextMenu("Regenerate ID")]
    void RegenerateId() => id = "pickup_" + System.Guid.NewGuid().ToString("N").Substring(0, 8);
}
