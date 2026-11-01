# Aeon//Sector — Living Project Context

**Repository:** `nitrous-git/AeonSector`  
**Working branch:** `New-features-test`  
**Unity baseline:** `6000.6.7f1`  
**Verified source checkpoint for this documentation:** `ef5691263993e07d8cbb485e068769a62226a161` (2026-10-31; P1.2A source implementation)  
**Approved product direction:** [`GAME_VISION.md`](GAME_VISION.md)  
**Codex agent instructions:** [`../AGENTS.md`](../AGENTS.md)

This document describes **implemented** architecture and integration constraints. It is not a chronological work diary or permission to implement future systems. Stabilization and completed P1 foundational work are the baseline. **Next planned checkpoints: P1.2B Tactical Readability, then P1.2C HUD & UX Polish.** P1.2A has agent-recorded validation; owner visual/gameplay acceptance should be recorded separately when confirmed.

## 1. Game and Unity stack

Aeon//Sector is a **2D isometric, grid-based turn-based tactical mecha game**, currently a playable combat foundation and showcase evolving toward a compact campaign RPG. The working combat has player Hybrid/Assault/Marksman mechs versus insectoid mechanical enemy types (melee drones, ranged red Scorpion Skirmishers, bombardment artillery).

- **Unity:** `6000.6.7f1`, URP `17.6.0` with 2D Renderer, Tilemaps, Cinemachine `6.6.0`, uGUI/TextMesh Pro.
- **Input:** legacy `UnityEngine.Input` mouse/keyboard for now. No blanket Input System migration requirement.
- **Tests:** Unity Test Framework `1.8.0` available, but no broad game-owned regression suite. Agent uses focused compile/Console and Play Mode checks; owner performs main gameplay acceptance.
- **Unity workflow:** edit through the desktop Unity Editor bridge, preserve `.meta` IDs and serialized scene/prefab references. Do not commit/push without owner authorization.

## 2. Core runtime ownership

| Domain | Primary source | Responsibility |
| --- | --- | --- |
| Battle lifecycle | `Assets/Scripts/Core/TurnManager.cs`, `BattleState.cs` | Scene roster registration, faction turns, battle phases, victory/defeat/results |
| Factions | `Assets/Scripts/Factions/Faction.cs`, `UnitManager.cs` | Unit membership and living-unit queries |
| Unit runtime | `Assets/Scripts/Units/CombatUnit.cs` | Grid position, facing, battle-local HP, moved/attacked flags, death/animation interaction |
| Shared definitions | `Assets/Scripts/Units/UnitStats.cs`, `Assets/Scripts/Units/SO_UnitStats/` | Attack capabilities, base values, enemy role, UI identity and projectile defaults |
| Tactical board | `Assets/Scripts/Grid/TilemapBoardAdapter.cs` | Authoritative walkability, occupancy, board/world coordinates, tile overlays |
| Paths/ranges | `Assets/Scripts/Pathfinding/` | BFS reachability, A* routing and attack-range queries |
| Input/callback façade | `Assets/Scripts/Input/PlayerInputController.cs` | Existing mouse/keyboard, scene-facing animation events and coroutine host |
| Command authority | `Assets/Scripts/Input/PlayerCommandState.cs` | Selection, current command, valid cells, resolution lock |
| Movement | `Assets/Scripts/Input/PlayerMovementExecutor.cs` | Route validation, reservation, animation, action consumption |
| Attacks | `Assets/Scripts/Input/PlayerAttackExecutor.cs` | Melee/ranged sequencing, callbacks, projectile resolution and at-most-once damage |
| Battle UI | `Assets/Scripts/Input/PlayerBattleUI.cs` and `Assets/Scripts/UI/` | Selection/path/range visualization, cards, commands, banners, results |
| Enemy behavior | `Assets/Scripts/AI/EnemyTacticalPlanner.cs`, `EnemyTurnController.cs` | Assault, Skirmisher, Bombardment planning/execution |
| Animation/motion | `Assets/Scripts/Units/UnitVisualAnimator.cs`, `UnitMover.cs` | Two authored facing views, mirrored directions and motion |
| Damage/death | `Assets/Scripts/Core/BattleDamage.cs` | Authorized HP modifications and lethal cleanup gateway |
| Projectile motion | `Assets/Scripts/Projectiles/ProjectileMover.cs` | Travel and arrival callback for synchronized effects/damage |
| P1.2A effects | `Assets/Scripts/CombatPresentation/` | Presentation-only profile/effect, hit flash, damage text, camera/HUD feedback |
| Audio | `Assets/Scripts/Music_SFX/SFXManager.cs` | Existing UI/combat sound handling |

