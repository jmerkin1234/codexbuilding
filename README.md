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
- Cloth motion now handles sliding, rolling, skid-to-roll transition, forward-spin matching, moving side-spin scrub, and a modest deterministic side-spin path bias.
- Ball-ball collisions now resolve equal-mass contacts with restitution, overlap separation, capped tangential transfer, and side-spin exchange during contact.
- Cushion and pocket-jaw interactions now resolve against explicit hardcoded boundary segments derived from the Blender table reference, including tangential rail friction and side-spin rail response.
- Pocket capture now uses a hardcoded pocket-mouth/drop model derived from jaw geometry, so pocket acceptance depends on entry lane and speed instead of a single radius check.
- Pocket capture now also has a shelf-like slow-speed lip rule, so very slow edge-hangers can stay up while equally slow center-line rollers still fall.
- Rail rebound tuning now distinguishes glancing versus head-on cushion impacts, preserves most tangential bank travel through a separate retention term, and no longer applies fake rail scrub when a ball is only being pushed out of overlap.
- Rail english is now driven by a separate transfer term, so opposite side spin produces meaningfully different rail throw instead of only a small tangential nudge.
- Ball-object collisions now convert a controlled amount of forward/back spin into immediate post-contact follow/draw carry-through, so the cue ball can continue or pull back after contact instead of losing that behavior once it nearly stops.
- Shot event expansion now covers first cue-ball contact, cushion/jaw contact, pocketed balls, scratch, and settled-shot events.
- Deterministic replay now records per-step frames, cue-strike seeds, and shot events into a portable trace object.
- Regression coverage now locks a canonical straight-shot SHA-256 fingerprint for deterministic replay validation and was updated on `2026-03-14` after the follow/draw carry-through pass.
- The Godot 4.6 adapter now seeds a standard 8-ball rack from the portable core, mirrors core ball state into named visual nodes, exposes keyboard shot controls, directly instantiates `res://art/customtable_9ft.blend` as the render-only table when present, and falls back to a procedural table if that Blender asset is not available.
- The checked-in Blender table now imports directly through Godot 4.6, which also extracted the referenced texture set into `godot/art/textures/` so the visual asset can load without a manual export step.
- The Godot scene graph now preserves Blender-facing names where used: `GodotRoot`, `TableRoot`, `BallsRoot`, `CueRoot`, `CueStick`, `CueBall`, `Ball_01` through `Ball_15`, rail names, and pocket names.
- A portable rules layer now resolves 8-ball turns from replay traces, including break legality, open-table group assignment, foul detection, ball-in-hand, legal 8-ball win/loss, and configurable 8-ball-on-break handling.
- Training mode now exists as a separate portable rules path with free cue-ball repositioning and optional 8-ball respot flow for freeplay layouts.
- The Godot adapter now records live shot traces, resolves them through the portable rules layer, supports `Tab` switching between 8-ball versus computer and freeplay, exposes cue-ball-in-hand placement with arrow keys, and shows mode/rules state directly in the HUD.
- The Godot adapter now renders predictive aim guides from cloned portable simulations: a primary cue line, a post-bounce or post-collision cue continuation line, and an object-ball line after first contact.
- Freeplay now supports free layout adjustment with `Z/X` ball selection and arrow-key movement for the selected ball.
- The Godot adapter now includes a toggleable hardcoded-table overlay that draws the cloth bounds, cushion segments, jaw segments, pocket capture circles, and cue/rack reference spots directly from `TableSpec`.
- The hardcoded-table overlay now has per-layer toggles for cloth, cushions, jaws, pockets, and reference spots so geometry inspection is no longer all-or-nothing.
- The Godot adapter now starts in an orthographic top-down inspection view and still supports switching plus runtime zoom across broadcast, top-down, foot-rail, and side-rail camera presets without changing the core simulation.
- The Godot HUD is now framed into dedicated status and debug panels with wrapped text, so the live engine readout is easier to inspect while playing.
- The Godot HUD now also includes a dedicated last-shot summary panel that turns portable rule summaries into readable eight-ball and training/freeplay shot breakdowns.
- Training/freeplay now gives the selected layout ball a pulsing ring marker in the 3D view so placement edits are easy to track without relying on HUD text.
- In 8-ball mode, Player 2 is now driven by a simple computer opponent that plans legal shots from cloned portable simulations, while freeplay stays human-controlled.
- The Godot adapter now raises a transient shot-feedback banner for shot starts, first contact, pocketed balls, scratches, fouls, wins, and other key rule outcomes.
- The status panel now gives the current mode, turn, winner, and ball-in-hand state a dedicated color-accented header so match flow is readable at a glance.
- `F1` now enables a Godot debug panel that shows live portable-engine data, including table/config values, simulation counters, cue-ball state, selected-ball state, moving-ball counts, preview status, and live tuning state. Debug mode also forces the hardcoded-table overlay visible.
- The Godot adapter now supports runtime debug tuning of core-physics constants such as cloth friction, spin decay, side-spin drift, ball/rail restitution, tangential transfer, ball follow/draw carry, glancing rail restitution, tangential rail retention, rail english transfer, and solver iteration counts, while preserving the current ball layout between changes.
- The Godot adapter now uses a split shot-speed envelope tuned for feel: regular shots clamp to `0.3-5.0 m/s`, eight-ball break shots clamp to `0.3-8.0 m/s`, the default player speed remains `2.2 m/s`, and the computer opponent now samples a separate harder break-speed set instead of using regular-shot speeds for every turn.
- The Godot HUD now has a dedicated shot-setup card with speed, tip-offset, and tuning readouts, plus a separate controls/help card toggled with `F6`, so the main status panel no longer has to carry the full control map as raw text.
- The Godot adapter now opens on a proper start/menu overlay with button-based `EightBall` and `FreePlay` selection, and `Esc` reopens that menu later for resume/reset/return-to-start actions.
- Validation on `2026-03-14` covers `54` passing standalone tests via `dotnet test`, a successful Godot adapter compile via `dotnet build`, a successful Godot 4.6 Mono `--build-solutions` pass, a clean headless startup pass via `--quit-after 10`, and a verified direct import of `godot/art/customtable_9ft.blend` through Godot’s Blender pipeline.

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
14. Continue presentation polish, live tuning, and tune previously simplified physics.

## First Hardcoded Facts

- Cloth/slate bounds from `Tableslate`: `2.54m x 1.27m`
- Ball diameter from `CueBall`: `0.05715m`
- Six rail segments are present in the Blender scene:
  `rail_head`, `rail_foot`, `rail_upper_left`, `rail_upper_right`, `rail_bottom_left`, `rail_bottom_right`

## Next Step

The next implementation step remains broader pocket-behavior and overall feel tuning, now that the first slow-speed lip-hang pass is in alongside pocket-mouth behavior, base rail rebound tuning, stronger rail english, follow/draw carry-through, and a more realistic split between regular-shot and break-shot power in the Godot adapter.
