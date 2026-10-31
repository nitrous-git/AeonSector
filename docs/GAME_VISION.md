# Aeon//Sector — Approved Game Vision

**Status:** Product direction approved by the project owner, 2026-10-31  
**Scope:** Desired future player experience and development priorities—not a list of features already implemented  
**Technical implementation truth:** [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md)

## 1. Game promise

Build **a polished, approachable 2D isometric, turn-based mecha tactical RPG** where a player forms a small squad of distinctive mechs, survives missions against increasingly varied enemies, grows in combat power, and can **see that growth** in shaders, sprites, attack effects and battlefield spectacle.

The initial goal is a **cohesive, highly playable compact campaign**, not a sprawling content library. Prefer a few rich missions, memorable enemy types and complete menus over many unfinished features.

Maintain a recognizable sci-fi / industrial / mechanical visual identity. Existing assets are the stylistic starting point, not the ceiling for polish. New content should be original, visually coherent and readable on an isometric grid.

## 2. The intended player journey

1. **Start / intro.** Main menu launches a short, generic opening cutscene: a few illustrated sci-fi panels, narrative text, cinematic fades/transitions and appropriate audio. Keep it concise, skippable and replayable.
2. **Hangar / squad preparation.** Before a battle, the player sees the available mech roster, compares roles, attacks, stats and levels, selects a small squad from **two or three distinct mech choices initially**, and optionally applies earned upgrades.
3. **Mission briefing and deployment.** Show encounter context, enemy threat or difficulty information; load the selected mechs into valid deployment cells. Present the objective clearly.
4. **Tactical battle.** Move and attack with a selected squad on a 2D isometric map against **multiple enemies per encounter**. Enemy archetypes have different roles, sprites and behaviors; map layouts, blocking obstacles and visual cues produce tactical choices.
5. **Results and progression.** Present win/loss, earned XP or other rewards, and mech level gains. Persist owned mechs and progress across missions. Return to hangar to prepare for the next mission.
6. **Continue campaign.** New missions introduce different enemy mixes, difficulty, environments and opportunities for advancement, making the player's squad feel increasingly capable.

**Core flow:** Intro → Hangar / Select & Upgrade → Mission → Results / XP → Hangar → Next Mission.

The existing current combat action economy is **one movement action + one attack action per unit turn**. The product direction does not silently change that rule; future abilities or extra actions require an intentional gameplay decision.

## 3. Playable mech roster

Target an initial roster of **three distinct, finished mecha archetypes**, with a selection step before battles. The current project contains one established integrated playable hybrid mech and other legacy melee/ranged assets that can be revived and polished.

Suggested starting roles (**concepts, not binding names or finalized stats**):

| Archetype | Mechanical identity | Visual identity / tradeoff |
| --- | --- | --- |
| **Hybrid** | Balanced melee and ranged access | Versatile, middle-ground silhouette, reliable core mech |
| **Assault** | Strong close combat, durability, positioning | Heavier frame, impactful blade/slash effects, lower reach |
| **Artillery / Marksman** | High ranged pressure and spacing | Distinct launcher / cannon effects, more vulnerable up close |

Unit selection must reflect **real supported commands**. A mech without ranged capability should not have a functional ranged attack simply because its stats schema has ranged fields. Separate identity, shared base stats, capability/attack definitions, runtime condition and visuals.

Future extensions may include specialized weapons, multiple ability types, loadouts, and support/scout roles. Do not introduce all of these before the first roster is playable.

## 4. Mech levels and power fantasy

**Levels should matter numerically and visually.** Earn XP through missions; level gains improve effective stats or unlock meaningful upgrades while giving more impressive combat presentation.

Examples of scalable visuals:
- **Early levels:** restrained core glow, basic projectiles, blades, impacts and idle energy details.
- **Intermediate levels:** richer weapon trails, improved projectile sprites/particles, brighter blade shaders, more expressive hits and energy accents.
- **Advanced levels:** distinctive attack effects, occasional sprite variants, layered impact VFX and visibly stronger mech presentation.

These are **presentation tiers, not fixed level thresholds**. The developer should propose practical milestones after the progression model exists.

Engineering principles:
- Store XP, level and unlocked upgrades **per owned mech**, persist across battles and game restarts.
- Keep shared ScriptableObjects as immutable design-time defaults; compute effective runtime combat values from base + progression.
- Prefer reusable shader/material parameters, VFX definitions and sprite sets; avoid duplicating an entire character prefab for every level.
- Visual effects must remain performant and must not obscure tile occupancy, range previews, targets or health.
- Progression should feel earned and balanced; do not invent permanent stat curves without a scoped design/acceptance step.

## 5. Enemies and tactical encounters

Move beyond reusing the same insect enemies by introducing original enemy archetypes with **different AI combat roles, sprites, attack animations and death effects**.

