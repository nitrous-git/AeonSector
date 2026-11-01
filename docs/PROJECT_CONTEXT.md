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

Existing assets include hybrid, melee and ranged player-mech prefabs/stat definitions; insect melee/artillery enemy types; direction-aware animations; projectile/slash effects; 2D isometric tiles and battle UI. Hybrid, Assault and Marksman are integrated in the isolated P1 showcase. Assault, Marksman and Scorpion Skirmisher use the curated P1 artwork and matching portraits; Hybrid, Melee Drone and Artillery retain their existing assets.

There is **no implemented hangar/squad picker, campaign mission definition, persistent XP/levels, save/progression loop, visual power-tier system, or options/settings interface** as of this source snapshot. MainMenu Options now opens a master-volume slider and Close panel; volume is session-only and is not saved.

### P1.1A playable combat showcase

`Assets/Scenes/BattleTestScene_P1_Showcase.unity` is an isolated copy of Scene01 with the existing board, camera, HUD, input and turn systems. It is appended to enabled build scenes for direct opening/replay; existing campaign scenes/menu routes are unchanged. Its own result Yes replays the showcase, No returns to MainMenu.

Allies: Hybrid at (9,3), Assault at (9,5), Marksman at (10,2). Enemies: two melee drones at (7,5)/(6,2), two skirmishers at (7,2)/(5,4), one artillery at (5,3). All eight cells are walkable and distinct. The opening Marksman lane targets the skirmisher at (7,2); Assault can move to (8,5) and melee the adjacent drone. Hybrid has a ranged lane to artillery and movement access toward melee targets. Existing stats, capability flags and catalog IDs are unchanged.

Six active prefab families (three mechs, InsectBee drone, skirmisher, artillery) have serialized health Slider/visual references, colliders and movers. CombatUnit falls back to an inactive-inclusive child Slider lookup for old scenes, warns when absent, and initializes slider min/max/current value in Initialize. Damage updates the same slider. Assault/Marksman retain their existing HP canvas and display HP=150 / EN=80. Scorpion HP bars sit above the raised tail. P1.1C replaces the three target units' presentation, preserving stats, capabilities, stable identities and Skirmisher AI.

UnitVisualAnimator keeps existing player events and only forwards them for player-owned units. Generic enemy attacks use available Attack states, falling back to Idle for the existing static artillery controller rather than requesting a missing state. Enemy projectile timing/execution remains owned by EnemyTurnController. Movement still uses directional idle presentation; P1.2A adds procedural hit/death feedback without dedicated sprite animation clips.

P1.1A owner-validated gameplay remains the integration baseline. P1.1C verification focuses on compile/Console, imported assets, directional wiring, portraits, ranged synchronization and a short combat smoke. Full campaign and terminal battle flows were not retested in P1.1C.

### P1.1C curated sprite and animation integration - implemented

