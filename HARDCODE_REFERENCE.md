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

- `pockett_TL1`
  kind: `Corner`
  center: `(1.2640905, 0.49089444)`
  capture radius seed: `0.0584`
- `pocket_BL2`
  kind: `Corner`
  center: `(1.2508553, -0.6167401)`
  capture radius seed: `0.0584`
- `pocket_BM3`
  kind: `Side`
  center: `(0.0000029, -0.66602194)`
  capture radius seed: `0.0614`
- `pocket_BR4`
  kind: `Corner`
  center: `(-1.2508456, -0.6167401)`
  capture radius seed: `0.0584`
- `Pocket_TR5`
  kind: `Corner`
  center: `(-1.2508456, 0.61673915)`
  capture radius seed: `0.0584`
- `Pocket_TM6`
  kind: `Side`
  center: `(0.0000029, 0.66556805)`
  capture radius seed: `0.0614`

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
- `pockett_TL1_jaw_vertical`
- `pockett_TL1_jaw_horizontal`
- `pocket_BM3_jaw_left`
- `pocket_BM3_jaw_right`
- `Pocket_TM6_jaw_left`
- `Pocket_TM6_jaw_right`

Each jaw segment is derived from:

- one adjacent hardcoded rail endpoint
- the matching hardcoded pocket center
- an inward offset from pocket center toward cloth center

The resulting derived segments are compiled into `TableSpec.JawSegments`.

## Rules

- This file changes only when a code-facing seed value changes.
- If Blender source facts change without a code change, update `MODEL_REFERENCE.md` first.
- If code constants change, update both this file and the code in the same commit.
