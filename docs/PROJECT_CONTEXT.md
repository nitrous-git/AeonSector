# Aeon//Sector — Living Project Context

**Repository:** `nitrous-git/AeonSector`  
**Working branch:** `New-features-test`  
**Unity baseline:** `6000.6.7f1`  
**Source snapshot used for this refresh:** `e127f8b70f956514afdc7660261897d39ffa0c97` (2026-10-31)  
**Product direction:** [`GAME_VISION.md`](GAME_VISION.md)  
**Agent workflow:** [`../AGENTS.md`](../AGENTS.md)

This is the **current implemented architecture**, not a historical task log. P0 stabilization is assumed baseline unless a concrete issue emerges. Update this file when the project actually changes, and distinguish implemented, partly integrated and planned systems.

## 1. Current game and technical stack

Aeon//Sector is an isometric **2D, grid-based, turn-based tactical mecha game**, evolving into a compact, polished tactical RPG campaign. Current play involves player mechs versus insectoid enemies, with positioning, separate move/attack actions, melee/ranged/area attacks and turn-based AI.

- Unity `6000.6.7f1`, URP `17.6.0` / 2D Renderer, Tilemaps, Cinemachine `6.6.0`, uGUI and TextMesh Pro.
- Legacy `UnityEngine.Input` handles mouse/keyboard; an Input System migration is **not** a current prerequisite.
- Unity Test Framework `1.8.0` is available. No comprehensive project-owned gameplay regression suite is established.
- Use the Unity Editor bridge for in-project editing / asset integration. Owner conducts principal gameplay tests; agent performs minimal compile, Console and focused smoke checks.

## 2. Implemented runtime ownership

| Area | Key code | Present responsibility |
| --- | --- | --- |
| Battle lifecycle | `Assets/Scripts/Core/TurnManager.cs`, `BattleState.cs` | Register scene units, initialize factions, advance turns, resolve terminal states and result UI |
| Factions and rosters | `Assets/Scripts/Factions/Faction.cs`, `UnitManager.cs` | Membership, living-unit queries and per-faction turn setup |
| Unit runtime | `Assets/Scripts/Units/CombatUnit.cs` | Per-battle HP, grid coordinate, facing, moved/attacked flags, death and animation calls |
| Unit configuration | `Assets/Scripts/Units/UnitStats.cs`, `Assets/Scripts/Units/SO_UnitStats/` | ScriptableObject identity, base stats, attack damage/ranges and card presentation fields |
| Board and terrain occupancy | `Assets/Scripts/Grid/TilemapBoardAdapter.cs` | Walkability, occupancy, grid/world conversion and tile highlighting |
| Paths / ranges | `Assets/Scripts/Pathfinding/` | BFS reachable-cell queries, A* routing, attack-range calculations |
| Input façade | `Assets/Scripts/Input/PlayerInputController.cs` | Scene-facing MonoBehaviour, legacy input handling, callbacks and coroutine host |
| Selection/commands | `Assets/Scripts/Input/PlayerCommandState.cs` | Selected unit, command mode, valid cells, resolution lock |
| Movement executor | `Assets/Scripts/Input/PlayerMovementExecutor.cs` | Route validation, destination reservation, movement animation and completion |
| Attack executor | `Assets/Scripts/Input/PlayerAttackExecutor.cs` | Melee/ranged attack resolution, projectile/animation callbacks, damage-once guards |
| Battle UI coordinator | `Assets/Scripts/Input/PlayerBattleUI.cs` | Cards, menu refresh, selection/path/attack overlays; presentation only |
| Enemy AI | `Assets/Scripts/AI/EnemyTurnController.cs` | Configured assault/skirmisher/bombardment execution, with separate tactical planner |
| Unit animation | `Assets/Scripts/Units/UnitMover.cs`, `UnitVisualAnimator.cs` | Movement, facing, attacks and existing animation-event forwarding |
| Menus/results | `Assets/Scripts/UI/` | Main menu, action menu, unit cards, turn banner, results |
| Effects/audio | `Assets/Scripts/Projectiles/`, `Assets/Scripts/Music_SFX/`, other relevant asset folders | Projectiles, combat feedback and SFX |

### Player controller refactor — implemented

P1.0A is in the branch: `PlayerInputController` delegates to four plain C# collaborators created at runtime, without new MonoBehaviour references requiring scene rewiring. Keep existing selection and animation-event callbacks compatible. Preserve the single authoritative selected-unit/command state and the shared movement/attack resolution lock.

Source/agent-recorded verification: Unity compilation with zero errors; focused Scene01 movement/selection/locking smoke and callback/reference inspection. **Owner verification of physical input, melee/ranged damage and full battle results is not yet established here.** Do not reopen the refactor unprompted.

### Stable combat rules and state