### Combat invariants

- **One move + one attack per unit turn**, each independently consumed. Invalid movement or rejected attacks must not spend an action.
- Movement uses BFS for available cells, A* for paths and destination reservation before motion. Valid units register only when placed on a walkable, free cell.
- `CombatUnit` owns battle-local HP and action flags; `UnitStats` ScriptableObjects are **shared base definitions**, never mutate them with per-player leveling or upgrades.
- `UnitStats.SupportedAttacks` flags gate melee/ranged/area commands independently from `UnitType`. UI, range and executor must agree. Area attack remains an enemy command; no general player area ability button is implemented.
- `UnitStats.EnemyRole`: None, Assault, Skirmisher or Bombardment; AI planning never performs occupancy/damage mutations itself.
- Attack callbacks, projectile arrival and recovery maintain at-most-once damage, valid/dead-target checks, input resolution locks and post-battle no-op behavior. HP, occupancy, faction membership and terminal state remain authoritative, not visual effects.
- `CombatUnit.GetEnergyPercent()` currently synthesizes **EN for display** from 70% HP and 15% each unused move/attack allowance. EN is **not a consumable energy pool** and cannot be used for an upgrade economy without new state design.
- Legacy scenes and Hybrid animation/event behavior remain supported. Do not recouple unit roles to identity or rebuild a monolithic `PlayerInputController`.

## 3. Implemented roster, archetype authoring and scenes

### Playable mechs and abilities

`PlayableMechCatalog` uses stable unique archetype IDs, matching `UnitStats` and `CombatUnit` prefabs. The configured catalog at `Assets/Scripts/Units/SO_UnitStats/PlayableMechs.asset` contains three entries:

| Mech | Allowed attacks | Existing presentation |
| --- | --- | --- |
| Hybrid | Melee + ranged | Existing Hybrid art/controller, coherent cyan VFX profile |
| Assault (`Mech_Melee`) | Melee | Curated heavy orange/white artwork, slash effects |
| Marksman (`Mech_Range`) | Ranged | Curated ivory/blue rifle artwork, precision projectile |

The initial three playable archetypes are **available in data and the showcase**. They are **not yet player-selectable through a persistent Hangar**.

To author a new playable mech, create a shared base `UnitStats` definition, compatible prefab and animation controller, configure supported commands and add a unique catalog entry. Do not confuse archetype definitions with a player's owned instance, upgrade levels or final loadout.

### Enemy roles

- Melee Drone / InsectBee: short-range Assault pursuer, existing insect art.
- Scorpion Skirmisher: ranged positional AI, crimson curated enemy art, stinger projectile, `EnemyCombatRole.Skirmisher`.
- Insect Artillery: Bombardment area attacks, staggered target cells and warning presentation.

The `EnemyTacticalPlanner` uses deterministic reachable firing positions and stable tie-breaking; its range calculations respect blockers and occupancy and ignore a moving attacker's vacated cell when appropriate. `EnemyTurnController` owns enemy action sequencing and projectile release; the Scorpion does **not** depend on the player's ranged animation callbacks. Missing/incompatible roles or capabilities must fail safely and log a useful warning.

To author a new enemy, create a `UnitStats` with role/capability/range values, a correctly configured prefab with animator/mover/projectile, place on walkable ground and register through existing roster initialization. Avoid `UnitType` branches.

### Scenes and current scene flow

| Scene | Present use |
| --- | --- |
| `Assets/Scenes/MainMenu.unity` | Current menu and Play entry |
| `Assets/Scenes/BattleTestScene01.unity` | Legacy initial test (1 player / 3 enemies) |
| `Assets/Scenes/BattleTestScene04.unity` | Legacy next scene (2 players / 6 enemies) |
| `Assets/Scenes/BattleTestScene06.unity` | Legacy follow-on scene (2 players / 6 enemies) |
| `Assets/Scenes/BattleTestScene_P1_Showcase.unity` | Isolated P1 three-mech, three-enemy-role showcase and P1.2A VFX validation |

Legacy navigation currently runs MainMenu → Scene01 → Scene04 → Scene06; Scene06's Yes route reloads itself. The showcase is separately enabled in build scenes; its result Yes replays the showcase and No returns to MainMenu. The final campaign must redesign results semantics intentionally.

**Showcase composition:** Hybrid (9,3), Assault (9,5), Marksman (10,2); melee drones (7,5)/(6,2), Scorpion Skirmishers (7,2)/(5,4), artillery (5,3). These are distinct walkable cells. Initial attack lanes intentionally exercise Marksman, Assault and Hybrid while later enemy turns exercise all enemy combat roles. Gameplay acceptance of P1.1A was conducted by the owner; subsequent P1.1C/P1.2A agent probes were focused rather than full-campaign certification.