- The 15 approved PNGs are preserved byte-for-byte under `Assets/Sprites/AeonSector_P1/{Assault,Marksman,Scorpion}/`. Twelve sheets provide 72 frames, top row first, left to right. Import baseline: Sprite Multiple (portraits Single), PPU 256, Point, Clamp, uncompressed, no mipmaps, sRGB/alpha transparency, Full Rect, max size 4096. Humanoid frames: 896x896, pivot (0.5, 0.1741); Scorpion: 1024x1024, pivot (0.5, 0.09).
- Dedicated controllers and four clips each live in `Assets/Animation/AnimationController/P1_Assault/`, `P1_Marksman/` and `P1_Scorpion/`, assigned respectively to `Mech_Melee`, `Mech_Range` and `Enemy_InsectSkirmisher`. Hybrid's controller is unchanged. Four-frame idles loop in one second. Eight-frame Assault melee takes 1.2 seconds, preserving existing melee damage timing. Eight-frame Marksman/Scorpion attacks take one second and do not loop. Existing combat executors restore directional idle after resolution.
- `UnitVisualAnimator` holds optional per-prefab ranged release delay, recovery duration and front/back projectile-origin attachments. `CombatUnit` forwards this presentation data; grid positions and damage ownership are unchanged. Visual-child scales: both mechs 0.5, Scorpion 0.38. Rendering uses white tint, existing sorting layers/materials and pivot sorting. Associated UnitStats assets use the new portraits; only the Skirmisher display name changes to Scorpion Skirmisher.
- Both Marksman ranged clips contain one `AnimEvent_FireRangedProjectile` at 0.5 seconds (frame 5) and one `AnimEvent_RangedAttackFinished` at 0.99 seconds. Its missing-fire fallback waits 0.6 seconds (release plus 0.1-second grace); legacy/Hybrid prefabs retain 0.25 seconds. Player completion requires projectile resolution and animation completion. The five-second deadline now also stops/removes a stalled projectile and resolves damage through the existing at-most-once guard.
- `EnemyTurnController` retains Skirmisher ranged release/damage ownership. Only its ranged path consumes the configured release delay (Scorpion: 0.75 seconds, frame 7) and remaining one-second recovery. Mirrored front/back stinger attachments provide the origin. Enemy clips have no player callbacks; melee/artillery timing and projectile behavior are unchanged.
- Verification: Unity compilation passed. All PNG hashes, settings, slices, pivots, clip references/events and all four facing/mirror combinations passed bridge checks. Focused showcase smoke exercised Marksman/Hybrid ranged, Assault move/melee and one mixed enemy turn back to player; damage matched configured values. Marksman release was observed at 0.505 seconds on frame 5. A missing-fire/stalled-projectile probe kept input locked despite an early finish callback, resolved one hit at approximately five seconds and removed the effect. Game-view screenshots were inspected for scale, ground alignment, portraits and HP bars. Temporary harness errors were corrected; final validation had no runtime Console errors. Expected fallback warnings occurred only in the fault probe.
- Current limits: movement uses directional idle; dedicated walk/hit/death artwork is absent. The showcase now uses the compact P1.2A effects; legacy scenes retain their old projectile/slash assets. Full physical-input acceptance, victory/defeat, death cleanup, campaign navigation and balance remain owner-side checks for this checkpoint.

### Combat presentation - implemented P1.2A

