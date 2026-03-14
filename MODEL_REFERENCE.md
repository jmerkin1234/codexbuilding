# Model Reference

This file records the Blender scene facts that are allowed to drive hardcoded geometry in the portable physics engine.

## Source Scene

- Blender file: `/home/justin/Desktop/customtable_9ft.blend`
- Scene name: `Scene`
- Purpose: render reference only

## Object Naming Rule

The names below are preserved exactly as they appear in Blender. If cleaner names are introduced in code later, the mapping must be explicit and documented here.

## Key Scene Roots

- `GodotRoot`
- `TableRoot`
- `BallsRoot`
- `CueRoot`

## Table Visual Objects

- `Tableslate`
- `Tableframe`
- `framebottom`

## Rail Objects

The current Blender scene contains six rail segments:

- `rail_head`
- `rail_foot`
- `rail_upper_left`
- `rail_upper_right`
- `rail_bottom_left`
- `rail_bottom_right`

## Pocket Objects

- `pockett_TL1`
- `pocket_BL2`
- `pocket_BM3`
- `pocket_BR4`
- `Pocket_TR5`
- `Pocket_TM6`

## Ball and Cue Objects

- `CueBall`
- `Ball_01` through `Ball_15`
- `CueStick`

## Measured Values

All units are meters in Blender world space.

### Tableslate

- Source object: `Tableslate`
- World bounding box min: `(-1.2699998617, -0.6349999309, -0.1382045150)`
- World bounding box max: `(1.2699998617, 0.6349999309, 0.0)`
- Hardcoded cloth rectangle seed:
  `min = (-1.2699999, -0.6349999)`
  `max = (1.2699999, 0.6349999)`

### Tableframe

- Source object: `Tableframe`
- World bounding box min: `(-1.3960279226, -0.7530107498, -0.2260493040)`
- World bounding box max: `(1.3938320875, 0.7582436800, 0.0451114625)`

### CueBall

- Source object: `CueBall`
- World bounding box min: `(-0.7624769807, -0.0261920001, 0.0)`
- World bounding box max: `(-0.7053269744, 0.0309579987, 0.0571499988)`
- Diameter seed: `0.05715`
- Spawn seed from object origin: `(-0.7339020, 0.0023830)`

### Rack Apex

- Source object: `Ball_01`
- Apex seed from object origin: `(0.6169410, 0.0)`

### Rail Nose Seeds

The inner face of each rail bounding box is used as the initial hardcoded cushion-nose seed. These are seed values only; later refinement can replace them with explicit jaw and nose extraction.

- `rail_head`
  inner x: `-1.2249265909`
  y span: `-0.5697703362` to `0.5703847408`
- `rail_foot`
  inner x: `1.2258036137`
  y span: `-0.5681504011` to `0.5702276230`
- `rail_upper_left`
  inner y: `-0.5929251313`
  x span: `-1.2068905830` to `-0.0503926873`
- `rail_upper_right`
  inner y: `-0.5929249525`
  x span: `0.0503990054` to `1.2069001198`
- `rail_bottom_left`
  inner y: `0.5953580737`
  x span: `-1.2068905830` to `-0.0503926873`
- `rail_bottom_right`
  inner y: `0.5953246951`
  x span: `0.0503987670` to `1.2068998814`

### Pocket Seeds

Pocket centers are currently seeded from the Blender object origins.

- `pockett_TL1`: `(1.2640905, 0.4908944)`
- `pocket_BL2`: `(1.2508553, -0.6167401)`
- `pocket_BM3`: `(0.0000029, -0.6660219)`
- `pocket_BR4`: `(-1.2508456, -0.6167401)`
- `Pocket_TR5`: `(-1.2508456, 0.6167392)`
- `Pocket_TM6`: `(0.0000029, 0.6655681)`

### Derived Jaw Layout

Jaw collision does not come from mesh data at runtime. It is derived from the Blender-backed rail and pocket seeds.

- Corner jaws are built from the adjacent rail endpoint and a point `0.03m` inward from the pocket center toward the cloth center.
- Side jaws are built from the adjacent rail endpoint and a point `0.031m` inward from the pocket center toward the cloth center.
- The current compile-time layout produces `12` jaw segments total.

### Derived Pocket Mouth Layout

Pocket acceptance is now also derived from the Blender-backed jaw and pocket seeds.

- Mouth centers are compiled from the midpoint between the two relevant jaw-start rail endpoints for each pocket.
- Mouth half-widths are compiled from half the distance between those jaw-start points.
- The portable engine now uses those mouth values together with hardcoded drop radii and speed limits to decide accept vs reject near the jaws.

## Notes

- `pockett_TL1` contains a double `t` in the source name. That typo is preserved intentionally.
- `pockett_TL1` is not currently symmetric with the other corner pocket origins. The portable engine should treat this as a model fact until we explicitly replace it with refined jaw extraction.
- The engine must not read collision from the mesh at runtime. These measurements are compile-time reference values only.
