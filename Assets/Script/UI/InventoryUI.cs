using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品欄畫面。依 Inventory 的格數自動生成 slotPrefab，監聽 Changed 事件刷新。
/// 格子排版交給 container 上的 HorizontalLayoutGroup。
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [SerializeField] Inventory inventory;
    [SerializeField] InventorySlotUI slotPrefab;
    [SerializeField] RectTransform container;

    readonly List<InventorySlotUI> slots = new List<InventorySlotUI>();

    public Inventory Inventory => inventory;

    void Start()
    {
        for (int i = 0; i < inventory.SlotCount; i++)
        {
            var slot = Instantiate(slotPrefab, container);
            slot.Init(this, i);
            slots.Add(slot);
        }
        inventory.Changed += Refresh;
        Refresh();
    }

    void OnDestroy()
    {
        if (inventory != null) inventory.Changed -= Refresh;
    }

    void Refresh()
    {
        for (int i = 0; i < slots.Count; i++)
            slots[i].Refresh(inventory.Get(i), i == inventory.Selected);
    }
}