Showcase six prefab families have health bars and configured colliders/movers. `CombatUnit` initializes a Slider (with inactive-child fallback for old scenes), warns if missing and updates health consistently. Unit card HP/EN presentation follows current state, not a new energy system.

### P1.1C curated sprite integration

- Original curated PNG sprites live under `Assets/Sprites/AeonSector_P1/{Assault,Marksman,Scorpion}/`. **15 PNGs**: four directional animation sheets and one icon per unit; **72 total animation frames** across twelve sheets.
- Import: Sprite Multiple for sheets / Single for portraits, PPU 256, Point, Clamp, uncompressed, no mipmaps, sRGB and alpha transparency, Full Rect, max 4096; row-major top-row-first slicing.
- Humanoid frame canvases: 896×896 and pivot approximately (0.5, 0.1741). Scorpion: 1024×1024, pivot approximately (0.5, 0.09). Visual-child scales currently 0.5 humanoid / 0.38 Scorpion, without root grid coordinate changes.
- Four clips + one controller for each archetype in `Assets/Animation/AnimationController/P1_Assault/`, `P1_Marksman/`, `P1_Scorpion/`. Two authored directions `FrontSW` and `BackNE`, other facings mirrored by `UnitVisualAnimator`. Four-frame idle loops take one second; eight-frame attacks are non-looping. The Hybrid controller is unchanged.
- Marksman `AnimEvent_FireRangedProjectile` occurs at 0.5s and `AnimEvent_RangedAttackFinished` at 0.99s, exactly once per relevant clip. Player missing-fire grace is 0.6s for Marksman, 0.25s for legacy Hybrid. Stalled player projectile deadline remains five seconds and resolves with at-most-once protection.
- Scorpion stinger's ranged release is configured near 0.75s from the enemy controller, with directional origin attachments; no player-specific ranged events. Target identity, numeric combat rules and archetype IDs unchanged. UnitStats icons use the curated art and the Skirmisher presentation label is Scorpion Skirmisher.
- Existing movement is represented by directional idle sprites; no dedicated walking/death/hit sprite sheets were imported. P1.2A adds procedural hit/death visual feedback.

## 4. P1.2A Combat Feel & VFX — implemented in source

Source checkpoint: `ef5691263993e07d8cbb485e068769a62226a161` (*New Attack VFX and Feedback*).

### Runtime and asset organization

The P1 showcase now contains `Assets/CombatPresentation/Prefabs/CombatPresentation.prefab`, with `Assets/Scripts/CombatPresentation/CombatPresentation.cs` coordinating visual presentation. Six `CombatVfxProfile` assets under `Assets/CombatPresentation/Profiles/` map Hybrid, Assault, Marksman, Scorpion, Drone and Artillery to distinctive colors, impact/camera magnitudes, melee contact/recovery values and projectile/effect prefab references. `UnitStats` and `BattleDamage` remain gameplay authority.

`Assets/Scripts/CombatPresentation/` includes:
- `CombatPresentation.cs` — optional coordinated combat-effects entry point.
- `CombatVfxProfile.cs` — unit-family presentation and timing settings.
- `CombatEffect.cs` — lifetime-managed energy charge/discharge, slash, contact, shockwave, artillery warning and destruction rendering.
- `UnitHitFeedback.cs` — 0.18s flash/recoil on the **visual child**, not the board transform/collider.
- `FloatingDamageNumber.cs` — actual displayed HP delta for both factions, about 0.85s lifetime, lethal label.
- `CombatHudStabilizer.cs` — cancels Cinemachine positional corrections for important UI roots.

Prefabs live under `Assets/CombatPresentation/Prefabs/` including `WeaponCharge`, `WeaponDischarge`, `EnergySlash`, `ContactBurst`, `GroundShockwave`, `ArtilleryWarning`, `EnergyCollapse`, `DamageNumber`, and projectile variants `HybridPulse`, `MarksmanLance`, `ScorpionStinger`, `ArtilleryShell`. Materials and small VFX textures live under `Assets/CombatPresentation/`; two accompanying shader assets live under `Assets/Shaders/`.

**Visual identities:** Hybrid restrained cyan pulses; Assault broad ice-blue slash/contact; Marksman fast narrow blue-white lance; Scorpion crimson stinger bolt; Artillery orange/red shell, warning rings and ground impact; Drone compact melee. Six showcase prefab families have `UnitHitFeedback` wired.

### Synchronization and lifecycle