- The showcase instantiates `Assets/CombatPresentation/Prefabs/CombatPresentation.prefab`. Its optional `CombatPresentation` scene component reads six `CombatVfxProfile` assets (Hybrid, Assault, Marksman, Scorpion, Artillery, Drone). Profiles hold color, impact size, camera response, melee contact/recovery and projectile references; UnitStats still owns all combat values and permissions.
- `UnitHitFeedback` is wired on the six showcase unit prefab families. It flashes a sprite silhouette and recoils only the art child for 0.18 seconds, preserving facing, collider, HP UI and board position. Two small URP shaders in `Assets/Shaders/` support outlined energy geometry and sprite-alpha flashes; no package was added.
- `CombatEffect` animates reusable charge, discharge, slash, contact burst, ground shockwave, warning and destruction prefabs. Four projectile prefabs use short trails and dark-edged energy cores. Hybrid uses cyan pulses; Assault has broader ice-blue arcs and stronger contact; Marksman has faster, narrow blue-white lances; Scorpion uses crimson stinger bolts; Artillery uses orange-red shells, cell warning rings and restrained ground bursts. Effects expire within 0.16-0.75 seconds; spark systems cap at 32 particles each.
- `BattleDamage.Apply` accepts optional attacker/command presentation context and invokes feedback after HP changes, before existing removal. `FloatingDamageNumber` shows the actual displayed-HP delta (matching the cards' scaling/rounding), fits to a fixed world height, and expires after 0.85 seconds. Lethal hits add a short DESTROYED label, a detached fading silhouette and spark collapse; occupancy/faction removal remains immediate. The enemy card hides on death.
- Player melee contact occurs at 0.6 seconds (Assault frame 5), with 0.6 seconds recovery and the input lock retained. Drone melee uses 0.7 seconds contact + 0.8 recovery. `ProjectileMover.FlyAndHit` accepts an optional arrival callback: damage/flash/text occur at actual arrival, not after the old 0.5-second hit animation. New projectiles have 0.09-second visual recovery. Marksman release/finish events (0.5/0.99 seconds), Scorpion release (0.75 seconds), player at-most-once guard and five-second fallback remain intact. Artillery gets 0.22 seconds launch anticipation; area coverage and cell stagger are unchanged.
- `CameraShakeImpulse.PlayImpact` clamps short impulses, coalesces nearby impacts and applies only the extra amplitude for stronger overlapping hits. Profiles use 0.035-0.14 force / 0.1-0.2 seconds; the showcase disables the previous long secondary noise tail. `CombatPresentation.shakeMultiplier` can reduce or disable feedback. `CombatHudStabilizer` cancels Cinemachine positional corrections on the existing GameUI, banner and result roots, keeping the world-space HUD steady; unit health bars remain attached to their units. No hit-stop or time-scale modification is used.
- Validation: compilation, shader/reference checks and staged showcase combat passed, including exact damage, duplicate-fire guards, recovery locks, lethal occupancy cleanup, effects on both factions and enemy-to-player turn return. The camera probe measured under 0.5 pixels of HUD drift. Test staging was Play Mode only. Full physical-input, all facings, victory/defeat and performance acceptance remain owner checks; dedicated walk/hit/death art and new audio remain absent.

### Showcase readiness fixes

The showcase fits unit-card names on one line, uses shorter display names without temporary-art suffixes, replaces baked MP costs with actual command shortcuts, and displays accurate controls/whole-squad end-turn instructions. Replay prompt says REPLAY SHOWCASE rather than next battle. Decorative HUD graphics do not intercept pointer rays; interactive button targets remain enabled. PlayerInputController ignores world pointing/clicks over interactive UI, preventing UI clicks from also committing movement/attacks. MainMenu has a modal audio panel with a master-volume slider and Close; existing Play navigation is preserved. Font fitting and HUD were visually checked at 1920x1080; the overlay options panel was checked at 1280x720. No balance values were changed. Full test-game automation used normal command buttons/executors with temporarily accelerated time, not direct damage or stat edits; the automated player lost with two enemies surviving, so encounter difficulty is not considered balance-certified.

## 4. Next extension points and current constraints

### Unit definitions and combat capabilities � implemented P1.0B

`UnitStats.SupportedAttacks` is an explicit `AttackCapabilities` flags field (Melee, Ranged, Area); None is safe and grants no attacks. Existing damage/range fields remain authoritative. `Supports(CommandMode)` rejects unsupported or invalid damage/range configuration, with Editor warnings for invalid definitions. Capabilities are independent of `UnitType`. Command state, action-menu buttons, range queries and the player attack executor all enforce permission. The executor rechecks permission before damage. Existing ranged animation callbacks/context and separate movement/attack allowances remain intact. Area is currently an enemy command; no new player area button or ability framework is added.

Migrated assets under `Assets/Scripts/Units/SO_UnitStats/`: Hybrid = melee+ranged, Melee = melee, Range = ranged, Insect_Melee = melee/Assault, Insect_Artillery = area/Bombardment. Numeric balance and GUIDs are preserved. Legacy Melee/Range prefabs now have a `UnitVisualAnimator` bridge using their existing controller (which contains the expected directional melee/ranged states); icons/card display values are completed with temporary shared presentation for the showcase. `BattleDamage` provides the shared HP/death gateway; `CombatUnit` still owns runtime HP/action state and `TurnManager` owns removal/results.

### Enemy tactical roles � implemented P1.0B

`UnitStats.EnemyRole` selects None, Assault, Skirmisher or Bombardment independently of identity. None/incompatible roles safely skip with a useful runtime warning. The plain `EnemyTacticalPlanner` chooses paths and targets deterministically; it never changes occupancy or damage. `EnemyTurnController` sequences previews, movement and attacks. Assault approaches and uses melee; Bombardment preserves area-first opportunities, targeting and staggered projectiles; Skirmisher prefers reachable cardinal firing solutions over adjacency, respecting terrain/occupancy blockers, and uses an independent projectile sequence rather than player animation events. Hypothetical range queries ignore only the moving attacker's vacated cell. Stable cell ordering breaks ties.

`SO_Insect_Skirmisher.asset` and `Assets/Prefab/EnemyInsect/Enemy_InsectSkirmisher.prefab` are a compatibility example using existing insect visuals/base numbers and a configured projectile; the P1 showcase now places two skirmishers; existing campaign rosters are unchanged. `UnitStats.RangedProjectilePrefab` configures its projectile presentation. New enemy roles must support their required attack; missing projectile presentation still resolves a legal attack safely.

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

- Unused legacy prefabs outside the six showcase unit prefabs may still need presentation wiring before use. Missing health sliders produce an explicit warning.
- Player ranged attacks have a five-second stalled-effect deadline. Enemy projectile behavior retains its existing coroutine implementation; broader effects/lifecycle work remains separately scoped.
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
