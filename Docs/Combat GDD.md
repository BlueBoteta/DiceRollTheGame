# Combat System GDD
**Last Updated:** 2026-04-24  
**Branch:** movement  
**Status:** Design locked — ready for implementation

---

## Overview

Auto-battle combat where the player's equipped gear defines HOW they fight. The player cannot pick moves — instead, success comes from smart loadout building before combat and tactical consumable use during combat. Combat is horror-paced: ammo runs dry, zombies rage, items drop on the floor mid-fight.

---

## Core Combat Loop

1. Player lands on a Combat tile
2. CombatScreen opens — player vs 1–3 enemies (scales by lap)
3. Auto-battle runs: player attacks based on equipped weapon stats
4. Enemies drop loot mid-fight — item icon appears on the battlefield floor
5. Player can hold a dropped item to pause the fight → drag to inventory or leave it
6. Player can tap consumables (meds, etc.) from bottom inventory bar at any time
7. All enemies dead → combat ends → XP/loot summary → return to board

---

## Equipment Slots (4 total)

| Slot | Type | Examples |
|------|------|---------|
| **Gun** | Ranged weapon | Pistol, Shotgun, SMG, Revolver, Rifle |
| **Melee** | Backup weapon | Spiked Bat, Knife, Wrench, Crowbar, Fire Axe |
| **Defense** | Armor/protection | Vest, Riot Gear, Helmet, Makeshift Shield |
| **Utility** | Active-use item | Grenade, Adrenaline Shot, Trip Mine, Flashlight |

- Gun auto-fires until ammo is empty → auto-switches to Melee
- Running out of ammo mid-fight should feel panicky (slower, weaker melee)
- Utility slot = one active-use per fight, manually triggered by player

---

## Stat System

### Core Stats

| Stat | In-Game Name | Description |
|------|-------------|-------------|
| Damage | **DMG** | Base damage per hit |
| Attack Speed | **Attack Speed** | How fast the player attacks (auto-fire rate) |
| Critical Chance | **Crit Chance** | % chance to land a critical hit |
| True Damage | **Armor Pierce** | Damage that ignores enemy armor |
| Max HP | **Max HP** | Increases HP bar ceiling (bar visually extends) |
| Armor / Defense | **Armor** | Flat damage reduction per enemy hit |
| Dodge Chance | **Dodge** | % chance to fully avoid a hit |
| Life Steal | **Blood Drain** | % of damage dealt returned as HP |
| HP Regeneration | **Field Dressing** | Passive HP recovery per second |
| Kill Heal | **Kill Rush** | HP restored on each enemy kill |
| Luck | **Scavenger's Luck** | Improves loot drop rate and quality |
| Item Range | **Effective Range** | Gun damage falloff — longer range = less dropoff |

### Unique / Horror Stats

| Stat | Description | Primary Source |
|------|-------------|----------------|
| **Bleed** | Chance to apply bleed — enemy takes small DMG every second | Knife, Spiked Bat |
| **Frenzy** | Each kill grants attack speed boost for 3 seconds | Melee weapons, rare accessories |
| **Last Stand** | Below 20% HP → significant damage spike | Rare/Prototype items only |
| **Hollow Point** | Increases critical HIT damage (not chance) | Guns only |
| **Ammo Save** | % chance a shot doesn't consume ammo | Guns only |
| **Scavenger** | Increases enemy loot drop chance during combat | Utility/Accessories |

---

## Stat Distribution by Item Type

| Item Type | Eligible Stats |
|-----------|---------------|
| **Guns** | DMG, Attack Speed, Crit Chance, Effective Range, Armor Pierce, Hollow Point, Ammo Save |
| **Melee** | DMG, Attack Speed, Crit Chance, Blood Drain, Kill Rush, Bleed, Frenzy, Last Stand |
| **Defense** | Armor, Max HP, Dodge, Field Dressing, Kill Rush (rare), Last Stand (rare) |
| **Utility / Accessories** | Scavenger's Luck, Scavenger, Field Dressing, Frenzy, any stat as rare bonus |

---

## Item Rarity System

