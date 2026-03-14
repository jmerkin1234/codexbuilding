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
- The portable `Rules` layer and training-mode state now exist in the core, but they are not yet wired into the Godot HUD or turn-flow code.

Keyboard controls:

- `A/D`: aim left/right
- `W/S`: raise/lower strike speed
- `J/L`: apply left/right english
- `I/K`: apply follow/draw
- `Space`: shoot
- `Backspace`: center the tip offset
- `R`: reset the standard rack
