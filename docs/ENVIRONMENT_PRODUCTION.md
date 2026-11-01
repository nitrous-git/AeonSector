# Aeon//Sector — Environment Art & Hybrid 2.5D Production Direction

**Status:** Approved creative production approach, 2026-11-01  
**Applies to:** Solar Citadel first; reusable lessons may inform the other two approved battlefield themes.  
**Relationship to existing docs:** `GAME_VISION.md` remains the product/experience vision. `PROJECT_CONTEXT.md` remains the record of **implemented** code and assets. This document authorizes a creative *method*, not a claim that new technology is already in the project.

## Artistic target

Produce striking, original isometric mecha battlefields that match the visual identity, mood, scale, palette and craftsmanship of their **attached approved concept references**. The provided mockups are the visual-quality target, not a technical blueprint or a permission to silently substitute simplified graphics. Favor cohesive composition and a memorable, finished scene over a large asset count.

Keep existing mech/enemy artwork recognizable. Each battlefield should have a distinct atmosphere, architecture and visual language, while preserving clear unit silhouettes, movement targets and tactical choices. Do not add unrelated environments or overhaul all three themes in a Solar Citadel assignment.

## Hybrid approach — freedom of method

The scene may mix, according to what produces the **best result**:

- **High-quality sprites/illustrated elements:** characters, focal landmarks, signature machinery, unique silhouettes, decorative art and textures where authored imagery is visibly superior.
- **Procedural/modular geometry:** metallic platforms, industrial beams, bridges, facades, rails, pillars, mechanical recesses, tile-edge depth and reusable structures. Generate stable reusable content in editor-time tools where useful; runtime generation is optional.
- **Shaders and materials:** controlled emissive accents, animated energy circuits, scrolling surface details, scanning patterns, holographic projections, reactive surfaces, material variation and tasteful atmospheric treatments.
- **Particles/animation:** vents, steam, sparks, dust, energy motes, ambient motion, localized effects and restrained camera atmosphere.
- **Deliberate level design:** art-direct the layout rather than relying on indiscriminate random generation. Procedural techniques are a means of production efficiency, not a requirement for randomness or simplistic repeating meshes.

There is no fixed required percentage of procedural versus authored content. Choose technique by visual quality, scalability, implementation risk and performance.

## Real project constraints

- **Current stack:** Unity 6000.6.7f1, URP 17.6, 2D Renderer, isometric Tilemaps and sprite-based mechs.
- **Renderer compatibility:** Unity 6 supports appropriately configured MeshRenderer objects in URP 2D scenes, including the 2D-compatible `Mesh2D-Lit-Default` material or suitable 2D Shader Graph targets. Test this in the actual project before relying on it. Ordinary 3D lighting/PBR workflows are not automatic replacements for 2D lighting.
- **Tactical authority:** keep `TilemapBoardAdapter` as the source of truth for ground, blocked cells, positions and paths. Decorative geometry must align with the authoritative grid; no false walkability or visually invisible walls. Elevation is initially **presentation**, not a new elevation gameplay mechanic.
- **Sprites and 2.5D:** confirm orthographic/isometric camera alignment, pivot/PPU, ground contact, tile-to-world scale, sorting, occlusion, translucency and unit readability. Existing placeholder tile dimensions may be corrected in the isolated production scene; don't blindly propagate changes to legacy scenes.
- **Web delivery:** favor WebGL2-friendly URP shaders, CPU/editor-generated meshes and modest particle counts. Avoid requiring GPU compute, large dependent assets or wholesale rendering migration. Optimize for readable browser gameplay, not only attractive still renders.
- **Rights and asset fidelity:** use original, provided or appropriately licensed art. Don't claim an external image-generation tool was used unless it was actually available. If a critical piece cannot be produced to reference quality, call out the gap rather than masking it with poor proxy art.

## What a finished environment means

A successful environment is not merely a generated mesh library, importable tile set or compilation-passing scene. It is a **playable, art-directed Unity battlefield** with convincing depth and spatial composition, complete foreground/midground/background treatment, integrated units, lighting and motion, and no glaring placeholders.

Review actual Game-view screenshots at intended gameplay zoom. Inspect a wide establishing composition, a close combat/targeting state, terrain boundaries and foreground occlusion, and readability with multiple units. Correct the biggest visible discrepancies from the reference. Verify pathfinding and blocked geometry. Provide visual evidence and targeted owner test instructions.

## Production sequencing

1. **Solar Citadel — first complete vertical slice:** choose techniques freely, compose and implement an end-to-end playable showcase, assess quality and learn from it.
2. **Reuse proven environment technology selectively:** keep a small modular library of generators, materials, props and effects that demonstrably contributes to the scene. No speculative engine framework.
3. **Other themes later:** establish their own palette, architecture and atmosphere while reusing useful technology, not blindly reskinning Solar Citadel.

A separate, approved desktop task supplies the actual scope and attached reference images. This document should not be treated as a standing request to build environments in the background.

## References

- Unity 6: [Rendering 3D GameObjects in 2D URP scenes](https://docs.unity.com/en-us/engine/6000.6/manual/unity2d/2d-urp/2d-renderer-urp-features/shader-compatibility)
- Unity 6: [2D-compatible Shader Graph and materials](https://docs.unity.com/en-us/engine/6000.0/manual/unity2d/2d-urp/2d-index/prep-shader)
