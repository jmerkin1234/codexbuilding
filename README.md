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
- Cloth motion now handles sliding, rolling, skid-to-roll transition, and passive spin decay in straight-line travel.
- Ball-ball collisions now resolve equal-mass contacts with restitution and overlap separation.
- Cushion and pocket-jaw interactions now resolve against explicit hardcoded boundary segments derived from the Blender table reference.
- Pocket capture now marks balls as pocketed, zeros their motion, and emits `Pocketed` or `Scratch` events.
- Shot event expansion now covers first cue-ball contact, cushion/jaw contact, pocketed balls, scratch, and settled-shot events.
- Deterministic replay now records per-step frames, cue-strike seeds, and shot events into a portable trace object.
- Regression coverage now locks a canonical straight-shot SHA-256 fingerprint for deterministic replay validation.
- The Godot 4.6 adapter now seeds a standard 8-ball rack from the portable core, mirrors core ball state into named visual nodes, exposes keyboard shot controls, and falls back to a procedural table when `res://art/ImportedTable.tscn` is not present.
- The Godot scene graph now preserves Blender-facing names where used: `GodotRoot`, `TableRoot`, `BallsRoot`, `CueRoot`, `CueStick`, `CueBall`, `Ball_01` through `Ball_15`, rail names, and pocket names.
- A portable rules layer now resolves 8-ball turns from replay traces, including break legality, open-table group assignment, foul detection, ball-in-hand, legal 8-ball win/loss, and configurable 8-ball-on-break handling.
- Training mode now exists as a separate portable rules path with free cue-ball repositioning and optional 8-ball respot flow for practice layouts.
- The Godot adapter now records live shot traces, resolves them through the portable rules layer, supports `Tab` switching between 8-ball and training, exposes cue-ball-in-hand placement with arrow keys, and shows mode/rules state directly in the HUD.
- The Godot adapter now renders predictive aim guides from cloned portable simulations: a primary cue line, a post-bounce or post-collision cue continuation line, and an object-ball line after first contact.
- Practice mode now supports free layout adjustment with `Z/X` ball selection and arrow-key movement for the selected ball.
- Validation on `2026-03-14` covers `38` passing standalone tests via `dotnet test` plus a successful Godot adapter compile via `dotnet build`. Godot runtime launch was not executed in this environment because no Godot CLI/editor binary is available here.

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
12. Wire rules and training-mode flow into the Godot adapter and HUD.
13. Expand training layout tools and richer in-game presentation.
14. Continue presentation polish and scenario tooling.

## First Hardcoded Facts

- Cloth/slate bounds from `Tableslate`: `2.54m x 1.27m`
- Ball diameter from `CueBall`: `0.05715m`
- Six rail segments are present in the Blender scene:
  `rail_head`, `rail_foot`, `rail_upper_left`, `rail_upper_right`, `rail_bottom_left`, `rail_bottom_right`

## Next Step

The next implementation step is continued presentation polish and scenario tooling on top of the working rules-aware adapter.
