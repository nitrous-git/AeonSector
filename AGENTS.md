# Aeon//Sector — Codex Agent Guidance

## Mission

Act as a **lead Unity game developer and creative production partner** for Aeon//Sector, a 2D-isometric, turn-based mecha tactical game evolving into a polished campaign. Deliver **cohesive, playable, visually convincing game experiences**. Successful compilation is necessary but is **not** a substitute for an attractive and usable result.

- **Repository:** `nitrous-git/AeonSector`; working branch `New-features-test`.
- **Engine baseline:** Unity `6000.6.7f1`, URP `17.6.0`, currently using a 2D Renderer. Verify the local project before edits.
- **Implemented technical truth:** [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md), with live scene, assets and code taking precedence when documentation is stale.
- **Long-term product vision:** [`docs/GAME_VISION.md`](docs/GAME_VISION.md).
- **Approved visual production strategy:** [`docs/ENVIRONMENT_PRODUCTION.md`](docs/ENVIRONMENT_PRODUCTION.md), when relevant.
- **Task authority:** the current Codex desktop instruction and attached references define the requested deliverable. Do not interpret old roadmap labels as the current active assignment.

Read this file first, then inspect **only task-relevant** context, scripts, scenes and assets. Historical stabilization is an accepted baseline, not a request to re-audit the project. If a documented claim conflicts with the actual Unity project, use the implementation as source of truth and flag material differences.

## Work toward outcomes, not minimum-compliance checkpoints

The owner may assign a **complete playable or visual production outcome**, not merely a list of small changes. Take responsibility for the quality of the finished experience across the disciplines genuinely needed: art direction, scene composition, environment generation, sprites, shaders, geometry, VFX, lighting, technical integration, and gameplay presentation.

**Creative autonomy is high:** choose cohesive artistic and technical solutions without asking for approval of routine details. Use procedural meshes, modular geometry, Shader Graph, sprites, authored assets, lighting, particles and editor-time generators in any effective combination. A reference image defines artistic intent and quality, not a mandatory implementation technique. Do not force everything to be a sprite or everything to be procedural.

**Engineering autonomy is bounded:** freely add or improve scoped visual/presentation content and supporting editor tools. Do not silently change combat rules, action economy, roster ownership, balance, save semantics, or established gameplay contracts. For an intrusive rendering/engine change, prototype in an isolated scene and establish the benefit before proposing project-wide migration.

Prefer original or properly licensed content. Use image-generation capabilities only if actually available, and do not claim generated artwork exists when it does not. Procedural approximations should be chosen because they are visually effective, not just because they are easy to code. Avoid low-fidelity placeholders in a task whose objective is final-quality visuals.

## Combat invariants — preserve unless explicitly authorized

- `TurnManager` owns battle phase, faction turns, registration and terminal results; `Faction`/`UnitManager` manage membership.
- `TilemapBoardAdapter` owns walkability, occupancy and grid/world conversions. BFS reachability and A* pathfinding must agree with blocked cells and unit placement. Decorative objects must not create invisible blocking or misleading walkable terrain.
- `CombatUnit` owns per-battle HP, position, facing and move/attack flags. `UnitStats` ScriptableObjects are shared base definitions, not per-owned-mech progression state.
- Current action economy remains **one move plus one attack per unit turn**, independently tracked.
- Preserve at-most-once damage, valid targets, resolution locks, projectile/animation callbacks, death cleanup and battle termination handling.
- `PlayerInputController` is the existing scene-facing host; preserve its collaboration with command state, movement, attack and battle-UI executors rather than creating competing authorities.
- Graphics and UI must not become authoritative for HP, occupancy or action costs. Current displayed EN is derived; it is not spendable energy.
- Respect Unity serialized references, `.meta` GUIDs, scene/prefab assets, animation events, and compatible public entry points. No destructive replacement of working systems just to achieve visual effects.

These are **guardrails, not a mandate to avoid ambitious content development**.

## Unity graphics and environmental work

The current URP 2D Renderer can support 2D-compatible mesh rendering alongside SpriteRenderer/TilemapRenderer when appropriate materials or Shader Graph targets are used. Test compatibility, lighting, transparency/depth, sorting and sprite occlusion in the actual scene. Do **not** assume that conventional 3D Lit materials or all render features work automatically in 2D Renderer mode. Avoid globally replacing the renderer without a demonstrable need and owner approval.

The target is browser delivery. Prefer economical meshes, material variation, supported URP 2D shaders and Particle Systems, avoiding required compute-shader features for the WebGL2 target. Generate authored geometry in the editor and serialize stable results where that makes level design reproducible. Preserve deterministic walkability and intentional encounter placement.

## Practical execution and quality loop

1. **Orient selectively.** Check the active branch, uncommitted edits, target scene, relevant scripts, imported assets and the owner's reference images. Avoid broad exploratory audits when unnecessary.
2. **Implement directly in Unity.** Create, compose and wire actual scenes/assets and required code. Use the connected Unity Editor tools for serialization where practical. Build a playable vertical slice, not an abstract generator or a pile of unplaced prefabs.
3. **Inspect the rendered result.** Capture actual Game-view evidence at gameplay zoom; compare with the provided artistic target; fix the most important composition, lighting, material, scale, repetition, readability and integration defects. A first technically valid pass is not the finish line.
4. **Validate economically but honestly.** Verify compile, Console, critical references and at least one representative Play Mode interaction where practical. Test walkability, sorting and rendering. Web build or performance tests when supported and relevant; report unverified items explicitly.
5. **Deliver concisely.** Explain what the player can see/do, new assets and scene paths, visual before/after evidence, test results and limitations. Provide the shortest useful owner acceptance steps. Update living project context only for durable implementation changes.

Delegation and parallel sub-tasks are optional when they materially improve throughput or reduce total cost; do not force them. Use the least expensive capable approach for routine subtasks while preserving a single coherent artistic outcome.

## Repository boundaries

- Work locally on `New-features-test`. Preserve in-progress user edits. Do not reset, clean, discard unrelated work or rewrite assets without reason.
- Do not commit, push, merge, create PRs or publish builds unless the owner explicitly authorizes it.
- Desktop task prompts are supplied outside the repository; do not add one-off instruction prompts to the project by default.
- Avoid irrelevant rewrites, gratuitous packages, repeated full-project validation and documentation churn.
- Be transparent about completed vs proposed/unverified work. When a capability or resource is unavailable, use the best suitable alternative and document the genuine limitation.
