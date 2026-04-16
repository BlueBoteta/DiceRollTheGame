# Game Design Document — DiceRollTheGame

**Version:** 0.1  
**Date:** 2026-04-16  
**Genre:** Survival Roguelite / Horror Board Game  
**Platform:** PC  
**Perspective:** 2D Top-Down Isometric  

---

## 1. Vision

A survival horror roguelite where the player rolls dice to move across a tile-based board set in a zombie-infested world. Every tile triggers an event — combat, loot, story, or a mini-game. Resources are scarce, dread escalates each loop, and death sends the player back to the last safe shelter to try again.

The game draws its **atmosphere and emotional weight from Resident Evil and Silent Hill** — desperate survival, psychological horror, and a world that feels hopeless but not without meaning. The **board game roguelite mechanics** are the delivery system for that experience.

**Core Fantasy:** Every dice roll is a gamble for survival. You never have enough ammo. You're always one bad roll away from death.

---

## 2. Core Gameplay Loop

```
Roll Dice
  → Move on Tiles
    → Trigger Event (Combat / Loot / Story / Mini-game)
      → Collect Resources
        → Complete Loop
          → Boss Fight
            → Death or Continue
              → Return to Village → Repeat
```

- Loops escalate in danger and horror with each cycle
- Completing a loop and defeating the area boss unlocks the next infected zone
- Death resets the run but progress (unlocks, meta upgrades) carries over

---

## 3. Movement System (Board System)

| Property | Detail |
|---|---|
| Input | Player taps/clicks a Roll button |
| Result | Dice roll determines number of tiles moved |
| Movement | Automatic — player token moves along the board path |
| Camera | 2D isometric orthographic, follows player token |
| View | 2D Top-Down Isometric |

**Tile Types:**
- **Combat** — triggers a zombie/enemy encounter
- **Loot** — grants resources (ammo, health, items salvaged from the environment)
- **Story** — narrative horror event with a choice or emotional consequence
- **Mini-game** — small survival challenge for bonus resources
- **Boss** — a powerful mutated enemy or monster at end of each loop cycle

---

## 4. Resource System

| Resource | Purpose | Notes |
|---|---|---|
| HP (Health) | Player survivability | Core stat, reaching 0 = death |
| Ammo | Required for ranged attacks | No ammo = weak melee fallback |
| Sanity | Optional psychological meter | Affects events/choices (optional for now) |

- Resources are limited and managed across the full run
- No full restores between tiles — scarcity is intentional

---

## 5. Combat System (Separate Screen)

When the player lands on a Combat tile or reaches the Boss tile, the game transitions to a dedicated **Combat Screen**.

### Combat Style
- Simple **auto-battler** — combat resolves based on stats, not real-time input

### Player Options Each Turn
| Option | Condition |
|---|---|
| Attack (auto) | Always available |
| Use Item | If item is in inventory |
| Escape | Chance-based — not guaranteed |

### Combat Stats
- **Weapon** — equipped weapon determines attack type
- **Attack** — base damage value
- **Ammo** — consumed per ranged attack

### Important Rule
> **No Ammo → Weak Melee Attack only.** Ammo management is a core tension driver.

### Combat Resolution
- Auto-battler calculates hit/damage each round
- Enemy health depletes → Victory (loot + continue)
- Player HP depletes → Death (run ends)

---

## 6. Boss System

- A **Boss** (mutated zombie, monster, or a disturbing human enemy) appears after completing several loops
- Bosses are significantly stronger with unique threatening behaviors
- Defeating a Boss rewards:
  - Progress unlock (new infected zone, item, or ability)
  - Better salvage loot than standard encounters
- Bosses act as the emotional climax and checkpoint of each area

---

## 7. Stats System (Basic)

Keep only essential stats for now:

| Stat | Description |
|---|---|
| HP | Player health pool |
| Attack | Base melee/ranged damage |
| Ammo | Ranged attack resource |
| Sanity | (Optional) Affects event outcomes |

Stats can be upgraded between runs via village meta-progression.

---

## 8. Meta Progression

- Dying ends the current run but **progress carries over**
- Completing loops and bosses **unlocks the next infected zone**
- Players return to the **safe house / shelter hub** between runs
- Shelter may offer: upgrades, new starting supplies, story context, emotional narrative beats

---

## 9. UI Design (Simple)

| Element | Location | Notes |
|---|---|---|
| Board | Center of screen | Main play area |
| Roll Button | Bottom center | Large, tap-friendly |
| HP | Small HUD | Always visible |
| Ammo | Small HUD | Always visible |
| Combat Screen | Full screen overlay | Shown during encounters |
| Treasure/Event Chest | Pop-up panel | Shown on loot/story tiles |

**Design principle:** Minimal UI. Only show what the player needs right now.

---

## 10. Screens & Flow

```
Main Menu
  → New Run
    → Safe House Hub (meta)
      → Board (Movement + Tiles)
        → Event Screen (Combat / Loot / Story)
          → Back to Board
            → Boss Fight
              → Death Screen or Area Cleared Screen
                → Safe House Hub
```

---

## 11. Scope (MVP)

### In Scope (v0.1)
- Tile-based board with dice roll movement
- 3 tile types: Combat, Loot, Boss
- Basic auto-battler combat screen
- Core stats: HP, Attack, Ammo
- 1 area / 1 boss
- Simple main menu + HUD

### Out of Scope (later)
- Story / Mini-game tiles
- Sanity system
- Multiple locations / areas
- Village meta-progression UI
- Inventory system
- Full audio and VFX polish

---

## 12. Art Direction

- **View:** 2D top-down isometric board
- **Tone:** Dark, gritty, oppressive — survival horror atmosphere
- **Influences:** Resident Evil (tension, resource scarcity, monster design) + Silent Hill (psychological dread, emotional weight, disturbing imagery)
- Environments: Abandoned cities, hospitals, dark forests, overrun shelters
- Enemies: Zombies, mutated creatures, disturbing human enemies
- Color palette: Desaturated, muted greens/grays/blacks with high-contrast red for danger
- Minimal animations for MVP, with screen-shake and dark overlays for impact
- PC layout — mouse click to roll, keyboard shortcuts optional

---

*This document will be updated as development progresses.*
