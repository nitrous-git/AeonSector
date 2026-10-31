# Aeon//Sector — Living Project Context

**Project:** 2D isometric, turn-based mecha tactical RPG in development  
**Repository:** `nitrous-git/AeonSector` · **Working branch:** `New-features-test`  
**Local engine baseline:** Unity `6000.6.7f1`  
**Context refresh:** 2026-10-31, reviewed against commit `d5954079d77d59d5c38dd0a2e627c2d49f5181c6`

This document describes the **current working foundation and active expansion direction**. Earlier stabilization tasks are incorporated into the baseline, not an ongoing audit checklist. Confirm any detail affected by the current task in the actual Unity checkout/Editor. Read only the relevant sections.

## 1. Game identity and creative direction

Aeon//Sector is a **turn-based, grid-based, 2D isometric science-fiction tactical game** featuring playable mecha and insectoid enemies. We are evolving the combat prototype toward a cohesive tactical RPG with distinct unit roles, tactical tradeoffs, satisfying encounters, strong visual feedback, and eventually richer missions and customization.

Design priorities:
- **Clarity:** players understand selected units, attack reach, blocked paths, action availability, and damage outcomes.
- **Unit identity:** each mech or enemy has a coherent role, capabilities, silhouette, behavior, and counterplay.
- **Tactical decisions:** movement, positioning, range, and target choice matter.
- **Presentation:** readable original sprites, animations, VFX, sound, polished menus, and a unified isometric aesthetic.
- **Production:** new units should be authorable primarily from configuration and prefabs, without modifying core controllers for every archetype.

The creative agent may propose and implement original content **within its assigned phase**. Cover, pilot progression, salvage, modular loadouts, equipment, terrain effects, campaign systems, and special abilities are possible future directions, **not all existing functionality**.

## 2. Technology and project structure

- Unity `6000.6.7f1`, URP `17.6.0`, 2D Renderer, Tilemaps, Cinemachine `6.6.0`, uGUI and TextMesh Pro.
- Current gameplay uses legacy `UnityEngine.Input` for mouse/keyboard. A wholesale input-system migration is not part of P1.0A.
- Unity Test Framework `1.8.0` is available; no project-owned gameplay test suite was present at the last source review.
- Unity bridge supports direct Editor/source inspection and asset modifications. The project owner performs principal gameplay acceptance testing.
- Preserve the project's existing assets, scene references, and `.meta` GUIDs. Original new visuals should fit the current 2D isometric pipeline.

### Existing runtime ownership

| Area | Current implementation | Contract |
| --- | --- | --- |
| Battle/turns | `Assets/Scripts/Core/TurnManager.cs`, `BattleState.cs` | Faction turns, battle initialization, result state and end screen |
| Rosters | `Assets/Scripts/Factions/Faction.cs`, `UnitManager.cs` | Faction membership and living units |
| Runtime unit | `Assets/Scripts/Units/CombatUnit.cs` | HP, current cell, facing, moved/attacked flags, death |
| Unit definition | `Assets/Scripts/Units/UnitStats.cs`, `SO_UnitStats/` | ScriptableObject identity, core stats, attacks and card presentation data |
| Tactical board | `Assets/Scripts/Grid/TilemapBoardAdapter.cs` | Walkability, occupancy, world/grid conversion, overlays |
| Navigation/range | `Assets/Scripts/Pathfinding/` | BFS reachability, A* routing, attack cells |
| Player interaction | `Assets/Scripts/Input/PlayerInputController.cs`, `CommandMode.cs` | Input, selection, command modes, move/attack orchestration; **P1.0A refactor target** |
| Enemy behavior | `Assets/Scripts/AI/EnemyTurnController.cs` | AI turn sequencing, advance/melee/artillery; **P1.0B extension target** |
| Unit animation | `Assets/Scripts/Units/UnitMover.cs`, `UnitVisualAnimator.cs` | Path animation, directional sprites, attack animation callbacks |
| UI | `Assets/Scripts/UI/` | Unit cards, action menu, turn/result display, main menu |
| Audio/effects | `Assets/Scripts/Music_SFX/`, `Assets/Scripts/Projectiles/`, `Assets/Scripts/Camera/` | Battle SFX, projectile and camera feedback |

### Current combat invariants

- A valid unit enters the battle roster only after successful board placement. The board's occupancy grid is authoritative.
- Movement uses BFS reachability plus A* pathfinding. Reserve the destination before animating; an invalid/rejected move does not consume the move action.
- `CombatUnit.BeginTurn()` resets **one move allowance and one attack allowance**, not a shared AP budget.
- Player melee, ranged, and enemy area attacks exist; enemy melee and artillery behaviors exist.
- Ranged resolution tracks firing, projectile completion, one-time damage, and animation completion. A missing animation-event fallback exists; normal attack sequencing must not reintroduce duplicate damage or early input unlock.
- Death removes a unit from occupancy and its faction roster. Victory/defeat are terminal states: no further turn handoff or post-battle damage should occur.
- Result SFX routing and repeated banner-duration mutation were corrected. Turn UI shows current turn without imposing a fictitious ten-turn mission cap.
- **EN is a derived UI score**, weighted from health and remaining move/attack allowances. It is **not** fuel, ammo, mana, or a spendable energy pool. DisplayMaxHP and DisplayMaxEN in `UnitStats` are presentation numbers.

These are current design/implementation contracts. They do **not** imply that every path has comprehensive automated regression coverage.

## 3. Current scenes and content

**Enabled scenes:**

