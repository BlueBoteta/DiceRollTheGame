// Single source of truth for every item definition in the game.
public static class ItemFactory
{
    public static InventoryItem Create(string id, int quantity = 1)
    {
        return id switch
        {
            // ── Resources (stackable, used in crafting / trading / mini-games) ──
            "ammo"    => new InventoryItem("ammo",    "Ammo",             ItemCategory.Resource,    true,  quantity, 99),
            "scrap"   => new InventoryItem("scrap",   "Scrap",            ItemCategory.Resource,    true,  quantity, 99),
            "meds"    => new InventoryItem("meds",    "Medical Supplies", ItemCategory.Resource,    true,  quantity, 99),
            "battery" => new InventoryItem("battery", "Battery",          ItemCategory.Resource,    true,  quantity, 99),

            // ── Consumables (single-use, instant effect) ──────────────────────
            "medkit"  => new InventoryItem("medkit",  "Medkit",           ItemCategory.Consumable,  false, quantity, 1),
            "food"    => new InventoryItem("food",    "Food",             ItemCategory.Consumable,  false, quantity, 1),
            "pills"   => new InventoryItem("pills",   "Pills",            ItemCategory.Consumable,  false, quantity, 1),

            // ── Utility Tools ────────────────────────────────────────────────
            "lockpick"  => new InventoryItem("lockpick",  "Lockpick",   ItemCategory.UtilityTool, false, quantity, 3),
            "flashlight"=> new InventoryItem("flashlight","Flashlight", ItemCategory.UtilityTool, false, quantity, 1, EquipSlot.Utility),

            // ── Weapons — equip from inventory into HUD slots ─────────────────
            "pistol"  => new InventoryItem("pistol",  "Pistol",  ItemCategory.Weapon, false, quantity, 1, EquipSlot.Primary),
            "shotgun" => new InventoryItem("shotgun", "Shotgun", ItemCategory.Weapon, false, quantity, 1, EquipSlot.Primary),
            "knife"   => new InventoryItem("knife",   "Knife",   ItemCategory.Weapon, false, quantity, 1, EquipSlot.Secondary),

            _         => null
        };
    }
}
