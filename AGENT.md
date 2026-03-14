# Agent Notes

This repository is built under the following constraints.

## Project Goal

Build a portable custom billiards physics engine in pure C# that can run standalone and be adapted to Godot 4.6 without locking gameplay to Godot.

## Hard Constraints

- Do not reuse old project gameplay files unless the user explicitly names them.
- Keep the physics engine portable and renderer-agnostic.
- Godot 4.6 is an adapter and viewer, not the physics authority.
- The Blender scene currently open through MCP is the source for hardcoded table geometry and source object names.
- Keep object names used from Blender exactly documented in `MODEL_REFERENCE.md`.
- Maintain `README.md`, `MODEL_REFERENCE.md`, `HARDCODE_REFERENCE.md`, `PLAN.md`, and `AGENT.md` as the code evolves.
- Push to GitHub incrementally as milestones land.
- Do not jump ahead in implementation order.

## Ordered Execution

1. Extract and document Blender measurements and names.
2. Hardcode the table specification in portable C#.
3. Build the deterministic fixed-step simulation shell.
4. Implement cue strike and spin.
5. Implement rolling, sliding, and cloth friction.
6. Implement ball-ball collisions.
7. Implement cushion, jaw, and pocket interactions.
8. Implement shot event capture and settle detection.
9. Add standalone deterministic tests.
10. Add the Godot 4.6 adapter.
11. Add full 8-ball rules and game flow.

## Naming

- Preserve Blender names in documentation exactly.
- Use explicit mappings in code when a cleaner internal identifier is needed.
- Current verified six-rail names:
  `rail_head`, `rail_foot`, `rail_upper_left`, `rail_upper_right`, `rail_bottom_left`, `rail_bottom_right`

## Current Scope

The repository now has a working portable core scaffold, a fixed-step simulation shell with explicit shot phases, cue-strike input seeding for velocity and spin, a cloth-motion layer for straight-line travel, equal-mass ball-ball collision resolution, hardcoded cushion and pocket-jaw boundary interaction, pocket capture with shot-event emission, deterministic replay trace capture with a locked straight-shot regression signature, a standard 8-ball opening rack factory, a portable 8-ball rules layer, a portable training-mode rules path, and a Godot 4.6 adapter that records shot traces, resolves them through the portable rules engines, exposes rules/training state in framed HUD panels, renders predictive guide lines from cloned portable simulations, can now show the hardcoded table geometry as a toggleable overlay with per-layer visibility controls, boots into an orthographic top-down main camera while still supporting switchable camera presets with runtime zoom for inspection, shows transient shot-feedback banners for key events and rule outcomes, highlights current turn state in a dedicated color-accented header, and keeps a dedicated last-shot summary panel for completed eight-ball and training/freeplay shots. The next implementation step remains broader presentation polish.

## Progress Log

- 2026-03-14: Step 9 completed. Added `SimulationReplayTrace`, `SimulationReplayFrame`, `SimulationReplayRunner`, and `SimulationFingerprintBuilder`; locked the canonical straight-shot replay signature; verified standalone coverage at `27/27` passing tests with `dotnet test`.
- 2026-03-14: Step 10 completed. Added `StandardEightBallRack`, expanded standalone coverage to `30/30` passing tests, and replaced the Godot stub with a real adapter that builds a named scene graph, supports keyboard shot control, syncs ball visuals from `SimulationWorld`, and falls back to a procedural table when no imported scene is present. Verified with `dotnet test` and `dotnet build`; Godot runtime launch was not executed here because no Godot CLI/editor binary is available in this environment.
- 2026-03-14: Step 11 completed. Added a portable `Rules` layer with replay-trace shot summaries, 8-ball turn resolution, foul and break handling, ball-in-hand, configurable 8-ball-on-break behavior, and a separate training-mode state path. Expanded standalone coverage to `38/38` passing tests and reverified the Godot adapter build with `dotnet build`.
- 2026-03-14: Step 12 completed. Wired the Godot adapter into the portable rules engines by capturing live replay traces, resolving 8-ball and training-mode shots in `Main.cs`, exposing mode switching and cue-ball-in-hand placement controls, and surfacing rules state in the HUD. Reverified with `dotnet build` for the Godot adapter and `dotnet test` for `38/38` passing standalone tests.
- 2026-03-14: Step 13 completed. Expanded practice presentation in the Godot adapter with predictive cue/object guide lines driven by cloned portable simulations, plus freeplay ball selection and placement controls for training layouts. Reverified with `dotnet build` for the adapter and `dotnet test` for `38/38` passing standalone tests.
- 2026-03-14: Step 14 started. Added a Godot-side hardcoded-table overlay sourced from `TableSpec` for cloth bounds, cushions, jaws, pockets, and spawn/reference spots, with a runtime toggle. Reverified with `dotnet build` for the adapter and `dotnet test` for `38/38` passing standalone tests.
- 2026-03-14: Step 14 continued. Added a Godot debug mode that surfaces live portable-engine data in a second HUD panel and forces the hardcoded-table overlay visible for inspection. Reverified with `dotnet build` for the adapter and `dotnet test` for `38/38` passing standalone tests.
- 2026-03-14: Verification pass. Fixed a Godot adapter startup bug in `Main.BuildTableVisual()` by parenting procedural rail visuals before calling `LookAt()`. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Training-mode simplification. Removed the temporary preset/drill layer and kept training as freeplay only. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Step 14 continued. Split the hardcoded-table overlay into cloth, cushion, jaw, pocket, and spot sublayers with runtime hotkeys, and surfaced the live layer state in both the HUD and debug panel. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Step 14 continued. Added switchable Godot camera presets and runtime zoom controls for broadcast, top-down, foot-rail, and side-rail inspection views, with the active camera state reflected in the HUD and debug panel. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Step 14 continued. Reworked the Godot HUD into styled status and debug panels with wrapped text so the live state readout is more usable during play and inspection. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Step 14 continued. Added a transient Godot shot-feedback banner that reacts to shot starts, first contact, pocketed balls, scratches, fouls, wins, and other rule outcomes without changing the portable core. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Step 14 continued. Added a color-accented status header in the Godot HUD so training state, active player, winner state, and ball-in-hand stand out immediately instead of being buried in the body text. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Step 14 continued. Changed the Godot adapter so the main/default camera preset is now an orthographic top-down view, while the broadcast, foot-rail, and side-rail presets remain available through runtime cycling and zoom. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
- 2026-03-14: Step 14 continued. Added a dedicated Godot last-shot summary panel that turns completed eight-ball and training/freeplay rule results into readable shot breakdowns without changing the portable core. Reverified with `dotnet test` for `38/38` passing standalone tests, `dotnet build` for the Godot adapter, Godot 4.6 Mono `--build-solutions`, and a clean headless startup run via `--quit-after 10`.
