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

## Hardcoded Godot Adapter Values

These are not physics-authority geometry values, but they are still hardcoded compile-time constants in the Godot 4.6 adapter and should stay documented here when they change.

- Cue stick tip gap target: `0.03 m`
- Default strike speed: `2.2 m/s`
- Regular shot speed cap: `5.0 m/s`
- Break shot speed cap: `8.0 m/s`
- Overlay line thickness default: `1.5 px`
- Overlay line thickness min/max: `0.5 px` to `2.0 px`
- Aim-preview guide thickness: `2.0 px`
- Aim-preview post-contact frames: `48`
- Aim-preview max simulation steps: `360`

## Hardcoded Cushion Segments

- `rail_head`
  start: `(-1.2249266, -0.56977034)`
  end: `(-1.2249266, 0.57038474)`
  inward normal: `(1.0, 0.0)`
- `rail_foot`
  start: `(1.2258036, -0.5681504)`
  end: `(1.2258036, 0.5702276)`
  inward normal: `(-1.0, 0.0)`
- `rail_upper_left`
  start: `(-1.2068906, -0.59292513)`
  end: `(-0.050392687, -0.59292513)`
  inward normal: `(0.0, 1.0)`
- `rail_upper_right`
  start: `(0.050399005, -0.59292495)`
  end: `(1.2069001, -0.59292495)`
  inward normal: `(0.0, 1.0)`
- `rail_bottom_left`
  start: `(-1.2068906, 0.5953581)`
  end: `(-0.050392687, 0.5953581)`
  inward normal: `(0.0, -1.0)`
- `rail_bottom_right`
  start: `(0.050398767, 0.5953247)`
  end: `(1.2068999, 0.5953247)`
  inward normal: `(0.0, -1.0)`

## Hardcoded Jaw Segments

- `pocket_BR4_jaw_vertical`
  start: `(-1.22492660, -0.56977034)`
  end: `(-1.22393849, -0.60347332)`
  inward normal: `(0.99957050, 0.02930557)`
- `pocket_BR4_jaw_horizontal`
  start: `(-1.20689060, -0.59292513)`
  end: `(-1.22393849, -0.60347332)`
  inward normal: `(0.52616470, -0.85038269)`
- `pocket_BL2_jaw_vertical`
  start: `(1.22580360, -0.56815040)`
  end: `(1.22394815, -0.60347340)`
  inward normal: `(-0.99862325, 0.05245579)`
- `pocket_BL2_jaw_horizontal`
  start: `(1.20690010, -0.59292495)`
  end: `(1.22394815, -0.60347340)`
  inward normal: `(-0.52617062, -0.85037902)`
- `Pocket_TR5_jaw_vertical`
  start: `(-1.22492660, 0.57038474)`
  end: `(-1.22393848, 0.60347239)`
  inward normal: `(0.99955438, -0.02985034)`
- `Pocket_TR5_jaw_horizontal`
  start: `(-1.20689060, 0.59535810)`
  end: `(-1.22393848, 0.60347239)`
  inward normal: `(-0.42977155, -0.90293766)`
- `pocket_TL1_jaw_vertical`
  start: `(1.22580360, 0.57022760)`
  end: `(1.22222664, 0.60454568)`
  inward normal: `(-0.99461194, -0.10366814)`
- `pocket_TL1_jaw_horizontal`
  start: `(1.20689990, 0.59532470)`
  end: `(1.22222664, 0.60454568)`
  inward normal: `(-0.51552071, 0.85687712)`
- `pocket_BM3_jaw_left`
  start: `(-0.05039269, -0.59292513)`
  end: `(0.00000276, -0.63043388)`
  inward normal: `(0.59706361, 0.80219389)`
- `pocket_BM3_jaw_right`
  start: `(0.05039900, -0.59292495)`
  end: `(0.00000276, -0.63043388)`
  inward normal: `(-0.59705944, 0.80219700)`
- `Pocket_TM6_jaw_left`
  start: `(-0.05039269, 0.59535810)`
  end: `(0.00000276, 0.63456805)`
  inward normal: `(0.61407222, -0.78924984)`
- `Pocket_TM6_jaw_right`
  start: `(0.05039877, 0.59532470)`
  end: `(0.00000276, 0.63456805)`
  inward normal: `(-0.61439372, -0.78899959)`

## Hardcoded Pocket Specs

- `pocket_TL1`
  kind: `Corner`
  center: `(1.249117, 0.61784637)`
  capture radius seed: `0.0584`
  mouth center: `(1.21635175, 0.58277615)`
  mouth half-width: `0.01570998`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(0.68268613, 0.73071174)`
  funnel depth: `0.04799460`
- `pocket_BL2`
  kind: `Corner`
  center: `(1.2508553, -0.6167401)`
  capture radius seed: `0.0584`
  mouth center: `(1.21635185, -0.58053768)`
  mouth half-width: `0.01558140`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(0.68991678, -0.72388869)`
  funnel depth: `0.05001103`
- `pocket_BM3`
  kind: `Side`
  center: `(0.0000029, -0.66143388)`
  capture radius seed: `0.0614`
  mouth center: `(0.00000316, -0.59292504)`
  mouth half-width: `0.05039585`
  drop radius: `0.044`
  max entry speed: `1.0`
  entry direction: `(-0.00000380, -1.0)`
  funnel depth: `0.06850884`
- `pocket_BR4`
  kind: `Corner`
  center: `(-1.2508456, -0.6167401)`
  capture radius seed: `0.0584`
  mouth center: `(-1.2159086, -0.58134774)`
  mouth half-width: `0.01467516`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(-0.70251377, -0.71167015)`
  funnel depth: `0.04973141`
- `Pocket_TR5`
  kind: `Corner`
  center: `(-1.2508456, 0.61673915)`
  capture radius seed: `0.0584`
  mouth center: `(-1.2159086, 0.58287142)`
  mouth half-width: `0.01540265`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(-0.71800898, 0.69603384)`
  funnel depth: `0.04865817`
- `Pocket_TM6`
  kind: `Side`
  center: `(0.0000029, 0.66556805)`
  capture radius seed: `0.0614`
  mouth center: `(0.00000304, 0.5953414)`
  mouth half-width: `0.05039573`
  drop radius: `0.044`
  max entry speed: `1.0`
  entry direction: `(-0.00000199, 1.0)`
  funnel depth: `0.07022665`

## Hardcoded Jaw Derivation

Jaw segments are now part of the compile-time table spec.

- Corner jaw apex offset toward cloth center: `0.03`
- Side jaw apex offset toward cloth center: `0.031`
- Total derived jaw segments: `12`

Each jaw segment is derived from one adjacent hardcoded rail endpoint and an inward apex offset from the matching pocket center toward cloth center, and the resulting exact segment values are listed above.

## Hardcoded Pocket Mouth Model

Pocket capture is no longer a single circle check.

- Mouth centers are derived from the midpoint between the two relevant jaw start points.
- Mouth half-widths are derived from half the distance between those two jaw start points.
- Entry directions are derived from `Normalize(center - mouthCenter)`.
- Funnel depth is `Distance(mouthCenter, center)`.
- Balls are always accepted inside the `drop radius`.
- Balls inside the pocket funnel are only accepted when lane alignment and entry speed fit the hardcoded mouth model.

## Rules

- This file changes only when a code-facing seed value changes.
- If Blender source facts change without a code change, update `MODEL_REFERENCE.md` first.
- If code constants change, update both this file and the code in the same commit.
