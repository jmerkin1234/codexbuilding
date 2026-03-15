# Model Reference

This file records the Blender scene facts that are allowed to drive hardcoded geometry in the portable physics engine.

## Source Scene

- Blender file: `/home/justin/Desktop/MASTERtable_9ft.blend`
- Geometry note: `/home/justin/Downloads/table_geometry_reference (1).md`
- Scene name: `Scene`
- Purpose: render-only source plus compile-time measurement reference
- Convention from the supplied geometry note:
  `+X = foot end`
  `-X = head end`
  `+Y = right rail`
  `-Y = left rail`
  `+Z = up`

## Object Naming Rule

The names below are preserved exactly as they appear in Blender. When the portable engine keeps older internal pocket IDs for rules/test stability, the mapping is documented explicitly instead of being guessed.

## Key Scene Roots

- `GodotRoot`
- `TableRoot`
- `BallsRoot`
- `CueRoot`

## Table Visual Objects

- `Tableslate`
- `Tableframe`
- `framebottom`
- `TableOverlay`

## Rail Objects

- `rail_head`
- `rail_foot`
- `rail_upper_left`
- `rail_upper_right`
- `rail_bottom_left`
- `rail_bottom_right`

## Pocket Objects In Blender

- `pocket_FRC`
- `pocket_FLC`
- `pocket_LS`
- `pocket_HLC`
- `pocket_HRC`
- `pocket_RS`

## Legacy Internal Pocket-ID Mapping

The portable engine still uses its older stable pocket IDs in code and tests. Those IDs now map to the new Blender pocket objects like this:

- `pocket_TL1` -> Blender `pocket_FRC`
- `pocket_BL2` -> Blender `pocket_FLC`
- `pocket_BM3` -> Blender `pocket_LS`
- `pocket_BR4` -> Blender `pocket_HLC`
- `Pocket_TR5` -> Blender `pocket_HRC`
- `Pocket_TM6` -> Blender `pocket_RS`

## Ball And Cue Objects

- `CueBall`
- `Ball_01` through `Ball_15`
- `CueStick`

## Ground Truth Measurements

All units are meters in Blender world space.

### Tableslate

- Source object: `Tableslate`
- Half-length: `1.2699999`
- Half-width: `0.6349999`
- Play-area rect min: `(-1.2699999, -0.6349999)`
- Play-area rect max: `(1.2699999, 0.6349999)`
- Z bottom: `-0.1382042`
- Z top: `0.0008492`
- Playing-surface Z bottom: `-0.016741`
- Playing-surface Z top: `0.0`

### Spots

- Foot spot / rack apex: `(0.6349999500, 0.0)`
- Head spot: `(-0.6349999500, 0.0)`

### CueBall

- Diameter: `0.05715`
- Radius: `0.028575`
- Spawn seed from object origin: `(-0.7339019775, 0.0023830000)`

### CueStick

- Tip origin X: `-0.7943970561`
- Tip origin Y: `0.0000000000`
- Tip origin Z: `0.0285750000`

### Rail Nose Seeds

These are the actual inner-face mesh measurements from the supplied geometry note. They are source facts only. Runtime collision still comes from the hardcoded spec, not from the mesh.

- `rail_head`
  inner x: `-1.2249265909`
  y span: `-0.5140137672` to `0.5146253705`
- `rail_foot`
  inner x: `1.2258036137`
  y span: `-0.5123929977` to `0.5144737959`
- `rail_upper_left`
  inner y: `-0.5934566259`
  x span: `-1.1487218142` to `-0.0622503757`
- `rail_upper_right`
  inner y: `-0.5929293036`
  x span: `0.0622566938` to `1.1487314701`
- `rail_bottom_left`
  inner y: `0.5953580737`
  x span: `-1.1487218142` to `-0.0622503757`
- `rail_bottom_right`
  inner y: `0.5953246951`
  x span: `0.0622564554` to `1.1487312317`

### Pocket Seeds

#### Blender Object Origins

- `pocket_FRC`: `(1.2491170, 0.6178464, -0.0447360)`
- `pocket_FLC`: `(1.2508553, -0.6167401, -0.0447360)`
- `pocket_LS`: `(0.0000029, -0.6614339, -0.0447360)`
- `pocket_HLC`: `(-1.2508456, -0.6167401, -0.0447360)`
- `pocket_HRC`: `(-1.2508456, 0.6167392, -0.0447370)`
- `pocket_RS`: `(0.0000029, 0.6655681, -0.0447370)`

#### Mesh-Top Centers

- `pocket_FRC`: center `(1.2491073608, 0.6177103221)`, top radius `0.0264515`
- `pocket_FLC`: center `(1.2508456707, -0.6166017950)`, top radius `0.0264508`
- `pocket_LS`: center `(0.0000029057, -0.6650000)`, top radius `0.0700000`
- `pocket_HLC`: center `(-1.2508408427, -0.6166017950)`, top radius `0.0264484`
- `pocket_HRC`: center `(-1.2508408427, 0.6166031063)`, top radius `0.0264491`
- `pocket_RS`: center `(0.0000029057, 0.6650000)`, top radius `0.0700000`

#### BCA Jaw Midpoints Used As A Hard Reference

These are the supplied idealized opening positions from the geometry note. They are reference targets, not the current runtime pocket-mouth implementation.

- `pocket_FRC`: `(1.2128499, 0.5778499)`
- `pocket_FLC`: `(1.2128499, -0.5778499)`
- `pocket_LS`: `(0.0, -0.6984999)`
- `pocket_HLC`: `(-1.2128499, -0.5778499)`
- `pocket_HRC`: `(-1.2128499, 0.5778499)`
- `pocket_RS`: `(0.0, 0.6984999)`

## Notes

- The portable engine still does not read collision from the mesh at runtime. These values are compile-time reference inputs only.
- The current code-facing play area still comes from the `Tableslate` rectangle, while actual rebound uses the separate hardcoded cushion segments.
- The Blender object names changed with `MASTERtable_9ft.blend`; this document reflects the actual Blender names and the explicit mapping back to the older internal pocket IDs.
