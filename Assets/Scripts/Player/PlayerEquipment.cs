using UnityEngine;
using System;

public class PlayerEquipment : MonoBehaviour
{
    public static PlayerEquipment Instance { get; private set; }

    public event Action OnChanged;

    readonly InventoryItem[] _slots = new InventoryItem[4]; // indexed by EquipSlot

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public InventoryItem Get(EquipSlot slot) => _slots[(int)slot];

    // Equips item into slot. Returns displaced item (or null) — caller puts it back in inventory.
    public InventoryItem Equip(EquipSlot slot, InventoryItem item)
    {
        var displaced = _slots[(int)slot];
        _slots[(int)slot] = item;
        OnChanged?.Invoke();
        return displaced;
    }

    public InventoryItem Unequip(EquipSlot slot)
    {
        var item = _slots[(int)slot];
        _slots[(int)slot] = null;
        OnChanged?.Invoke();
        return item;
    }

    public void ResetAll()
    {
        for (int i = 0; i < _slots.Length; i++) _slots[i] = null;
        OnChanged?.Invoke();
    }

    public bool IsEquipped(string id)
    {
        foreach (var s in _slots)
            if (s != null && s.id == id) return true;
        return false;
    }
}
