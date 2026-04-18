public enum ItemCategory { Resource, Consumable, UtilityTool, Weapon }

[System.Serializable]
public class InventoryItem
{
    public string       id;
    public string       displayName;
    public ItemCategory category;
    public bool         stackable;
    public int          quantity;
    public int          maxStack;

    public InventoryItem(string id, string displayName, ItemCategory category,
                         bool stackable, int quantity, int maxStack)
    {
        this.id          = id;
        this.displayName = displayName;
        this.category    = category;
        this.stackable   = stackable;
        this.quantity    = quantity;
        this.maxStack    = maxStack;
    }

    public InventoryItem Clone() =>
        new InventoryItem(id, displayName, category, stackable, quantity, maxStack);
}
