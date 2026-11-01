# Aeon//Sector — Approved Game Vision

**Owner-approved direction updated:** 2026-10-31  
**Purpose:** The intended finished player experience, priorities and visual standard—not a claim that future systems are implemented  
**Current implementation truth:** [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md)  
**Agent execution rules:** [`../AGENTS.md`](../AGENTS.md)

## 1. Product promise

Create a **cohesive, polished 2D isometric turn-based mecha tactical RPG** where players assemble a small squad of unique combat mechs, deploy into increasingly demanding missions, earn rewards, upgrade their machines and weaponry, and **see and feel their growth** through substantially more powerful attacks and richer presentation.

The target is a compact, complete, replayable campaign experience rather than a huge collection of partial features. A few excellent missions, fully functioning menus, distinctive characters and upgrades, and expressive combat should beat a sprawling but unfinished game.

The established original mech, red hostile mechanical-insect faction and existing elevated-isometric pixel/raster aesthetic are the **stylistic baseline**. Future art should be dramatically better in finish while still recognizably belonging to the same game. Prioritize functional gameplay, clear tactical communication, polish and an identity of its own.

## 2. Target player journey

1. **Main menu and intro:** open the game, view a short illustrated/text atmospheric sci-fi intro (skippable and replayable), begin or continue a campaign.
2. **Hangar and squad builder:** browse the initial **three playable mech archetypes** (Hybrid, Assault, Marksman), inspect their real roles, current levels/stats/attacks and choose the mission squad before every battle. Allow later roster growth without designing a new menu.
3. **Armory / Upgrade Bay:** inspect current resources, earned upgrades and purchasable stronger attacks; preview each option's **real mechanical effect and distinct visual tier**, confirm a purchase and see the mech's revised loadout or ability level.
4. **Mission briefing:** understand the mission objective, threat composition and environmental theme, confirm squad/loadout, then deploy the selected mechs onto valid board starting positions.
5. **Battle:** play an isometric turn-based tactical encounter with engaging enemy AI, meaningful terrain and blocking, carefully telegraphed attacks, strongly readable command feedback and impactful combat VFX.
6. **Results and growth:** see win/loss, earned XP and currency, level-ups/unlocks, and a meaningful next action: retry, hangar, next mission, or main menu. Save progress.
7. **Next mission:** fight new mixes of enemies in more complex authored environments, with improvements in both challenge and your squad's presentation.

**Core loop:** Intro → Hangar / Build Squad → Armory / Upgrade → Briefing → Deploy → Battle → Results / Rewards → Hangar → Next Mission.

The currently implemented core rule is **one move and one attack per mech per turn**, independently consumable. Do not silently replace this economy with action points. Future skill systems may extend it only by an intentional gameplay decision.

## 3. Mechs, roles and squad construction

### Initial playable archetypes

| Mech | Gameplay identity | Visual identity |
| --- | --- | --- |
| **Hybrid** | Balanced melee/ranged flexibility | Versatile, restrained energy equipment; general-purpose chassis |
| **Assault** | Durable, close-quarters melee specialist | Wide armor, imposing blade arcs, weighty impacts |
| **Marksman** | Precision long-range fighter | Ivory/blue chassis, slender rifle, sharp focused blue/cyan energy |

The initial selection pool contains these three playable mech archetypes. **The exact maximum deployable squad size, duplicates policy, reserve slots and loadout rules remain to be designed**, rather than assumed from the number of available choices. The Hangar must honestly show move/attack capabilities, stats, progression, equipment and ability variants.

The player should be able to build a squad **before each mission**, not merely see a fixed roster hardcoded into the scene. Different mechs should invite interesting composition choices and strategic tradeoffs.

The Scorpion/Skirmisher and other insectoid enemies are distinct, red/crimson-dominant hostile faction units, not additional playable squad options by default.

## 4. Hangar, Armory and Mission UI

Establish a **shared interface language in P1.2C** so P2 menus do not feel disconnected from the in-battle HUD.

### Hangar / Squad Assembly (P2.0)

A visually impressive, functional pre-battle workspace showing the actual owned mechs. Present selectable character visuals, clear team slots, mech name/class, level, current HP/combat parameters, attack capabilities, upgrade status and brief role summaries. Clearly distinguish **available**, **selected**, **locked**, and **not-deployable** states. Accept/revert the draft squad and persist the committed selection appropriately.

### Armory / Upgrade Bay (P2.1)

One or more functional menus for comparing, buying and equipping/improving weapons/abilities and associated upgrade levels. Show cost, prerequisites, current resources, resulting combat delta, and **a genuine preview of the chosen VFX tier**. Never display a purchase as successful unless its state is saved and the upgrade will actually apply in combat. Avoid adding dozens of cosmetic-only buttons.