- `BattleDamage.Apply` permits optional attacker/command presentation context and triggers hit text/flash after the HP change but before existing lethal cleanup. Lethal hits add a short DESTROYED cue and detached fading silhouette/sparks; grid occupancy and roster removal remain immediate.
- `ProjectileMover.FlyAndHit` supports a projectile-arrival callback; ranged damage and feedback now happen at actual impact, **not after the old 0.5s post-arrival hit animation**. New effects have approximately 0.09s visual recovery. Player duplicate-event/at-most-once and recovery locks remain intact.
- Assault melee connects at approximately 0.6s with 0.6s recovery. Drone melee uses approximately 0.7s contact and 0.8s recovery. Marksman 0.5/0.99s animation callbacks and Scorpion 0.75s release remain; Artillery adds about 0.22s launch anticipation while retaining its area/stagger logic.
- `CameraShakeImpulse.PlayImpact` uses short intensity-clamped/coalesced impulses (profiles roughly 0.035–0.14 strength and 0.1–0.2s duration), showcase avoids the prior long secondary shake tail. `CombatPresentation.shakeMultiplier` reduces/disables it; `CombatHudStabilizer` keeps UI roots from drifting. **No global hit-stop/time-scale modification** was introduced.
- P1.2A effects have intentionally brief lifetimes and limited particles (up to 32 per spark effect). Area attacks may create visuals per affected tile, so evaluate artillery overdraw/frame pacing in owner tests.

**Agent-recorded verification:** Unity compilation, shader/reference checks, staged showcase combat, exact damage and duplicate-fire guards, recovery/input locks, lethal occupancy cleanup, effects on both factions, enemy-to-player turn return, and under 0.5px HUD drift during a camera probe. Temporary staging was Play Mode only. **Full physical-input, every facing, victory/defeat replay and sustained performance are still owner acceptance items**; do not invent their results.

### Other current showcase/UI fixes

Unit-card names fit on a single line and temporary-art suffixes were removed. Combat prompts use actual controls and whole-squad end-turn phrasing. Showcase replay says REPLAY SHOWCASE. Decorative UI does not intercept pointer raycasts; `PlayerInputController` ignores world clicks on interactive UI. The current main-menu Options panel offers session-only master-volume adjustment and Close; **no saved comprehensive settings system exists**. UI was visually inspected at 1920×1080 and Options overlay at 1280×720. Automated showcase combat is **not** balance-certified (one staged run ended in player defeat with two enemies remaining).

## 5. Immediate work: P1.2B and P1.2C

### P1.2B — Tactical Readability (NEXT, not implemented)

Improve the existing `PlayerBattleUI` + `TilemapBoardAdapter` + `ActionMenuUI` presentation without replacing their runtime authority. Present:

- Stronger selected-mech cue and friendly/enemy distinction; do not rely solely on VFX color.
- Easily distinguished reachable movement cells, provisional path, legal attack range, actually legal targets, blocked/invalid tiles and out-of-range interactions.
- Intentional hover/click confirmation, target previews based on **real** attack validation, and understandable invalid-command feedback.
- State-aware action buttons and move/attack allowance cues that remain accurate mid-turn and after combat resolution.
- Consistent overlay layering with P1.2A VFX and HP bars, preserving pointer interactions and grid coordinate semantics.

Current board overlays reuse a limited highlight tilemap and the command UI is functional but elementary; avoid conflicting overlays and guessing command legality. Do **not** implement a new combat resource, attack shape, cover system or squad UI as part of this pass. The agent should inspect actual classes and serialized references before touching them.

### P1.2C — Battle HUD & UX Polish (AFTER P1.2B, not implemented)

Create a strong, game-specific common interface style for unit cards, command panels, turn/result cues, transitions and mouse/keyboard UX. Use accurate in-game state. The result becomes a **design foundation for** later Hangar, Armory and Mission Briefing menus, but do not implement those future screens in P1.2C. Preserve usable visual hierarchy across practical screen sizes; avoid intrusive HUD animation and misleading UI energy costs.

## 6. Future extension contracts — approved direction, NOT implemented

### P2.0 — Intro, squad builder and deployment

The initial playable roster is the three catalog archetypes. Future Hangar squad assembly must enumerate them, represent separately owned instances and committed selections, and support an owner-approved squad size/configuration policy; do not assume all three must deploy. Mission briefing should communicate objectives/threat/map. Intro remains concise, skippable/replayable.

`TurnManager` presently registers **scene-assigned units at `Start()`**. A future deployment contract must provide selected catalog IDs/owned-mech IDs, effective runtime stats, selected abilities, mission identifier, valid deployment/spawn cells and enemy definitions **before Start initialization**. Allow existing test scenes to fall back to scene-assigned rosters. Do not double-register units, reserve occupancy twice or create a second roster authority.

