using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 物品欄的一格。左鍵點擊 = 選取；拖曳到另一格 = 交換位置。
/// 美術換圖：直接改 InventorySlot.prefab 裡的 Image。
/// </summary>
public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] Image icon;
    [SerializeField] Text usesText;
    [SerializeField] Color emptyIconColor = new Color(0.4f, 0.4f, 0.4f, 0.6f);
    [SerializeField] Text keyText;
    [SerializeField] GameObject selectedFrame;
    [Tooltip("格子底色。無人機道具在場上有有電的無人機時變成 droneActiveColor")]
    [SerializeField] Image background;
    [SerializeField] Color droneActiveColor = new Color(0.1f, 0.55f, 0.65f, 0.9f);

    InventoryUI owner;
    int index;
    Transform iconParent;
    int iconSiblingIndex;
    Canvas rootCanvas;
    ItemInstance item;
    Color normalBackground;

    public void Init(InventoryUI owner, int index)
    {
        this.owner = owner;
        this.index = index;
        keyText.text = (index + 1).ToString();
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        if (background != null) normalBackground = background.color;
    }

    void Update()
    {
        if (background == null) return;
        bool droneActive = item != null && item.definition is DroneItem && Drone.Powered != null;
        background.color = droneActive ? droneActiveColor : normalBackground;
    }

    public void Refresh(ItemInstance item, bool selected)
    {
        this.item = item;
        selectedFrame.SetActive(selected);
        bool has = item != null;
        icon.enabled = has;
        usesText.text = has ? item.uses.ToString() : "";
        if (has)
        {
            icon.sprite = item.definition.icon;
            icon.color = item.uses > 0 ? Color.white : emptyIconColor; // 用完變灰，但道具保留
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left) owner.Inventory.Select(index);
    }

    // ---------- 拖曳 ----------

    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || owner.Inventory.Get(index) == null)
        {
            e.pointerDrag = null; // 空格不能拖
            return;
        }
        iconParent = icon.transform.parent;
        iconSiblingIndex = icon.transform.GetSiblingIndex();
        icon.transform.SetParent(rootCanvas.transform, true); // 移到最上層顯示
        icon.raycastTarget = false;
    }

    public void OnDrag(PointerEventData e)
    {
        if (iconParent != null) icon.transform.position = e.position;
    }

    public void OnEndDrag(PointerEventData e)
    {
        if (iconParent == null) return;
        icon.transform.SetParent(iconParent, false);
        icon.transform.SetSiblingIndex(iconSiblingIndex);
        icon.rectTransform.anchoredPosition = Vector2.zero;
        iconParent = null;
    }

    public void OnDrop(PointerEventData e)
    {
        var from = e.pointerDrag != null ? e.pointerDrag.GetComponent<InventorySlotUI>() : null;
        if (from != null && from != this) owner.Inventory.Swap(from.index, index);
    }
}