### Mission Briefing (P2.0)

A distinct, appealing briefing panel with mission description, map/environment preview, visible objectives, enemy threat cues, chosen squad and deploy confirmation. Reuse shared visual components but avoid making everything one overloaded screen.

### Results / Level-up (P2.1/P2.3)

Make outcomes, rewards and progression understandable. Provide actual navigation to hangar, retry or next mission; current demo result buttons should not dictate finished UX.

**UI principles:** original readable typography, clear hierarchy, deliberate visual density, responsive pointer/focus feedback, legible stat changes, coherent audio/transitions, functional controls rather than placeholders, and practical keyboard/mouse support first. Menus should be polished at realistic desktop resolutions and preserve consistent colors and icons.

## 5. Progression and purchasable attack upgrades

Progression must have **real mechanical value and a visible power fantasy**. Earn XP/currency through missions, persist ownership across launches, level individual mechs, and buy or unlock progressively stronger attacks. Exact currencies, economy values, level formulas, costs, upgrade trees and unlock thresholds are **future design decisions**, not preapproved numbers.

Future upgrades can improve damage, range, attack type, cooldown/resource cost if a real resource system is approved, area coverage, special effects, or alternate combat moves. Do not infer expanded damage cells solely from a larger visual explosion; targeting/damage shapes must be explicitly implemented and communicated.

### Dedicated visual power-tier phase — P2.1B

**A stronger attack must look substantially stronger.** Build a later, intentional VFX content-production pass, not just a small shader brightness slider:

- **Starter tier:** clean basic energy bolts, restrained slash, reliable hit sparks.
- **Advanced tier:** weapon-charge pulses, richer blade trails, brighter focused projectiles, electric filaments and fuller impact effects.
- **Elite tier:** striking multi-layer discharge, lightning/electrical arcs, large fireball/plasma visuals where appropriate, impactful area attacks, shockwaves, controlled lighting and impressive destruction feedback.

This three-tier example is an **art-direction metaphor, not fixed levels or a guaranteed upgrade count**. Different attack families may warrant different staging. All upgraded visuals must still match the approved art style and remain tactical/readable.

Example families:

| Family | Upgrade potential | Presentation progression |
| --- | --- | --- |
| Assault blade | Stronger melee, possible special cleave if designed | Sharper trails → energized arcs → dramatic plasma impact |
| Marksman rail shot | Precision/range/damage or specialized piercing behavior if designed | Clear bolt → electric charge → intense blue-white lance |
| Hybrid weapons | Balanced abilities / branches | Refined pulse → richer charged strike → distinctive hybrid burst |
| Future area attacks | Legitimate AoE profiles with documented cells/damage | Compact burst → plasma/fireball charge → layered shockwave/area discharge |
| Hostile attack variants | Escalating threat identity without faction confusion | Crimson/orange-red charge, stinger discharge, explosion signatures |

The upgrade/ability system should associate a **shared base attack definition**, owned unlock/rank and effective gameplay rules with a **selectable VFX/audio/presentation profile**. Avoid making a copy of the entire mech prefab for each upgrade level. Treat equipment or weapon selection and permanent progression as separate state concepts where needed.

Data contracts:
- `UnitStats` design assets are shared, immutable defaults at runtime.
- Persist XP, level, currency ownership, purchased upgrades, equipped/active choices and campaign position independently.
- Compute effective runtime stats and attack behavior from base definitions plus owned progress.
- Support replayable missions and saving/loading without duplication or free upgrades.
- Keep visual tier a presentation concern that follows actual equipped gameplay level—not a source of damage authority.

## 6. Combat feel and tactical UX

**P1.2A** established the first reusable combat-feedback vocabulary. Continue refining it rather than replacing it without cause.

**P1.2B — next:** clear selected-unit identity, reachable movement and chosen route, attack range and legal target tiles, invalid/blocked indicators, enemy hover and intent cues where supported, spent/available command states, and coherent grid overlays. Never suggest a shot is valid when the underlying system will reject it.

**P1.2C — after:** cohesive battle HUD, readable unit cards, purposeful action menu, clear button states, tactical turn indicators, polished transitions, scalable layout and consistent visual feedback, forming the UI foundation for the Hangar/Armory.

Spectacle principles: anticipation → action → clear contact → damage/death confirmation → recovery. Effect intensity should communicate significance. Avoid overly long animation locks, huge obscuring flashes or excessive camera shake; players must be able to read tiles, selected units and tactical consequences.

## 7. Environment, maps and art-production phase — P2.2

A later **major environment-production phase** should build **new, more complex, substantially more professional battle levels**—not merely recolor the current test grid.