### P2.1 — Progression / Armory / purchases

Future owned-mech data will hold XP, level, any currencies, purchased attack upgrades/unlocks and active equipment/attack variant. Persist and load these independently of shared `UnitStats` assets and scene `CombatUnit`. A resolved-effective-stats/attack layer should apply base definitions plus owned progression at battle start. Author actual costs, unlock rules, attack behavior, resource semantics and UI transaction states during that phase; none of these are presently implemented. No new EN resource exists yet.

### P2.1B — upgraded attack visuals

Create a dedicated later **premium-quality attack variant production pass**: visually evolving energy swords, lightning/electric arcs, plasma/fireball attacks, enhanced rifle/projectile streaks, explosive area impacts, lighting/shader layers and controlled camera response, connected to real purchased/upgraded combat abilities. Prefer expanding `CombatVfxProfile`/shared VFX content keyed by attack definition and upgrade rank rather than one mech prefab per level. Keep gameplay-authoritative AoE cells and targeting separate from decorative glow/burst size. Clear differentiation and readability matter more than unlimited particles.

### P2.2 — advanced battle environments and maps

Future environment production is a **major authored content phase**: distinctive new map layouts, original coherent isometric tile palettes, terrain transitions, background scenes, building/wreck/mountain/rock props, intentional blocking terrain, atmospheric particles, shaders, URP 2D lighting and restrained post-processing. Game-style consistency, depth/sorting and tactical legibility are essential. Visual obstacles must update `TilemapBoardAdapter` walkability and agree with BFS/A* routing, enemy planning and deployment validity. Cover, LOS, destructibility or elevation combat rules need separate explicit design approval. Check for solvable routes, spawn safety and frame-time/overdraw budgets.

### P2.3 — campaign integration / final polish

Connect several meaningful missions, varied enemies, upgrades, rewards, replay/next-mission navigation, persistent progression and functional settings into one stable menu-to-battle-to-results loop. Current legacy scene flow and session-only audio option are scaffolding, not the final product.

## 7. Milestone status and known constraints

| Milestone | Current status | Notes |
| --- | --- | --- |
| P0 stabilization | Baseline | Do not reopen without a concrete regression |
| P1.0A input/controller split | Implemented | Maintain single command/resolution authority |
| P1.0B capabilities, AI roles, catalog | Implemented | Supports new unit authoring without UnitType-specific branching |
| P1.1A showcase | Implemented; owner gameplay-tested | Six unit families / three selectable-in-data mechs |
| P1.1B original sprites | Approved curated art | Original Assault, Marksman, red Scorpion |
| P1.1C asset/Animator integration | Implemented; focused smoke tested | Directional animations, icons, sprite pivots and projectile timings |
| **P1.2A Combat Feel & VFX** | **Implemented in source; owner acceptance pending** | Profiles, flashes, impacts, damage/death presentation, camera/HUD feedback |
| **P1.2B Tactical Readability** | **NEXT** | Overlays, selection, range/target, action-state clarity |
| **P1.2C HUD & UX Polish** | **After P1.2B** | Cohesive functional battle interface and eventual menu design foundation |
| P2.0 Hangar, squad and briefing | Not implemented | Actual pre-match selection and validated deployment |
| P2.1 progression and Armory | Not implemented | Owned-mech persistence, XP/currency, purchase/upgrade and stronger attacks |
| P2.1B premium upgrade VFX | Not implemented | Visibly spectacular, tier-specific attack effects |
| P2.2 battlefield/environment production | Not implemented | Complex blocking maps, palettes, props, backgrounds, URP effects |
| P2.3 campaign completion | Not implemented | Multiple connected missions and complete persistent player loop |

**Known constraints:** legacy prefab visual wiring may differ outside showcase; broad automated regression coverage absent; 5-second player stalled-projectile fallback remains; enemy projectile sequence remains its established coroutine; walking/hit/death sprite animations absent; owner should examine area VFX performance, all facings and full terminal results. Numeric combat balance is not certified. The future Hangar/Armory, persistent upgrades and complex environment content exist only as approved roadmap goals.

## 8. Agent working conventions

Operate on `New-features-test` in the owner's Unity Editor project. Follow `AGENTS.md` for authority, scope, validation, commits and reporting. Task briefs belong in desktop chat; avoid adding checkpoint-specific Markdown files. Update this context with durable **implemented** changes after integration, and remove obsolete statuses rather than appending a chronology. `GAME_VISION.md` is product direction and changes only with owner approval.
