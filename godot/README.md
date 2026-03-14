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
- `1` through `5` now toggle the cloth, cushion, jaw, pocket, and spot overlay sublayers independently.
- The adapter now starts in an orthographic top-down main camera preset, and `C` still cycles between broadcast, top-down, foot-rail, and side-rail camera presets while `Q/E` zoom the active preset in and out.
- The HUD now uses dedicated framed status and debug panels instead of raw overlay labels.
- A transient banner now surfaces shot starts, contact, pocketing, scratch, foul, win, and turn/result feedback in the running adapter.
- The status panel now has a color-accented header for current mode and turn state.
- `F1` toggles a debug panel with live portable-engine data such as `SimulationConfig` values, world counters, cue-ball state, selected-ball state, moving-ball counts, and preview lengths. Debug mode also forces the hardcoded-table overlay visible.

Verification on `2026-03-14`:

- `dotnet build CodexBuilding.Billiards.Godot46.csproj --no-restore`
- `dotnet test ../tests/CodexBuilding.Billiards.Tests/CodexBuilding.Billiards.Tests.csproj --no-restore` with `38/38` passing
- Godot 4.6 Mono `--build-solutions --quit`
- Godot 4.6 Mono headless startup `--quit-after 10`

Keyboard controls:

- `Tab`: toggle between 8-ball and training mode
- `F1`: toggle debug mode and engine-data panel
- `H`: show or hide the hardcoded-table overlay
- `1`: toggle cloth overlay lines
- `2`: toggle cushion overlay lines
- `3`: toggle jaw overlay lines
- `4`: toggle pocket overlay lines
- `5`: toggle cue/rack spot overlay lines
- `C`: cycle camera preset
- `Q/E`: zoom camera in/out
- `A/D`: aim left/right
- `W/S`: raise/lower strike speed
- `J/L`: apply left/right english
- `I/K`: apply follow/draw
- `Arrow keys`: move the cue ball when ball-in-hand or training placement is active
- `Z/X`: cycle the selected practice-layout ball in training mode
- `Space`: shoot
- `Backspace`: center the tip offset
- `R`: reset the standard rack