Expected ingredients:

- Several **authored combat layouts** with distinct silhouettes and recognizable environmental themes: e.g., damaged industrial complex, derelict research base, rugged canyon, reactor facility or siege approach; select a manageable few for the initial campaign.
- **Original, game-style-matched tile palettes**, terrain transitions and layered background scenery with consistent isometric perspective, scale, lighting and materials.
- Purposeful **blocking cells**: cliffs, mountains, wreckage, walls, machinery, heavy debris and structures. Decoration and actual collision/path legality must agree.
- Meaningful enemy placements and roles; routes, choke points, approach options and gameplay objectives that make each mission interesting.
- Authored foreground/background props, shadows and sorting/occlusion; readable unit silhouettes and health/target overlays.
- Ambient particle/VFX layers (dust, embers, fog, reactor particles), expressive shaders/materials, URP 2D lighting and **restrained post-processing** to give professional atmosphere.
- Performance budget, overdraw control and practical camera framing. The player must always know where they can move and attack.

**Do not make cover, line-of-sight, destructible terrain or height rules implicit** merely because a visual prop suggests them. Such mechanics require explicit scoped design and tactical-system integration. Blocking must be enforced through the existing authoritative board, BFS/A* paths and valid spawn/deployment logic.

Each newly built map should demonstrate intentional battle composition and be playable with the current tactical rules before adding more content. Treat art generation and import/Unity integration as separate quality gates when that improves consistency.

## 8. Enemies, missions and campaign scale

Start with several varied, polished encounters featuring melee pursuers, red Scorpion ranged skirmishers and area-artillery threats; then add original enemy archetypes with genuinely different attack behaviors, role-based AI, animations and red hostile visual identity. Escalate difficulty through encounter geometry and enemy roles, not merely HP inflation.

A compact campaign of approximately three distinct playable missions is a useful first end-to-end target—not an upper limit. Each mission should have a real objective, appropriate enemy/terrain composition, fair route/deployment and a clear result. Save and transition rules must be consistent across encounters.

## 9. Phased production roadmap

| Checkpoint | Status at 2026-10-31 | User-visible goal |
| --- | --- | --- |
| P0 / P1.0A–B — combat foundation and extensibility | Implemented | Stable combat, separated player command ownership, supported attacks and enemy AI roles |
| P1.1A–C — roster and art | Implemented; prior owner playtests for showcase | Three distinct playable mechs, expanded enemies, original production sprites/animations |
| P1.2A — Combat Feel & VFX | Implemented in source; full owner acceptance pending | Impactful weapon effects, damage numbers, reactions, camera/destruction feedback |
| **P1.2B — Tactical Readability** | **Next planned** | Obvious selected unit, movement path, legal target, actionable commands and hover feedback |
| **P1.2C — HUD & UX Polish** | **Planned next** | Cohesive production-level battle interface and presentation style |
| P2.0 — Intro, Hangar, squad, briefing | Future | Choose a squad from the initial three mech types and deploy to a mission |
| P2.1 — Progression, Armory, upgrades | Future | Persist XP/currency and buy stronger, effective attacks between missions |
| **P2.1B — Advanced upgraded-attack VFX** | **Future, dedicated high-impact art phase** | Upgrades yield spectacular differentiated projectiles, energy/fire/lightning/AoE effects |
| **P2.2 — Battlefield/environment production** | **Future, dedicated major visual phase** | Distinct complex battle maps, blocking terrain, new tiles/props/backgrounds, shaders/lighting |
| P2.3 — Campaign integration and final polish | Future | Reliable end-to-end missions, saved progression, results, options and polished demo |

The sequence preserves current momentum: **finish tactical readability and battle UX first**, then use that UI foundation for the squad builder and Armory. Avoid implementing all future phases in a single task; scope a concrete, playable, visually reviewable checkpoint each time.

## 10. Production standards and authority

- The **owner** decides major design tradeoffs, core balance progression and accepted visuals; the agent may choose local details within an approved phase and must deliver directly usable assets/code.
- Prefer technically real and copyright-safe original/licensed art, not disconnected concept pictures misrepresented as production sprite sheets.
- Reuse current P1.2A effect infrastructure for later attack tiers unless a documented architectural reason requires evolution.
- No quality pass should break existing battle mechanics, scene paths, enemy turns or input gating.
- The owner will do primary manual gameplay acceptance; the agent conducts focused compile/Console/Play Mode checks and reports actual evidence.
- **Do not equate vision with implementation**: this document defines desired future outcomes; see `PROJECT_CONTEXT.md` for what exists today.

**Vision maintenance:** change this approved direction only on an explicit owner decision, and leave task results in the living implementation context rather than accumulating a historical diary here.
