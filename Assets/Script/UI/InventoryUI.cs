using UnityEngine;

/// <summary>
/// 物品欄畫面 (工作列上的 6 格：最左邊的電腦 TerminalButton + Slot1~5)。格子 (InventorySlotUI) 直接擺在場景裡、依序拖進 slots，監聽 Changed 事件刷新。
/// 格子數要跟 Inventory 的 slotCount 一樣。
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] InventorySlotUI[] slots;

    public Inventory Inventory => inventory;

    void Start()
    {
        if (slots.Length != inventory.SlotCount)
            Debug.LogWarning($"InventoryUI 有 {slots.Length} 格，但 Inventory 有 {inventory.SlotCount} 格", this);
        for (int i = 0; i < slots.Length; i++) slots[i].Init(this, i);
        inventory.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= Refresh;
    }

    void Refresh()
    {
        for (int i = 0; i < slots.Length; i++)
            slots[i].Refresh(i < inventory.SlotCount ? inventory.Get(i) : null, i == inventory.Selected);
    }
}
