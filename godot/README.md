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
- `Main.tscn` now instances `res://art/customtable_9ft.blend` directly as `ImportedTableSource`, and the adapter binds to its imported `GodotRoot`, `TableRoot`, `BallsRoot`, and `CueRoot` nodes.
- If that Blender asset is absent or cannot be loaded, the adapter can still render a procedural fallback table from the hardcoded spec, but gameplay now requires the authored Blender `CueBall` and `Ball_01` through `Ball_15` meshes instead of spawning fallback spheres.
- The direct Blender import is now verified in this repo, and the imported asset material references were extracted under `godot/art/textures/` by Godot during the import process.
- Ball motion is always driven by the portable core and mirrored into Godot transforms each frame.
- Godot now also accumulates visible roll from mirrored ball travel plus side-spin yaw, so the authored Blender balls rotate instead of visually sliding.
- The authored Blender cue stick is now used during shot setup instead of hiding it behind the old placeholder cue mesh.
- The Godot project copy of `customtable_9ft.blend` now has `Tableslate` custom normals cleared and `Tableslate`, `Tableframe`, `CueStick`, and `rail_upper_right` triangulated so Godot can generate tangents and preserve the imported shading more faithfully.
- Godot now imports the ball textures lossless with mipmaps disabled, and the project render settings now keep 3D at native scale with TAA and screen-space AA disabled plus `MSAA 3D` enabled for a sharper standalone desktop result.
- The portable core now includes modest cloth side-spin drift/scrub, tangential spin transfer in ball-ball contact, controlled follow/draw carry-through after object contact, angle-aware rail rebound tuning, and a separate rail-english transfer term for stronger cushion spin response; Godot only mirrors the resulting state.
- The portable core now also uses a mouth/drop-based pocket model derived from hardcoded jaw geometry, with an added slow-speed lip-hang rule so edge-hangers can stay up while center-line rollers still fall.
- The adapter captures live shot traces and resolves them through the portable `Rules` layer.
- `Tab` switches between `EightBall` versus computer and `FreePlay` inside the running adapter.
- The HUD now shows mode, current player, group assignment, ball-in-hand, winner, and recent rules outcomes.
- Cue-ball-in-hand and freeplay placement are handled in the adapter with arrow-key repositioning, while the portable core remains the physics authority.
- Predictive guide meshes are generated from cloned portable simulations, including the primary cue path, post-bounce/post-collision cue continuation, and first-contact object-ball path.
- FreePlay supports layout editing by cycling the selected ball and moving it around the cloth.
- `H` toggles a hardcoded-table overlay sourced from `TableSpec` that shows cloth bounds, cushion segments, jaw segments, pocket capture circles, and cue/rack reference spots.
- `1` through `5` now toggle the cloth, cushion, jaw, pocket, and spot overlay sublayers independently.
- The adapter now starts in an orthographic top-down main camera preset, and `C` still cycles between broadcast, top-down, foot-rail, and side-rail camera presets while `Q/E` zoom the active preset in and out.
- The HUD now uses dedicated framed status and debug panels instead of raw overlay labels.
- The shot-setup panel now shows shot power as a percentage of the active speed cap in addition to the raw strike speed.
- The HUD now also keeps a dedicated last-shot summary panel for completed eight-ball and training/freeplay shots.
- The HUD now also has a dedicated shot-setup panel for aim/speed/tip information and a separate controls/help panel, so the main status card stays concise.
- `F7` now toggles the gameplay HUD cards and shot banner without affecting the menu overlay, so the table can be viewed with the HUD fully cleared.
- The playable Godot window now starts at `1920x1080` by default, while headless verification still runs at the CLI as before.
- Shot speed now uses a split feel-tuned envelope in the adapter: regular play clamps to `0.3-5.0 m/s`, eight-ball break shots clamp to `0.3-8.0 m/s`, and the shot-setup/debug readouts show the currently active cap.
- The adapter now opens on a proper menu/start overlay with button-based `EightBall` and `FreePlay` selection, and `Esc` reopens that menu later for resume/reset/return-to-start actions.
- Training/freeplay now marks the selected layout ball with a pulsing in-world ring instead of only a subtle scale change.
- In 8-ball, Player 2 is now driven by a simple computer opponent that also uses a separate harder break-speed sample set; FreePlay remains human-controlled.
- If the computer planner fails to produce a valid shot, the adapter now fails the turn forward by giving the opponent ball in hand instead of retrying the same broken turn forever.
- A transient banner now surfaces shot starts, contact, pocketing, scratch, foul, win, and turn/result feedback in the running adapter.
- The status panel now has a color-accented header for current mode and turn state.
- `F1` toggles a detached debug window with live portable-engine data such as `SimulationConfig` values, world counters, cue-ball state, selected-ball state, moving-ball counts, preview lengths, and active tuning state. That separate play-mode window can be moved to another monitor, and debug mode still forces the hardcoded-table overlay visible.
- The project now disables embedded subwindows so the debug view can become a native OS window; if you still use Godot editor embedded play, the editor itself can still keep the whole game trapped inside the editor pane.
- Debug mode now supports live tuning of key portable-physics constants, including ball follow/draw carry, glancing rail restitution, tangential rail retention, and rail-english transfer, and immediately rebuilds `SimulationWorld` with the current ball layout after each change.

Verification on `2026-03-14`:

- `dotnet build CodexBuilding.Billiards.Godot46.csproj --no-restore`
- `dotnet test ../tests/CodexBuilding.Billiards.Tests/CodexBuilding.Billiards.Tests.csproj --no-restore` with `56/56` passing
- Godot 4.6 Mono `--build-solutions --quit`
- Godot 4.6 Mono headless startup `--quit-after 5`
- Verified direct import and reimport of `godot/art/customtable_9ft.blend`

Keyboard controls:

- `Tab`: toggle between 8-ball vs computer and FreePlay
- `F1`: toggle the detached debug window and engine-data view
- `F2/F3`: select the active debug tuning parameter
- `F4/F5`: decrease or increase the selected tuning value
- `Shift` + `F4/F5`: coarse debug tuning adjustments
- `F6`: show or hide the controls/help panel
- `F7`: show or hide the gameplay HUD cards and shot banner
- `Esc`: open or close the start/pause menu
- `H`: show or hide the hardcoded-table overlay
- `1`: toggle cloth overlay lines
- `2`: toggle cushion overlay lines
- `3`: toggle jaw overlay lines
- `4`: toggle pocket overlay lines
- `5`: toggle cue/rack spot overlay lines
- `C`: cycle camera preset
- `Q/E`: zoom camera in/out
- `A/D`: aim left/right
- `W/S`: raise/lower strike speed within the active cap (`5.0 m/s` regular, `8.0 m/s` on the eight-ball break)
- `J/L`: apply left/right english
- `I/K`: apply follow/draw
- `Arrow keys`: move the cue ball when ball-in-hand or FreePlay placement is active
- `Z/X`: cycle the selected layout ball in FreePlay
- `Space`: shoot
- `Backspace`: center the tip offset
- `R`: reset the standard rack