| Scene | Current role | Initial serialized units |
| --- | --- | --- |
| `Assets/Scenes/MainMenu.unity` | Entry and Play | — |
| `Assets/Scenes/BattleTestScene01.unity` | First encounter | 1 player / 3 enemies |
| `Assets/Scenes/BattleTestScene04.unity` | Follow-on encounter | 2 player / 6 enemies |
| `Assets/Scenes/BattleTestScene06.unity` | Later encounter | 2 player / 6 enemies |

Other experimental scenes exist; do not delete or repurpose them without an assigned task.

**Current serialized navigation:** MainMenu → 01 → 04 → 06. Result-screen No actions return to MainMenu. Scene06 Yes currently reloads Scene06; that may serve as replay, but the final-battle UX is not yet defined. The same Yes/No result controls can appear on victory and defeat, so distinguish *retry*, *next mission*, and *return* intentionally when improving menus. Full button-driven route testing was not confirmed at the last review.

**Existing asset families:** `Assets/Prefab/PlayerMech/`, `Assets/Prefab/EnemyInsect/`, projectile/slash prefabs, unit animations, isometric tilemaps, SFX, and UI resources. Reuse compatible assets, but create coherent original new content when authorized.

**Current unit definitions:**
- Playable hybrid mech uses active combat data and melee/ranged attacks.
- Insect melee and insect artillery enemies exist.
- Legacy `SO_Mech_Melee.asset` and `SO_Mech_Range.asset` were migrated to the current stats schema; both have MaxHP 10 / MoveRange 2, with original primary attack damage/ranges retained.
- Those migrated assets **still have no icon and zero card display HP/EN values**. Complete their presentation configuration before featuring those archetypes as finished playable content.

## 4. Unit authoring — current workflow and expansion goal

**Existing workflow:** create/configure a `UnitStats` asset; assign it to a compatible `CombatUnit` prefab; ensure the `UnitMover`, directional visual animator, hit points UI, and any projectile/animation-event references are valid; place the prefab on a walkable scene cell; add it once to the correct `TurnManager` roster; test selection, movement, attack, death and result state.

**Current limitations to evolve, not historical bugs to re-audit:**
- `UnitType` currently combines classification with behavior. In particular, `EnemyTurnController` checks `UnitType.InsectArtillery` to select area AI; other enemy types use melee behavior. New ranged/hybrid enemies require configurable tactical behavior rather than another hardcoded enum branch.
- Player attack commands are gated by `CanAttack`, not by a per-unit list of equipped/supported attacks. Defining a “melee mech” does not automatically restrict its ranged command.
- Specialized abilities, modular weapon/loadout assets, and a reusable unit-roster selection screen do not yet exist.
- `PlayerInputController` was ~950 lines at the last review and mixes input, selection, movement, attack resolution, and UI. Its responsibilities need clean extraction before abilities multiply.
- `CombatUnit` still finds its health bar by a hierarchy child index, which is fragile for new prefabs; prefer a safer serialized/component reference as part of a relevant unit-content task.
- Normal ranged attacks were exercised successfully, but the current five-second fallback does not recover an indefinitely stalled projectile coroutine, because completion still awaits the projectile-resolved flag. Handle this if redesigning attack resolution.

**Target unit-authoring model:** independent **unit identity/stat definition**, **combat capabilities/attack configuration**, **tactical AI behavior profile**, **runtime battle state**, and **prefab/visual presentation**. The exact classes/data assets should be chosen incrementally after inspecting the actual source; avoid building a generic RPG framework before the first new units prove the design.

## 5. Active roadmap

| Checkpoint | Purpose | Practical completion condition |
| --- | --- | --- |
| **P1.0A — Player controller refactor** | Extract cohesive selection, movement, attack and UI responsibilities from `PlayerInputController` | Existing mouse/keyboard behavior and combat bindings still work; clear extension points for capabilities |
| **P1.0B — Unit capabilities and AI roles** | Data-driven supported attacks and enemy behavior independent of `UnitType` identity | Existing units retain behavior; new archetypes can be configured without central hardcoded switches |
| **P1.1 — First expanded units** | One creative new playable mech and one new enemy archetype, fully integrated | Actual prefabs/data/visuals/animations/AI/scene encounter; playable owner acceptance |
| **P1.2 — Tactical UX and visual polish** | Better selection/targeting cues, previews, action availability, VFX, sprites and menus | Clearer more attractive playable combat, functional polished UI |
| **Later — Mission and RPG depth** | Additional unit sets, mission objectives, hangar/squad selection, equipment, progression | Scoped separately after the first P1 vertical slices succeed |

P1.1/P1.2 deliberately allow the development agent to propose original concepts and art direction within the established style. Use **small integrated vertical slices** instead of building extensive abstract infrastructure with no playable result. Major changes to game-wide rules or campaign direction remain owner decisions.

## 6. Workflow and verification expectations

- The owner sends each scoped implementation brief **in the Codex desktop conversation**; never add task-prompt Markdown files to the repository.
- Work on `New-features-test` in-place with the Unity bridge. Do not commit/push unless asked.
- **Minimum agent validation:** Unity compile, Console check, critical asset/reference sanity check, and a short focused Play Mode smoke test when economical/available. The **owner manually plays and accepts the features**; no exhaustive regression campaign is expected on every iteration.
- Report limitations and untested behavior instead of claiming coverage. Existing Unity Test Framework support can be used for high-value focused checks, but an extensive automated test harness is not a P1 prerequisite.
- Keep this document a **current technical reference**. When features change, revise the relevant sections, remove outdated limitations, and update the roadmap status. Do **not** append dated stabilization logs, full task prompts, or stale issue lists.
