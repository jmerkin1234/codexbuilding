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

The repository now has a working portable core scaffold, a fixed-step simulation shell with explicit shot phases, cue-strike input seeding for velocity and spin, a cloth-motion layer for straight-line travel, equal-mass ball-ball collision resolution, hardcoded cushion and pocket-jaw boundary interaction, pocket capture with shot-event emission, deterministic replay trace capture with a locked straight-shot regression signature, a standard 8-ball opening rack factory, a portable 8-ball rules layer, a portable training-mode rules path, and a Godot 4.6 adapter that compiles and mirrors portable-core ball state into a controllable visual scene. The next implementation step is wiring rules and training-mode flow into the Godot adapter.

## Progress Log

- 2026-03-14: Step 9 completed. Added `SimulationReplayTrace`, `SimulationReplayFrame`, `SimulationReplayRunner`, and `SimulationFingerprintBuilder`; locked the canonical straight-shot replay signature; verified standalone coverage at `27/27` passing tests with `dotnet test`.
- 2026-03-14: Step 10 completed. Added `StandardEightBallRack`, expanded standalone coverage to `30/30` passing tests, and replaced the Godot stub with a real adapter that builds a named scene graph, supports keyboard shot control, syncs ball visuals from `SimulationWorld`, and falls back to a procedural table when no imported scene is present. Verified with `dotnet test` and `dotnet build`; Godot runtime launch was not executed here because no Godot CLI/editor binary is available in this environment.
- 2026-03-14: Step 11 completed. Added a portable `Rules` layer with replay-trace shot summaries, 8-ball turn resolution, foul and break handling, ball-in-hand, configurable 8-ball-on-break behavior, and a separate training-mode state path. Expanded standalone coverage to `38/38` passing tests and reverified the Godot adapter build with `dotnet build`.