Initial target: a few clearly differentiated threats such as melee pursuers, ranged skirmishers and artillery/area attackers; later add tougher variants, defensive enemies and unusual encounters. Enemy behavior should be configurable independently of a specific `UnitType` enum value.

Missions should vary by enemy composition, spawn positions, difficulty and map geometry. A harder level is not simply more HP; it may involve better enemy positioning, mixed roles and more constrained approaches.

Combat FX direction: clear anticipation, readable target zones, high-quality projectile/blade effects, strong hit reactions, convincing death feedback and controlled screen/camera response. Spectacle should support tactical understanding.

## 6. Battlefields, tiles and environmental polish

Expand the current battle test maps into **authored, attractive tactical environments** with improved isometric tiles, terrain transitions, environmental props and obstacles such as mountains, debris, wrecks and structures.

- Differentiate **blocking tiles** from decoration in the authoritative walkability model.
- Ensure blocked cells cannot be reserved or traversed; path previews and A* must match the visuals.
- Keep spawn points, reachable areas and enemy routes intentional; test that missions remain solvable.
- Use isometric sprite sorting, shadows/lighting, atmospheric elements and URP 2D post-processing to build depth and mood.
- Apply post-processing with restraint so unit silhouettes, target overlays and grid interactions remain readable.

Line-of-sight, cover systems, destructible terrain and terrain bonuses are **possible later tactical additions**, not requirements that currently exist.

## 7. Menus and user experience

Make the entire loop feel like one game, with a coherent interface language instead of isolated test screens.

**Desired screens:** main menu; intro/skip; hangar and mech details; squad selection; mission briefing; battle HUD; pause; results/rewards/level-up; options/settings. Implement each screen **functionally** when scheduled—avoid buttons that only log placeholders.

UX principles:
- Communicate available move/attack/ability options; disable or explain unavailable commands.
- Show range, valid targets, terrain obstruction, movement path and selected unit state clearly.
- Make hover/click/selection feedback consistent and responsive.
- Keep typography, panel layout, icons, transitions and sound cohesive.
- Include working controls for relevant audio, graphics/post-processing and gameplay preferences when options are built; save settings appropriately.
- Support the current mouse/keyboard workflow first; broader gamepad/accessibility work can be scheduled separately.

## 8. Production sequence and milestones

| Phase | Intent | User-visible acceptance |
| --- | --- | --- |
| **P1.0A — Input refactor** | Established foundation | Existing controls continue to work; no duplicate command state |
| **P1.0B — Unit / capability / AI architecture** | The next focused systems task | New unit roles and enemy behaviors are configurable, with backward compatibility |
| **P1.1 — Expanded unit content** | Prove the architecture with real content | Original playable mech and enemy integrated, then complete initial 3-mech roster |
| **P1.2 — Tactical UX and visual pass** | Improve moment-to-moment game feel | Clear targeting, strong attack/death VFX, cleaner cards/menus, appealing sprites |
| **P2.0 — Intro + hangar + deployment** | Connect pre-battle experience | Skippable intro, selected squad actually spawns in battle |
| **P2.1 — XP + levels + visual tiers** | Mech growth between missions | Combat power changes, saved progress, visibly improved attacks |
| **P2.2 — Missions + environments** | Expand encounter variety | Connected levels, enemy mixes, authored blocking terrain, original map props |
| **P2.3 — Polish and complete demo** | Cohesive campaign experience | Working options, transitions, UI, audio, post-processing and reliable end-to-end loop |

**First meaningful vertical-slice target:** a brief intro; three distinct mech choices; functional pre-battle squad selection; several connected encounters with differentiated enemies; basic persisted XP/level progression; a visually noticeable upgrade; readable polished HUD/results and functional options. Three missions is a useful initial target, **not a fixed content cap**.

Phase labels describe **work to schedule**, not blanket permission to implement everything. Each desktop task should own one integrated checkpoint and stop for owner playtesting.

## 9. Creative freedom and constraints

The implementation agent should have meaningful creative agency for **original mecha, enemy silhouettes, attack patterns, effects, menus and battlefields**. Propose coherent concepts and ship assets that are technically compatible with Unity's 2D isometric stack.

Non-negotiable production constraints:
- Preserve functioning combat and existing scenes until intentional migration.
- Make new content real and playable, not just configuration scaffolding or disconnected mockups.
- Use copyright-safe, original/licensed visual assets; no assumed access to external image generation or proprietary asset packs.
- Prefer clear, reusable architecture where it enables concrete content; do not overbuild a speculative RPG framework.
- Owner handles principal manual validation; agent does focused compilation / Console checks and targeted smoke testing.
- Keep the future vision separate from technical implementation truth and from individual task prompts.

This vision should change **only on owner approval**. Implementation status and discovered technical constraints belong in `PROJECT_CONTEXT.md`.
