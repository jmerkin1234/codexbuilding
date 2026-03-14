# CodexBuilding

Portable custom billiards physics in pure C#, with Godot 4.6 used only as a viewer and gameplay adapter.

## Non-Negotiables

- The physics engine lives in portable C# and does not depend on Godot physics.
- The open Blender scene is the source of truth for hardcoded table measurements and object names.
- The imported table is render-only. Gameplay cushions, pockets, and ball motion come from code.
- Jump shots are out of scope.
- `README.md`, `MODEL_REFERENCE.md`, `HARDCODE_REFERENCE.md`, `PLAN.md`, and `AGENT.md` are updated whenever architecture or table-reference facts change.

## Current State

- Fresh repository scaffolded from scratch.
- `CodexBuilding.Billiards.Core` created as the portable simulation library target.
- `CodexBuilding.Billiards.Tests` created for standalone deterministic tests.
- `godot/` created as the Godot 4.6 adapter/viewer project root.
- Initial hardcoded table spec seeded from the live Blender scene at `/home/justin/Desktop/customtable_9ft.blend`.
- `Godot.NET.Sdk 4.6.0` is now resolving and the Godot adapter builds on this machine.
- The fixed-step shell is now implemented with an accumulator, explicit phase tracking, and settle detection.
- Cue strike input now resolves normalized aim, clamped tip offsets, initial cue-ball velocity, and initial spin seeds.
- Current motion is intentionally minimal: constant-velocity integration only. Cloth friction, spin, collisions, and pockets are not implemented yet.
- Standalone verification currently covers `12` passing tests across the fixed-step shell, table spec, and cue-strike seeding.

## Repository Layout

- `src/CodexBuilding.Billiards.Core`
  Portable data types and simulation shell.
- `tests/CodexBuilding.Billiards.Tests`
  Standalone tests that do not require Godot.
- `godot`
  Godot 4.6 viewer/adaptor project. Rendering and input live here later; physics authority stays in the core library.
- `MODEL_REFERENCE.md`
  Blender-driven names and measurements used to hardcode geometry.
- `HARDCODE_REFERENCE.md`
  The exact compile-time values currently baked into the portable engine.
- `PLAN.md`
  The ordered implementation tracker used to prevent scope drift.
- `AGENT.md`
  Project constraints and execution order.

## Ordered Build Plan

1. Extract Blender model measurements and preserve source object names.
2. Hardcode the table spec in portable C#.
3. Build the fixed-step simulation shell.
4. Implement cue strike and spin.
5. Implement cloth motion.
6. Implement ball-ball collisions.
7. Implement cushion and pocket-jaw interactions.
8. Implement pocket capture and shot event logging.
9. Add deterministic standalone tests.
10. Add the Godot 4.6 visual adapter and gameplay controls.
11. Layer full 8-ball rules on top of physics.

## First Hardcoded Facts

- Cloth/slate bounds from `Tableslate`: `2.54m x 1.27m`
- Ball diameter from `CueBall`: `0.05715m`
- Six rail segments are present in the Blender scene:
  `rail_head`, `rail_foot`, `rail_upper_left`, `rail_upper_right`, `rail_bottom_left`, `rail_bottom_right`

## Next Step

The next implementation step is the cloth motion model: sliding, rolling, skid-to-roll transition, and spin decay.
