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

**P0.1 — Board registration and rejected movement (stabilized 2026-10-31)**
- `TurnManager.RegisterSceneUnits` now requires a ready board, valid unit stats, an in-bounds/base-walkable/unoccupied cell, and successful `TryPlaceUnit` before initialization or faction registration. Duplicate faction/roster membership is rejected.
- `PlayerInputController.CommitMove`/`ResolveUnitMove` now revalidate reachability and A* path data, require an idle `UnitMover`, reserve occupancy before animation, and clear/restore state on rejection or incomplete animation. Failed reservations do not mark the unit moved.

**P0.2 — Attack sequencing and battle-end behavior**
- Player ranged attacks now use a one-shot resolution context: repeated fire callbacks are ignored, damage is applied after the projectile routine resolves, and animation/projectile completion are both required before input is restored. A bounded fallback covers missing animation callbacks and missing projectile dependencies.
- `UnitVisualAnimator` resolves the scene `PlayerInputController` when its prefab reference is unset, which is required for the existing ranged animation events to reach gameplay code.
- Melee, artillery and unit removal paths guard invalid/dead targets, remove dead units from board occupancy and faction rosters once, and stop applying pending projectile damage after battle termination.
- `TurnManager` exposes an idempotent battle-ended state; victory/defeat transitions and delayed enemy-turn startup reject post-battle turn changes.
- `TurnManager.CheckBattleEnd` invokes victory sound on defeat and defeat sound on victory; `ShowEndBattleUIAfterBanner` also plays victory sound unconditionally.
- The sound-routing mismatch remains outside this checkpoint; P0.2 validation focused on combat state and resolution ordering.

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

### P0.3 implementation and content readiness (2026-10-31)

- Result audio routing is corrected in TurnManager; result-screen display no longer replays victory audio. Return navigation uses button-click audio. Audible verification remains Not tested.
- Banner durations use an immutable per-banner hold multiplier. A live Play Mode repeated-victory probe returned 4.7 seconds before and after repeated Show calls, with the turn duration unchanged at 2.9 seconds. Result-screen waiting uses the relevant banner duration.
- Turn display now shows the actual current turn without a fictitious/clamped 10-turn maximum. This does not impose or remove a gameplay turn limit.
- Unity confirmed MainMenu, BattleTestScene01, BattleTestScene04 and BattleTestScene06 are enabled. Serialized routing is MainMenu→01→04→06; battle No buttons route to MainMenu. Scene06 Yes routes to itself. The buttons use Yes/No graphics; intentional replay versus accidental progression remains ambiguous and was retained pending owner direction. Actual button-driven scene loading remains Not tested in P0.3.
- Legacy melee/ranged assets were migrated through Unity: both retain HP 10 and move 2; melee primary damage/range is 3/1–1, ranged primary damage/range is 3/1–4. Unspecified secondary stats retain existing schema defaults. Their prefab references remain intact; no direct scene references to these stats GUIDs were found. Hybrid and enemy stats were unchanged.
- P0.3 compilation passed and final Console reported zero errors/warnings. Full combat regression and automated tests were not rerun; prior P0.1/P0.2 observations remain historical evidence, not P0.3 passes. Stabilization acceptance is therefore still pending.

### Adding unit content

1. Create a UnitStats asset; configure identity/icon, HP/move and the supported attack damage/ranges. Display HP/EN must be configured deliberately; they are presentation values, not resources.
2. Duplicate a compatible prefab, assign its stats, CombatUnit, UnitMover, visual animator and health slider. Preserve animator state names and front/back directional clips; ranged clips require the existing callback receiver. Test both facing families.
3. Place it on a free walkable cell and add it once to the appropriate TurnManager scene roster. Initialization reserves occupancy before faction registration.
4. Verify selection, card/bar, movement, attack and death in Play Mode. Existing player melee/ranged logic is shared across definitions.

Content expansion limitations for P1: UnitType currently mixes identity with behavior selection; EnemyTurnController specifically treats InsectArtillery as area AI and all other enemies as melee. New ranged/hybrid enemy AI needs an explicit behavior-selection extension. Weapon/loadout assets and specialized abilities do not exist. Player attack availability is not a capability system: merely naming a unit melee/ranged does not disable its other command. Do not infer new balance or capabilities from enum names. Health-bar discovery still depends on prefab hierarchy, so validate it when authoring new prefabs.

- **2026-10-31 GitHub source audit:** complete; no Editor Play Mode validation in this audit.
- **Unity bridge:** previous isolated scene/prefab/material integration test reported successful source and Editor mutations and compilation; Play Mode not tested.
- **2026-10-31 P0.1 stabilization:** compiled cleanly and smoke-tested in Play Mode on BattleTestScene01 (1 player/3 enemies), BattleTestScene04 (2/6), and BattleTestScene06 (2/6). Registration samples matched board occupancy; valid movement, blocked movement, enemy turn handoff, melee resolution, and victory transition were exercised. No project-owned automated tests were available.
- **2026-10-31 P0.2 stabilization:** Unity compiled with zero Console errors. Live AnimationClip inspection confirmed three existing ranged fire events plus one finish event per ranged clip; the hybrid prefab had an unset `UnitVisualAnimator.playerInputController`. Play Mode probes verified one ranged damage application with control restored after resolution, enemy melee damage, artillery projectile staggering/cleanup, and idempotent victory/defeat turn guards in BattleTestScene01/04/06. Automated regression tests remain unavailable.
