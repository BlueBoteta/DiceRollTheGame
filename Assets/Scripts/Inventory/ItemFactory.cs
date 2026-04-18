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

            // ── Utility Tools (limited charges, exploration only) ─────────────
            "lockpick"=> new InventoryItem("lockpick","Lockpick",         ItemCategory.UtilityTool, false, quantity, 3),

            // ── Weapons (stored here, equipped via Armory — not used from inv) ─
            "pistol"  => new InventoryItem("pistol",  "Pistol",           ItemCategory.Weapon,      false, quantity, 1),
            "shotgun" => new InventoryItem("shotgun", "Shotgun",          ItemCategory.Weapon,      false, quantity, 1),
            "knife"   => new InventoryItem("knife",   "Knife",            ItemCategory.Weapon,      false, quantity, 1),

            _         => null
        };
    }
}
