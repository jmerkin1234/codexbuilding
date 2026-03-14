# Godot 4.6 Adapter

This folder is the Godot 4.6 front end for the portable billiards engine.

Rules:

- Godot renders and handles player input.
- Godot does not own physics.
- The portable core library remains authoritative for simulation.
- The imported Blender table is render-only.

The initial scaffold only boots a `Node3D` and confirms that the hardcoded table spec can be loaded from the core library.
