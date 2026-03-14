# Hardcode Reference

This file tracks the compile-time geometry and seed constants that the portable engine is allowed to bake into code.

## Purpose

- `MODEL_REFERENCE.md` documents raw Blender facts.
- `HARDCODE_REFERENCE.md` documents the subset of those facts that are intentionally compiled into the engine.

## Active Table Spec

- Code type: `CustomTable9FtSpec`
- Source blend: `/home/justin/Desktop/customtable_9ft.blend`
- Table name in code: `customtable_9ft`

## Hardcoded Play Surface

- Cloth minimum: `(-1.2699999, -0.63499993)`
- Cloth maximum: `(1.2699999, 0.63499993)`
- Ball diameter: `0.05715`
- Cue ball spawn seed: `(-0.733902, 0.002383)`
- Rack apex seed: `(0.616941, 0.0)`

## Hardcoded Cushion Seeds

- `rail_head`
  start: `(-1.2249266, -0.56977034)`
  end: `(-1.2249266, 0.57038474)`
- `rail_foot`
  start: `(1.2258036, -0.5681504)`
  end: `(1.2258036, 0.5702276)`
- `rail_upper_left`
  start: `(-1.2068906, -0.59292513)`
  end: `(-0.050392687, -0.59292513)`
- `rail_upper_right`
  start: `(0.050399005, -0.59292495)`
  end: `(1.2069001, -0.59292495)`
- `rail_bottom_left`
  start: `(-1.2068906, 0.5953581)`
  end: `(-0.050392687, 0.5953581)`
- `rail_bottom_right`
  start: `(0.050398767, 0.5953247)`
  end: `(1.2068999, 0.5953247)`

## Hardcoded Pocket Seeds

- `pocket_TL1`
  kind: `Corner`
  center: `(1.249117, 0.61784637)`
  capture radius seed: `0.0584`
  mouth center: `(1.21635175, 0.58277615)`
  mouth half-width: `0.01570998`
  drop radius: `0.0405`
  max entry speed: `1.15`
- `pocket_BL2`
  kind: `Corner`
  center: `(1.2508553, -0.6167401)`
  capture radius seed: `0.0584`
  mouth center: `(1.21635185, -0.58053768)`
  mouth half-width: `0.01558140`
  drop radius: `0.0405`
  max entry speed: `1.15`
- `pocket_BM3`
  kind: `Side`
  center: `(0.0000029, -0.66143388)`
  capture radius seed: `0.0614`
  mouth center: `(0.00000316, -0.59292504)`
  mouth half-width: `0.05039585`
  drop radius: `0.044`
  max entry speed: `1.0`
- `pocket_BR4`
  kind: `Corner`
  center: `(-1.2508456, -0.6167401)`
  capture radius seed: `0.0584`
  mouth center: `(-1.2159086, -0.58134774)`
  mouth half-width: `0.01467516`
  drop radius: `0.0405`
  max entry speed: `1.15`
- `Pocket_TR5`
  kind: `Corner`
  center: `(-1.2508456, 0.61673915)`
  capture radius seed: `0.0584`
  mouth center: `(-1.2159086, 0.58287142)`
  mouth half-width: `0.01540265`
  drop radius: `0.0405`
  max entry speed: `1.15`
- `Pocket_TM6`
  kind: `Side`
  center: `(0.0000029, 0.66556805)`
  capture radius seed: `0.0614`
  mouth center: `(0.00000304, 0.5953414)`
  mouth half-width: `0.05039573`
  drop radius: `0.044`
  max entry speed: `1.0`

## Hardcoded Jaw Derivation

Jaw segments are now part of the compile-time table spec.

- Corner jaw apex offset toward cloth center: `0.03`
- Side jaw apex offset toward cloth center: `0.031`
- Total derived jaw segments: `12`

Derived jaw names:

- `pocket_BR4_jaw_vertical`
- `pocket_BR4_jaw_horizontal`
- `pocket_BL2_jaw_vertical`
- `pocket_BL2_jaw_horizontal`
- `Pocket_TR5_jaw_vertical`
- `Pocket_TR5_jaw_horizontal`
- `pocket_TL1_jaw_vertical`
- `pocket_TL1_jaw_horizontal`
- `pocket_BM3_jaw_left`
- `pocket_BM3_jaw_right`
- `Pocket_TM6_jaw_left`
- `Pocket_TM6_jaw_right`

Each jaw segment is derived from:

- one adjacent hardcoded rail endpoint
- the matching hardcoded pocket center
- an inward offset from pocket center toward cloth center

The resulting derived segments are compiled into `TableSpec.JawSegments`.

## Hardcoded Pocket Mouth Model

Pocket capture is no longer a single circle check.

- Mouth centers are derived from the midpoint between the two relevant jaw rail endpoints.
- Mouth half-widths are derived from half the distance between those two jaw start points.
- Balls are always accepted inside the `drop radius`.
- Balls inside the pocket funnel are only accepted when lane alignment and entry speed fit the hardcoded mouth model.

## Rules

- This file changes only when a code-facing seed value changes.
- If Blender source facts change without a code change, update `MODEL_REFERENCE.md` first.
- If code constants change, update both this file and the code in the same commit.
