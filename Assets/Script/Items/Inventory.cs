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

    /// <summary>除錯用 (DebugTools F1)：無限道具。開著時 Consume 不扣次數，次數 0 的道具補回來。static，Play 開始時重置。</summary>
    public static bool InfiniteUses { get; set; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatics() => InfiniteUses = false;
    /// <summary>鎖定時不能選格、切換、拖曳交換。目前沒有地方使用 (操控無人機時也能切換格子)，留給之後需要時用。</summary>
    public bool Locked { get; set; }
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
        if (Locked || index < 0 || index >= slots.Length || index == Selected) return;
        Selected = index;
        Changed?.Invoke();
    }

    /// <summary>step = +1 下一格 / -1 上一格，會循環。</summary>
    public void Cycle(int step)
    {
        if (Locked || step == 0) return;
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
        if (item == null || item.uses <= 0 || InfiniteUses) return;
        item.uses--;
        Changed?.Invoke();
    }

    /// <summary>這格的道具固定不能移動 (ItemDefinition.Pinned)。</summary>
    public bool IsPinned(int index) => slots[index] != null && slots[index].definition.Pinned;

    /// <summary>次數 0 的道具補回開局次數 (至少 1)。無限道具開啟 / 讀檔時用。</summary>
    public void RefillEmpty()
    {
        foreach (var s in slots)
            if (s != null && s.uses <= 0) s.uses = Mathf.Max(1, s.definition.startingUses);
        Changed?.Invoke();
    }

    public void Swap(int a, int b)
    {
        if (Locked || a == b || IsPinned(a) || IsPinned(b)) return;
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
        // 預設選第一個一般道具 (跳過電腦這種固定格)
        Selected = Mathf.Max(0, Array.FindIndex(slots, x => x != null && !x.definition.Pinned));
        Changed?.Invoke();
    }

    // ---------- 存讀檔 ----------

    public void WriteTo(RoomSnapshot data)
    {
        data.items.Clear();
        foreach (var s in slots)
            data.items.Add(s == null ? new ItemSave() : new ItemSave { itemId = s.definition.id, uses = s.uses });
        data.selectedSlot = Selected;
    }

    public void ReadFrom(RoomSnapshot data)
    {
        Array.Clear(slots, 0, slots.Length);
        for (int i = 0; i < data.items.Count && i < slots.Length; i++)
        {
            var def = ItemRegistry.Get(data.items[i].itemId);
            if (def != null) slots[i] = new ItemInstance(def, data.items[i].uses);
        }
        Selected = Mathf.Clamp(data.selectedSlot, 0, slots.Length - 1);
        if (InfiniteUses) RefillEmpty(); // 死亡重載回到次數 0 的快照時
        Changed?.Invoke();
    }
}
