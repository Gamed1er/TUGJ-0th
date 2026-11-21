using UnityEngine;

/// <summary>
/// 生成位置預覽 (掛在玩家身上)：選中的道具實作 ISpawnPreviewItem (電池倉、無人機) 時，
/// 在 FindSpawnCell 的位置顯示半透明的圖；找不到位置時變紅色。操控無人機時不顯示。
/// </summary>
[RequireComponent(typeof(PlayerController), typeof(Inventory))]
public class SpawnPreview : MonoBehaviour
{
    [Tooltip("預覽用的 SpriteRenderer (放在玩家底下的子物件)")]
    [SerializeField] SpriteRenderer preview;
    [SerializeField] Color validColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] Color invalidColor = new Color(1f, 0.2f, 0.2f, 0.45f);

    PlayerController player;
    Inventory inventory;

    void Awake()
    {
        player = GetComponent<PlayerController>();
        inventory = GetComponent<Inventory>();
    }

    void LateUpdate()
    {
        if (preview == null) return;
        var item = inventory.SelectedItem;
        var def = item != null ? item.definition as ISpawnPreviewItem : null;
        var ctx = new ItemUseContext { player = player, inventory = inventory, item = item, aimWorld = GameInput.MouseWorld };
        bool show = def != null && Drone.Controlled == null && !GameInput.Locked && def.ShowPreview(ctx);
        preview.enabled = show;
        if (!show) return;

        bool ok = def.FindSpawnCell(ctx, out Vector2 cell);
        preview.sprite = def.PreviewSprite;
        preview.transform.position = cell;
        preview.color = ok ? validColor : invalidColor;
    }
}