| Tier | Color | Name | Stat Count | Notes |
|------|-------|------|------------|-------|
| 1 | Grey | **Scavenged** | 1 | Low values, rusty/barely functional |
| 2 | Green | **Salvaged** | 1–2 | Functional, reliable |
| 3 | Blue | **Modified** | 2 | Good values + 1 unique stat possible |
| 4 | Purple | **Military** | 3 | High values, always has a unique stat |
| 5 | Gold/Orange | **Prototype** | 3–4 | Max values, guaranteed rare unique stat |

**Key design rule:** The same weapon exists at every rarity tier.  
A Scavenged Pistol vs a Military Pistol = same weapon, wildly different stats.  
This keeps every loot drop exciting — you never know what you'll find.

---

## Enemy System

| Enemy Type | Behavior | Threat |
|------------|----------|--------|
| **Zombie** | Slow, straightforward | Low |
| **Crawler** | Fast, low HP | Harassment |
| **Brute** | Tanky, hits very hard | High HP check |
| **Screamer** | Summons 1 extra zombie if not killed fast | Priority target |

### Enemy Rage
When any enemy drops below 30% HP → it speeds up.  
Visual cue: red eyes, faster animation, audio shift.

### Enemy Grab Attack
Random chance a zombie grabs the player → reduces player attack speed for 2 seconds.  
Visual: player sprite shakes, screen briefly distorts.

---

## Mid-Fight Loot System

- When an enemy dies mid-fight, it may drop an item
- Item icon appears on the battlefield floor with rarity color glow
- **Hold** the item icon → game PAUSES
- Player can **drag** it to an inventory slot or **leave** it on the ground
- Uncollected items are lost when combat ends
- This creates real decisions: do I stop to grab this while 2 zombies are still alive?

---

## HP Bar System

- HP displayed as a green bar with numbers (e.g. `110 / 180`)
- **Blood Drain** (life steal) makes the bar tick up on each hit
- **Field Dressing** (regen) makes the bar slowly recover over time
- **Max HP items** permanently raise the bar ceiling — the bar visually extends
- **Critical HP (below 20%):**
  - Screen vignette goes red
  - Heartbeat audio cue
  - Player sprite gets blood overlay
  - Any equipped **Last Stand** stat activates here

---

## Player Intervention During Combat

| Action | How |
|--------|-----|
| Use consumable | Tap meds/food/pills from bottom inventory bar |
| Pick up floor loot | Hold item icon → game pauses → drag or leave |
| Use utility item | Tap utility slot button (grenade, adrenaline shot, etc.) |

---

## Build Archetypes (examples for design reference)

| Build | Gear | Playstyle |
|-------|------|-----------|
| **Glass Cannon** | Prototype Pistol (Hollow Point + Crit Chance) | Massive burst, dies fast |
| **Berserker** | Military Spiked Bat (Frenzy + Blood Drain + Last Stand) | Melee maniac, scary at low HP |
| **Survivor** | Scavenged Rifle + Riot Gear (Armor + Field Dressing) | Slow, tanky, outlasts everything |
| **Scavenger** | Any gun + Vest (Scavenger's Luck + Scavenger) | Farms loot aggressively |

---

## UI Layout (based on reference design)

- **Top:** Enemy HP bars with names and values
- **VS** divider in center
- **Player HP bar** — green, shows numbers, animates on change
- **Battlefield:** Player sprite left, enemy sprites right, floating damage numbers
- **Bottom inventory bar:** Items collected so far this combat + consumables accessible here
- **Utility button:** Tap to use active utility item
- **Floor loot:** Appears in battlefield area when enemy drops item

---

## Ammo Economy in Combat

- Gun auto-fires, consuming ammo from inventory each shot
- **Ammo Save** stat gives % chance to not consume ammo on a shot
- When ammo hits 0 → auto-switch to Melee slot (no ammo needed)
- Melee is weaker than guns — running dry is a real problem, not a fallback
- Player cannot reload mid-fight — ammo is what you had going in

---

## Scaling (by lap)

| Lap | Enemy Count | Enemy HP Multiplier | Loot Rarity Boost |
|-----|-------------|--------------------|--------------------|
| 1 | 1–2 | 1× | Scavenged / Salvaged |
| 2 | 2–3 | 1.25× | Salvaged / Modified |
| 3 | 2–3 | 1.5× | Modified / Military |
| 4+ | 3 | 2× | Military / Prototype |
