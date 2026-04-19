public enum ItemCategory { Resource, Consumable, UtilityTool, Weapon }
public enum EquipSlot    { Primary, Secondary, Defense, Utility }

[System.Serializable]
public class InventoryItem
{
    public string       id;
    public string       displayName;
    public ItemCategory category;
    public bool         stackable;
    public int          quantity;
    public int          maxStack;
    public EquipSlot?   equipSlot;  // null = not equippable from inventory

    public InventoryItem(string id, string displayName, ItemCategory category,
                         bool stackable, int quantity, int maxStack,
                         EquipSlot? equipSlot = null)
    {
        this.id          = id;
        this.displayName = displayName;
        this.category    = category;
        this.stackable   = stackable;
        this.quantity    = quantity;
        this.maxStack    = maxStack;
        this.equipSlot   = equipSlot;
    }

    public InventoryItem Clone() =>
        new InventoryItem(id, displayName, category, stackable, quantity, maxStack, equipSlot);
}
