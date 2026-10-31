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
| Enemy AI | `Assets/Scripts/AI/EnemyTurnController.cs` | Enemy action sequencing and current melee/artillery logic; P1.0B extension point |
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

### Unit definitions and combat capabilities — P1.0B

`UnitStats` currently contains identity (`UnitName`, `UnitType`, `UnitIcon`), base HP/move range, melee/ranged/area attack damage/ranges and card display data. It does **not** encode a unit's supported commands or reusable attack/ability profiles.

`PlayerCommandState` / `PlayerAttackExecutor` need a configuration source for legal commands, ranges and execution choices; **do not** expose unsupported attacks simply because a unit has an unused attack allowance. Backward-compatibility defaults must preserve existing playable units until migrated deliberately.

### Enemy tactical role selection — P1.0B

`EnemyTurnController` currently branches on `UnitType.InsectArtillery` to choose area AI; others use melee behavior. Define combat-role/AI profiles independently of identity so a new ranged skirmisher, defensive or support enemy can be configured without growing type-specific conditionals. Preserve existing movement, attack timing and enemy-turn completion semantics.

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
| **P1.0B — Extensible unit/capability/AI architecture** | **Next implementation checkpoint** | Independent unit identity, supported actions and AI roles; preserve legacy units |
| P1.1 — Expanded playable mechs and enemies | Planned | New original unit archetypes with complete assets, attack logic, enemy behavior and owner-playable encounter; build toward 3 mech options |
| P1.2 — Tactical UX / combat presentation | Planned | Command clarity, targeting, functional UI, original sprites, shaders and VFX |
| P2.0 — Intro and hangar | Planned | Short illustrated intro, actual selectable squad, mission deployment |
| P2.1 — Progression | Planned | XP, levels, effective stats and evolving visual tiers, saved per mech |
| P2.2 — Mission expansion | Planned | Varied enemy sets/difficulty, maps, obstacle tiles and environments |
| P2.3 — Full polish | Planned | Options, UI cohesion, level visuals, VFX, post-processing and complete playable demo |

The approved **product intent and acceptance experience** are in `docs/GAME_VISION.md`. The present P1.0B task should establish *extension points*, not implement the entire planned feature set at once.

## 6. Working conventions

Work on `New-features-test` through the connected Unity Editor. The user provides tasks directly in desktop chat, integrates and manually tests gameplay. Agents make focused, project-local changes; minimally compile, check Console/references and perform a short Play Mode probe when feasible. **Do not commit/push without explicit instruction.** Update this file when implementation changes, replacing stale sections rather than appending chronological audit logs.