- Valid scene units register only after a successful walkable, unoccupied board placement.
- Player movement uses BFS reachability and A* routing; rejected reservation/route does not consume a move.
- **One move plus one attack each turn**, independently consumable. No generic action-point resource exists.
- Melee and ranged player attacks and melee/artillery enemy attacks exist; ranged sequencing tracks fire, projectile impact, and animation completion with duplicate-damage safeguards.
- Unit death must release occupancy and faction membership; victory/defeat prevent further action execution.
- Result SFX and banner repeat-duration issues were addressed; turn UI reports actual current turns.
- `CombatUnit.GetEnergyPercent()` derives EN presentation from HP (70%) and unused move/attack allowances (15% each). **EN is not real spendable energy.**
- `UnitStats` is a shared ScriptableObject; future persistent XP/level overrides must be in independent per-owned-mech data and resolved combat stats, not written back into base definitions.

## 3. Existing content and scenes

| Enabled scene | Role | Current serialized roster |
| --- | --- | --- |
| `Assets/Scenes/MainMenu.unity` | Menu entry / Play | N/A |
| `Assets/Scenes/BattleTestScene01.unity` | Opening battle test | 1 player / 3 enemies |
| `Assets/Scenes/BattleTestScene04.unity` | Follow-up test | 2 players / 6 enemies |
| `Assets/Scenes/BattleTestScene06.unity` | Later test | 2 players / 6 enemies |

Currently configured navigation: MainMenu → Scene01 → Scene04 → Scene06. Scene06's Yes route reloads itself. Result buttons can appear on win and loss; future campaign UX should explicitly distinguish next mission, retry, and return rather than silently assuming the current Yes/No behavior is finished. Full UI-driven scene transition validation was not established in the last audit.

Existing assets include hybrid, melee and ranged player-mech prefabs/stat definitions; insect melee/artillery enemy types; direction-aware animations; projectile/slash effects; 2D isometric tiles and battle UI. **One hybrid mech is the established integrated playable choice.** Existing melee/ranged legacy definitions were migrated but their icons and `DisplayMaxHP`/`DisplayMaxEN` presentation fields remain incomplete; do not equate existence of a prefab with a finished selectable archetype.

There is **no implemented hangar/squad picker, campaign mission definition, persistent XP/levels, save/progression loop, visual power-tier system, or options/settings interface** as of this source snapshot. The menu's Options handler remains a placeholder.

## 4. Next extension points and current constraints

### Unit definitions and combat capabilities � implemented P1.0B

`UnitStats.SupportedAttacks` is an explicit `AttackCapabilities` flags field (Melee, Ranged, Area); None is safe and grants no attacks. Existing damage/range fields remain authoritative. `Supports(CommandMode)` rejects unsupported or invalid damage/range configuration, with Editor warnings for invalid definitions. Capabilities are independent of `UnitType`. Command state, action-menu buttons, range queries and the player attack executor all enforce permission. The executor rechecks permission before damage. Existing ranged animation callbacks/context and separate movement/attack allowances remain intact. Area is currently an enemy command; no new player area button or ability framework is added.

Migrated assets under `Assets/Scripts/Units/SO_UnitStats/`: Hybrid = melee+ranged, Melee = melee, Range = ranged, Insect_Melee = melee/Assault, Insect_Artillery = area/Bombardment. Numeric balance and GUIDs are preserved. Legacy Melee/Range prefabs now have a `UnitVisualAnimator` bridge using their existing controller (which contains the expected directional melee/ranged states); icons/card display values remain unfinished. `BattleDamage` provides the shared HP/death gateway; `CombatUnit` still owns runtime HP/action state and `TurnManager` owns removal/results.

### Enemy tactical roles � implemented P1.0B

`UnitStats.EnemyRole` selects None, Assault, Skirmisher or Bombardment independently of identity. None/incompatible roles safely skip with a useful runtime warning. The plain `EnemyTacticalPlanner` chooses paths and targets deterministically; it never changes occupancy or damage. `EnemyTurnController` sequences previews, movement and attacks. Assault approaches and uses melee; Bombardment preserves area-first opportunities, targeting and staggered projectiles; Skirmisher prefers reachable cardinal firing solutions over adjacency, respecting terrain/occupancy blockers, and uses an independent projectile sequence rather than player animation events. Hypothetical range queries ignore only the moving attacker's vacated cell. Stable cell ordering breaks ties.

`SO_Insect_Skirmisher.asset` and `Assets/Prefab/EnemyInsect/Enemy_InsectSkirmisher.prefab` are a compatibility example using existing insect visuals/base numbers and a configured projectile; no scene roster was changed. `UnitStats.RangedProjectilePrefab` configures its projectile presentation. New enemy roles must support their required attack; missing projectile presentation still resolves a legal attack safely.

Author an enemy by creating UnitStats, configuring flags/ranges/role, assigning it to a compatible CombatUnit prefab, and wiring animation/mover/projectile references. Place it on valid ground and register it in the existing scene roster. Do not add UnitType branches.

