using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>一格裡的道具：哪種道具 + 剩餘可用次數。次數歸 0 道具也不會消失。</summary>
[Serializable]
public class ItemInstance
{
    public ItemDefinition definition;
    public int uses;

    public ItemInstance(ItemDefinition def, int uses)
    {
        definition = def;
        this.uses = uses;
    }
}

/// <summary>
/// 物品欄 (純資料 + 規則)，掛在玩家身上。不處理輸入 (輸入在 PlayerItemUser)，不處理畫面 (畫面在 InventoryUI)。
/// 任何變動都會觸發 Changed 事件，UI 監聽它來刷新。
/// </summary>
public class Inventory : MonoBehaviour
{
    [Range(1, 9)] [SerializeField] int slotCount = 6;

    ItemInstance[] slots;

    public event Action Changed;
    public int SlotCount => slots.Length;
    public int Selected { get; private set; }
    public ItemInstance SelectedItem => slots[Selected];
    public ItemInstance Get(int index) => slots[index];

    public bool IsFull
    {
        get
        {
            foreach (var s in slots) if (s == null) return false;
            return true;
        }
    }

    void Awake()
    {
        slots = new ItemInstance[slotCount];
    }

    public void Select(int index)
    {
        if (index < 0 || index >= slots.Length || index == Selected) return;
        Selected = index;
        Changed?.Invoke();
    }

    /// <summary>step = +1 下一格 / -1 上一格，會循環。</summary>
    public void Cycle(int step)
    {
        if (step == 0) return;
        Select((Selected + step + slots.Length) % slots.Length);
    }

    public ItemInstance Find(ItemDefinition def) => Array.Find(slots, s => s != null && s.definition == def);

    /// <summary>替某種道具加次數；物品欄裡沒有這個道具就放進第一個空格。沒空格回傳 false。</summary>
    public bool AddUses(ItemDefinition def, int amount)
    {
        if (def == null) return false;
        var existing = Find(def);
        if (existing != null)
        {
            existing.uses += amount;
            Changed?.Invoke();
            return true;
        }
        int empty = Array.IndexOf(slots, null);
        if (empty < 0) return false;
        slots[empty] = new ItemInstance(def, amount);
        Changed?.Invoke();
        return true;
    }

    /// <summary>扣 1 次 (最低到 0，道具保留在格子裡)。</summary>
    public void Consume(ItemInstance item)
    {
        if (item == null || item.uses <= 0) return;
        item.uses--;
        Changed?.Invoke();
    }

    public void Swap(int a, int b)
    {
        if (a == b) return;
        (slots[a], slots[b]) = (slots[b], slots[a]);
        // 選取框跟著被拖走的道具
        if (Selected == a) Selected = b;
        else if (Selected == b) Selected = a;
        Changed?.Invoke();
    }

    /// <summary>清空並依序放入道具 (各自的初始次數)。開新遊戲用。</summary>
    public void SetItems(IList<ItemDefinition> defs)
    {
        Array.Clear(slots, 0, slots.Length);
        for (int i = 0; i < defs.Count && i < slots.Length; i++)
            if (defs[i] != null) slots[i] = new ItemInstance(defs[i], defs[i].startingUses);
        Selected = 0;
        Changed?.Invoke();
    }

    // ---------- 存讀檔 ----------

    public void WriteTo(SaveData data)
    {
        data.items.Clear();
        foreach (var s in slots)
            data.items.Add(s == null ? new ItemSave() : new ItemSave { itemId = s.definition.id, uses = s.uses });
        data.selectedSlot = Selected;
    }

    public void ReadFrom(SaveData data)
    {
        Array.Clear(slots, 0, slots.Length);
        for (int i = 0; i < data.items.Count && i < slots.Length; i++)
        {
            var def = ItemRegistry.Get(data.items[i].itemId);
            if (def != null) slots[i] = new ItemInstance(def, data.items[i].uses);
        }
        Selected = Mathf.Clamp(data.selectedSlot, 0, slots.Length - 1);
        Changed?.Invoke();
    }
}
