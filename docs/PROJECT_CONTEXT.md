# Aeon//Sector — Project Context & Technical Baseline

**Audit date:** 2026-10-31  
**Repository:** https://github.com/nitrous-git/AeonSector  
**Working branch:** `New-features-test`  
**Baseline commit:** `3f2551af0e883fdcd24581fd20eb15e42e722e88`  
**Verified Unity version:** `6000.6.7f1`

> This is a read-only GitHub source/serialized-asset audit. Gameplay and scene behavior have not been verified in Play Mode during this audit. The Codex agent should validate source findings through the actual Unity Editor before treating them as reproduced defects.

## Game identity and scope

Aeon//Sector is currently a **2D isometric, grid-based, turn-based tactical combat prototype**, with futuristic mecha player units and insectoid opposition. It is intended to grow into a more complete tactical mecha RPG. It is **not** currently a real-time strategy game and **does not yet have** a complete persistent RPG progression layer.

Design principles for future work: readable battlefield state, explicit action cost, interesting positional decisions, distinctive unit roles, responsive combat feedback, coherent visual identity, and maintainable/data-driven content. Cover, terrain modifiers, loadouts, salvage, pilot progression, mission objectives and campaign persistence are potential future mechanics, not already-implemented features.

## Technical environment

- Unity `6000.6.7f1`, URP `17.6.0`, 2D Renderer, Tilemaps, Cinemachine `6.6.0`, uGUI `2.6.0`, TextMesh Pro.
- Legacy `UnityEngine.Input` mouse/keyboard calls are in active gameplay scripts; do not migrate input wholesale during stabilization.
- Unity Test Framework dependency present (`1.8.0`). No project-owned gameplay tests were identified in the audited source tree.
- Unity Editor bridge was previously integration-tested using `Assets/AgentTests/AgentIntegrationTest.unity` and a test cube/prefab/material. The previous integration test did not enter Play Mode. These isolated test assets should not be promoted into the game or deleted without direction.
- Git LFS rules are configured for image/audio/model binaries; maintain `.meta` assets and GUID stability.

## Runtime architecture

| Area | Primary files | Existing responsibility |
| --- | --- | --- |
| Battle flow | `Assets/Scripts/Core/TurnManager.cs`, `BattleState.cs` | Initialize factions, register units, alternate turns, handle victory/defeat |
| Factions | `Assets/Scripts/Factions/Faction.cs`, `UnitManager.cs` | Unit collections, living-unit queries, turn action resets |
| Unit state | `Assets/Scripts/Units/CombatUnit.cs` | Runtime HP, `HasMoved`, `HasAttacked`, `GridPosition`, facing, damage |
| Unit definitions | `Assets/Scripts/Units/UnitStats.cs`, `SO_UnitStats/` | ScriptableObject stats: HP, move range, melee/ranged/area attacks, UI values |
| Grid | `Assets/Scripts/Grid/TilemapBoardAdapter.cs`, `GridCoord.cs` | Tilemap-derived walkability, occupancy, world/grid mapping, tactical overlays |
| Paths | `Assets/Scripts/Pathfinding/` | BFS reachable tiles, orthogonal A* pathfinding, attack-range generation |
| Player flow | `Assets/Scripts/Input/PlayerInputController.cs`, `CommandMode.cs` | Unit selection, commands, movement, melee/ranged attack sequencing |
| Enemy flow | `Assets/Scripts/AI/EnemyTurnController.cs` | Turn coroutine, movement toward players, melee or artillery targeting |
| Motion/visuals | `Assets/Scripts/Units/UnitMover.cs`, `UnitVisualAnimator.cs` | Path animation and four-direction isometric visual mapping |
| UI | `Assets/Scripts/UI/` | Unit cards, action buttons, turn banners, battle end, menu |
| Effects/audio | `Assets/Scripts/Projectiles/ProjectileMover.cs`, `Assets/Scripts/Music_SFX/`, `Assets/Scripts/Camera/` | Projectile animations, SFX/music, camera impulses |

### Gameplay state contracts

- **Movement:** Reachable tiles are calculated with BFS; paths with A*; board occupancy is authoritative. A successful move reserves its destination using `TryMoveUnit` and then animates the visual transform through `UnitMover`. Rejected moves must not mutate logical position or consume the move.
- **Turn allowance:** `CombatUnit.BeginTurn()` resets `HasMoved` and `HasAttacked`. Movement and attack are separate per-turn allowances, not a pooled action-point system.
- **Attack range:** melee uses Manhattan/diamond cells, ranged uses cardinal-line scans blocked by terrain/units, enemy artillery uses a BFS area range. Verify semantics and impact behavior before extending to new abilities.
- **Damage:** `CombatUnit.TakeDamage(int)` reduces runtime HP, updates the bar, triggers death state; `TurnManager.RemoveUnitFromBattle` removes grid occupancy and faction membership before destroying the GameObject.
- **Energy UI:** `GetEnergyPercent` combines 70% HP fraction, 15% ability to move and 15% ability to attack. This is a **derived presentation score, not a spendable energy pool**. Do not use it as ability resource capacity without explicit redesign.
- **Facing:** `UnitFacing` has four isometric directions; `UnitVisualAnimator` chooses front/back animations and mirrors sprites.

