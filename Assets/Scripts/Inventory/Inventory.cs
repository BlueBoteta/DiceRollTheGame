using System;
using UnityEngine;

public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    public const int SlotCount = 8;

    readonly InventoryItem[] _slots = new InventoryItem[SlotCount];

    public event Action OnChanged;

    void Awake()
    {
        if (Instance != null) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SeedStartingItems();
    }

    void SeedStartingItems()
    {
        Add("ammo",    10);
        Add("scrap",   1);
        Add("meds",    1);
        Add("battery", 1);
    }

    // ── Queries ───────────────────────────────────────────────────────────────

    public InventoryItem GetSlot(int i) => (i >= 0 && i < SlotCount) ? _slots[i] : null;

    public int Count(string id)
    {
        foreach (var s in _slots)
            if (s != null && s.id == id) return s.quantity;
        return 0;
    }

    public bool Has(string id, int amount = 1) => Count(id) >= amount;

    public bool IsFull()
    {
        foreach (var s in _slots)
            if (s == null) return false;
        return true;
    }

    public bool HasEmptySlot() => !IsFull();

    // ── Adding ────────────────────────────────────────────────────────────────

    // Returns true = item placed. False = inventory full, caller shows Replace/Leave prompt.
    public bool Add(string id, int qty = 1)
    {
        var template = ItemFactory.Create(id, qty);
        if (template == null) return false;

        if (template.stackable)
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (_slots[i] != null && _slots[i].id == id)
                {
                    _slots[i].quantity = Mathf.Min(_slots[i].quantity + qty, _slots[i].maxStack);
                    OnChanged?.Invoke();
                    return true;
                }
            }
        }

        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i] == null)
            {
                _slots[i] = ItemFactory.Create(id, qty);
                OnChanged?.Invoke();
                return true;
            }
        }

        return false; // Full — caller must handle Replace or Leave
    }

    // ── Removing ──────────────────────────────────────────────────────────────

    // Returns true if the item existed and was removed.
    public bool Remove(string id, int qty = 1)
    {
        for (int i = 0; i < SlotCount; i++)
        {
            if (_slots[i] != null && _slots[i].id == id)
            {
                if (_slots[i].quantity < qty) return false;
                _slots[i].quantity -= qty;
                if (_slots[i].quantity <= 0) _slots[i] = null;
                OnChanged?.Invoke();
                return true;
            }
        }
        return false;
    }

    public void DropSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        _slots[slotIndex] = null;
        OnChanged?.Invoke();
    }

    // Used by "Replace" prompt: overwrite slot with incoming item.
    public void ReplaceSlot(int slotIndex, string id, int qty = 1)
    {
        if (slotIndex < 0 || slotIndex >= SlotCount) return;
        _slots[slotIndex] = ItemFactory.Create(id, qty);
        OnChanged?.Invoke();
    }

    public void SwapSlots(int a, int b)
    {
        if (a < 0 || a >= SlotCount || b < 0 || b >= SlotCount) return;
        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        OnChanged?.Invoke();
    }
}
