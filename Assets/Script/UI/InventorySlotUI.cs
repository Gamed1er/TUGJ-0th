using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 物品欄的一格 (工作列上)。左鍵點擊 = 選取；拖曳到另一格 = 交換位置 (固定的道具 Pinned 不能拖、也不能被換)。
/// 電腦格 (TerminalButton) 也是一格，同一個物件上的 Button 會另外打開終端機。
/// 選中時顯示 selectedFrame (UI_select 框)。keyText、background 可以不接。
/// </summary>
public class InventorySlotUI : MonoBehaviour,
    IPointerClickHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IDropHandler
{
    [SerializeField] Image icon;
    [SerializeField] Text usesText;
    [Tooltip("道具不能用 (次數歸 0) 時套用的黑白材質")]
    [SerializeField] Material grayscaleMaterial;
    [SerializeField] Text keyText;
    [SerializeField] GameObject selectedFrame;
    [Tooltip("格子上的色塊 (平常透明)。無人機道具在場上有有電的無人機時變成 droneActiveColor")]
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
        if (keyText != null) keyText.text = (index + 1).ToString();
        rootCanvas = GetComponentInParent<Canvas>().rootCanvas;
        if (background != null) normalBackground = background.color;
    }

    void Update()
    {
        // 能不能用可能隨時間改變，所以每幀更新
        if (item != null) icon.material = item.definition.IsAvailable(item) ? null : grayscaleMaterial;

        if (background == null) return;
        bool droneActive = item != null && item.definition is DroneItem && Drone.Powered != null;
        background.color = droneActive ? droneActiveColor : normalBackground;
    }

    public void Refresh(ItemInstance item, bool selected)
    {
        this.item = item;
        selectedFrame.SetActive(selected);
        bool has = item != null;
        icon.enabled = has && item.definition.icon != null; // 電腦的圖畫在工作列底圖上，沒有 icon
        usesText.text = has && item.definition.ShowUses ? item.uses.ToString() : "";
        if (has)
        {
            icon.sprite = item.definition.icon;
            icon.material = item.definition.IsAvailable(item) ? null : grayscaleMaterial; // 用完變黑白，但道具保留
        }
    }

    public void OnPointerClick(PointerEventData e)
    {
        if (e.button == PointerEventData.InputButton.Left) owner.Inventory.Select(index);
    }

    // ---------- 拖曳 ----------

    public void OnBeginDrag(PointerEventData e)
    {
        if (e.button != PointerEventData.InputButton.Left || owner.Inventory.Get(index) == null || owner.Inventory.IsPinned(index))
        {
            e.pointerDrag = null; // 空格、固定的道具不能拖
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