## Scene and content inventory

**Build Settings enabled scenes (order):**
1. `Assets/Scenes/MainMenu.unity`
2. `Assets/Scenes/BattleTestScene01.unity`
3. `Assets/Scenes/BattleTestScene04.unity`
4. `Assets/Scenes/BattleTestScene06.unity`

Other existing scenes include `BattleTestScene.unity`, `BattleTestScene05.unity`, `DebugScene.unity`, and the isolated `Assets/AgentTests/AgentIntegrationTest.unity`.

Serialized `TurnManager` scene lists show:
- BattleTestScene01: 1 player unit, 3 enemies.
- BattleTestScene04: 2 player units, 6 enemies.
- BattleTestScene06: 2 player units, 6 enemies.

`MainMenu` sets `battleSceneName` to `BattleTestScene01`. BattleTestScene04's end-battle next-scene override points to `BattleTestScene06`; BattleTestScene06's next-scene override points to itself. Treat further campaign routing as an approved design decision, not a guess.

Relevant prefab families: `Assets/Prefab/PlayerMech/` (hybrid, melee, range), `Assets/Prefab/EnemyInsect/` (insect, bee, artillery), `Assets/Prefab/Projectiles/`, `Assets/Prefab/SwordSlashes/`, `Assets/Prefab/UI/`. Existing animation controllers, sprite sheets, sound effects, music, tile sets and UI fonts are available for reuse.

## Audit findings / stabilization backlog

These are **source-observed risks**, to be runtime-verified and fixed in narrow phases:

**P0.1 — Board registration and rejected movement**
- `TurnManager.RegisterSceneUnits` calls `TryPlaceUnit` but continues to `Initialize`, register and reposition a unit when placement fails. This may create a faction-roster/occupancy mismatch. Treat duplicate/spawn-on-blocked/out-of-bounds cases explicitly.
- `PlayerInputController.ResolveUnitMove` logs failed `board.TryMoveUnit` but continues visual movement and `MarkMoved`; the `UnitMover` reference is not null-guarded. Never animate or consume movement after a rejected board reservation.

**P0.2 — Attack sequencing and battle-end behavior**
- Player ranged attack uses animation-event callbacks and a projectile coroutine. Investigate whether `AnimEvent_RangedAttackFinished` can finish an attack before projectile damage resolves, as well as absent-animation-event cases.
- `TurnManager.CheckBattleEnd` invokes victory sound on defeat and defeat sound on victory; `ShowEndBattleUIAfterBanner` also plays victory sound unconditionally.
- Review turn-state changes around victory/defeat and pending coroutines to avoid post-battle commands.

**P0.3 — Unit definitions and scene robustness**
- `SO_Mech_Melee.asset` and `SO_Mech_Range.asset` still serialize older `AttackDamage`/`MinAttackRange`/`MaxAttackRange` fields instead of the current `Melee*`/`Ranged*` schema. Confirm whether they are active before editing; migrate safely with tests and no stat loss.
- `CombatUnit.Awake` retrieves a health bar from a hard-coded child index; prefer an explicit serialized reference or safe component discovery with validation.
- `BattleBannerUI.Show` doubles its hold duration whenever a victory/defeat banner is shown, mutating shared timing state.
- `BattleStatsTracker` displays a hardcoded 10-turn maximum, which may not represent a real mission limit.
- `MainMenuUI.Options` is currently a placeholder.
- Verify existing scene and prefab wiring, especially animation event references, `EndBattleUI` transitions, static managers and null-sensitive references.

## Development progression (proposed, subject to owner approval)

| Phase | Intent | Definition of done |
| --- | --- | --- |
| P0 | Stabilize existing combat, scenes, data and tests | No regressions in the current playable loop; meaningful automated/smoke checks |
| P1 | Combat feedback and interaction polish | Clear target/attack previews, responsive UI and satisfying game feel |
| P2 | More tactical depth | Validated unit roles, new mechanics and stronger configurable enemy behaviors |
| P3 | Missions and encounter content | Reusable objectives, varied maps, staged missions, encounter configuration |
| P4 | Mecha RPG layer | Approved progression, loadouts, equipment and between-mission gameplay |

Do not treat this roadmap as permission to implement all features at once. Scope each agent task, obtain approvals for major design decisions, and update this file after verified implementation.

## Task delivery convention

Individual Codex implementation briefs are delivered in the **ChatGPT desktop conversation**, not stored as task Markdown files in the repository. `AGENTS.md` is the permanent agent policy and this document is long-lived technical context. Only record verified, lasting facts and checkpoint outcomes here; avoid accumulating full task prompts.

## Development checkpoint tracking

- **2026-10-31 GitHub source audit:** complete; no Editor Play Mode validation in this audit.
- **Unity bridge:** previous isolated scene/prefab/material integration test reported successful source and Editor mutations and compilation; Play Mode not tested.
- **Current stabilization backlog:** start with P0.1 board registration and rejected-movement invariants; no runtime results recorded. The active task itself is supplied separately in the desktop conversation.