### Playable catalog and authoring

`PlayableMechCatalog` stores serializable archetype entries with a stable unique ID, shared UnitStats and matching CombatUnit prefab. `SO_UnitStats/PlayableMechs.asset` lists hybrid, assault and marksman using the three existing mech prefabs. A future Hangar enumerates this catalog; runtime/persistent owned-mech state must be separate. Catalog validation warns on duplicate/missing IDs and mismatched references.

To add a playable archetype: author UnitStats with explicit supported attacks, assign those stats to a compatible prefab, add its unique ID/stats/prefab entry to the catalog, and validate commands and animation events. This does not make legacy art/presentation production-ready. Future effective-stat/progression calculation belongs between shared base definitions and runtime combat reads; never mutate shared assets when leveling individuals. Presentation tiers must not determine attack legality.

Future deployment must validate starting cells through the authoritative board, instantiate selected catalog prefabs and populate the TurnManager scene roster **before its Start initialization**. Let existing initialization perform board placement/registration exactly once; do not pre-register occupancy or introduce a competing mission/roster authority. Deployment, XP and saving are not implemented here.

Focused validation: Unity compile and Console checks; controlled Scene01 Play Mode probe verified all three mech command permissions, independent executor rejection without action cost, catalog contracts, and one assault/area/ranged enemy attack each with exact damage and allowance consumption, without unexpected turn handoff. Ranged placement planner was queried; full autonomous positioning/movement, physical input, death/results and all-scenes regression remain owner tests. Temporary smoke setup repositioned units only in Play Mode; no scene changes were saved.

### Dynamic squad selection and missions — future P2

`TurnManager` presently registers **scene-assigned units** at `Start()`. Before introducing a hangar, design a **mission/deployment contract**: chosen squad (persistent mech identities and progression), validated spawn/deployment cells, enemy spawn definitions, mission identifier, completion/result transition, and a default path for old test scenes. Do not introduce competing roster or grid authorities.

### Visual identity and progression — future P2

Levels must affect **effective combat values** and optionally unlock **presentation tiers** (shader parameters, trails, blade/projectile effects, alternate sprites). Keep persistent ownership/XP state separate from scene CombatUnits and shared ScriptableObject definitions. Make variants data-driven; avoid one prefab copy for every mech level.

### Environment and rendering — P1.2 / P2

New isometric tiles, structures, mountains, debris and blocking props must agree with board walkability, reservation and pathfinding. Use correct sprite sorting/occlusion, performant URP 2D lighting and judicious post-processing; tactical readability takes precedence over visual noise.

### Small known constraints — address when relevant

- `CombatUnit` health bar lookup still depends on a child index, fragile when creating diverse new prefabs; replace with robust serialized/component lookup when authoring new units.
- A stalled projectile coroutine can still prevent ranged-attack completion despite animation-event fallbacks; handle if extending combat resolution, without turning every task into a stabilization audit.
- The final mission/result menu semantics need intentional UX when the campaign UI is built.
- Existing project does not yet have broad automated gameplay test coverage; do not interpret minimal smoke tests as exhaustive proof.

## 5. Active development milestones

| Phase | Status | Outcome |
| --- | --- | --- |
| P1.0A — Player controller refactor | **Implemented in source**; owner acceptance may remain | Input, selection, movement, attacks and UI have distinct ownership |
| **P1.0B — Extensible unit/capability/AI architecture** | **Implemented; owner gameplay acceptance remains** | Independent unit identity, supported actions and AI roles; preserve legacy units |
| P1.1 — Expanded playable mechs and enemies | Planned | New original unit archetypes with complete assets, attack logic, enemy behavior and owner-playable encounter; build toward 3 mech options |
| P1.2 — Tactical UX / combat presentation | Planned | Command clarity, targeting, functional UI, original sprites, shaders and VFX |
| P2.0 — Intro and hangar | Planned | Short illustrated intro, actual selectable squad, mission deployment |
| P2.1 — Progression | Planned | XP, levels, effective stats and evolving visual tiers, saved per mech |
| P2.2 — Mission expansion | Planned | Varied enemy sets/difficulty, maps, obstacle tiles and environments |
| P2.3 — Full polish | Planned | Options, UI cohesion, level visuals, VFX, post-processing and complete playable demo |

The approved **product intent and acceptance experience** are in `docs/GAME_VISION.md`. The present P1.0B task should establish *extension points*, not implement the entire planned feature set at once.

## 6. Working conventions

Work on `New-features-test` through the connected Unity Editor. The user provides tasks directly in desktop chat, integrates and manually tests gameplay. Agents make focused, project-local changes; minimally compile, check Console/references and perform a short Play Mode probe when feasible. **Do not commit/push without explicit instruction.** Update this file when implementation changes, replacing stale sections rather than appending chronological audit logs.
