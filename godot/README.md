# Godot 4.6 Adapter

This folder is the Godot 4.6 front end for the portable billiards engine.

Rules:

- Godot renders and handles player input.
- Godot does not own physics.
- The portable core library remains authoritative for simulation.
- The imported Blender table is render-only.

Current behavior:

- `Main.cs` creates a live `SimulationWorld` from the portable core and seeds it with `StandardEightBallRack`.
- Runtime node names preserve the Blender-facing names where they are used: `GodotRoot`, `TableRoot`, `BallsRoot`, `CueRoot`, `CueStick`, `CueBall`, `Ball_01` through `Ball_15`, rail names, and pocket names.
- If `res://art/ImportedTable.tscn` exists, it is instantiated under `TableRoot`.
- If that imported scene is absent, the adapter renders a procedural fallback using the hardcoded table spec and Blender-derived source names.
- Ball motion is always driven by the portable core and mirrored into Godot transforms each frame.
- The adapter captures live shot traces and resolves them through the portable `Rules` layer.
- `Tab` switches between `EightBall` and `Training` mode inside the running adapter.
- The HUD now shows mode, current player, group assignment, ball-in-hand, winner, and recent rules outcomes.
- Cue-ball-in-hand and training placement are handled in the adapter with arrow-key repositioning, while the portable core remains the physics authority.
- Predictive guide meshes are generated from cloned portable simulations, including the primary cue path, post-bounce/post-collision cue continuation, and first-contact object-ball path.
- Practice mode supports freeplay layout editing by cycling the selected ball and moving it around the cloth.
- `H` toggles a hardcoded-table overlay sourced from `TableSpec` that shows cloth bounds, cushion segments, jaw segments, pocket capture circles, and cue/rack reference spots.
- `F1` toggles a debug panel with live portable-engine data such as `SimulationConfig` values, world counters, cue-ball state, selected-ball state, moving-ball counts, and preview lengths. Debug mode also forces the hardcoded-table overlay visible.

Keyboard controls:

- `Tab`: toggle between 8-ball and training mode
- `F1`: toggle debug mode and engine-data panel
- `H`: show or hide the hardcoded-table overlay
- `A/D`: aim left/right
- `W/S`: raise/lower strike speed
- `J/L`: apply left/right english
- `I/K`: apply follow/draw
- `Arrow keys`: move the cue ball when ball-in-hand or training placement is active
- `Z/X`: cycle the selected practice-layout ball in training mode
- `Space`: shoot
- `Backspace`: center the tip offset
- `R`: reset the standard rack
