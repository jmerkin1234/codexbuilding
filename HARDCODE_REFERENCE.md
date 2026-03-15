# Hardcode Reference

This file tracks the compile-time geometry and seed constants that the portable engine is allowed to bake into code.

## Purpose

- `MODEL_REFERENCE.md` documents raw Blender facts.
- `HARDCODE_REFERENCE.md` documents the subset of those facts that are intentionally compiled into the engine.

## Active Table Spec

- Code type: `CustomTable9FtSpec`
- Source blend: `/home/justin/Desktop/MASTERtable_9ft.blend`
- Table name in code: `MASTERtable_9ft`

## Pocket Name Mapping

The runtime engine still keeps its older pocket IDs for rules/test stability. Those IDs now map to the Blender source names like this:

- `pocket_TL1` -> Blender `pocket_FRC`
- `pocket_BL2` -> Blender `pocket_FLC`
- `pocket_BM3` -> Blender `pocket_LS`
- `pocket_BR4` -> Blender `pocket_HLC`
- `Pocket_TR5` -> Blender `pocket_HRC`
- `Pocket_TM6` -> Blender `pocket_RS`

## Hardcoded Play Surface

- Play-area minimum: `(-1.2699999, -0.63499993)`
- Play-area maximum: `(1.2699999, 0.63499993)`
- Ball diameter: `0.05715`
- Cue ball spawn seed: `(-0.733902, 0.002383)`
- Rack apex seed: `(0.63499995, 0.0)`

## Hardcoded Godot Adapter Values

These are not physics-authority geometry values, but they are still hardcoded compile-time constants in the Godot 4.6 adapter and should stay documented here when they change.

- Cue stick tip gap target: `0.03 m`
- Default strike speed: `2.2 m/s`
- Regular shot speed cap: `5.0 m/s`
- Break shot speed cap: `8.0 m/s`
- Overlay line thickness default: `1.5 px`
- Overlay line thickness min/max: `0.5 px` to `2.0 px`
- Aim-preview guide thickness: `2.0 px`
- Aim-preview post-contact frames: `96`
- Aim-preview max simulation steps: `720`

## Hardcoded Cushion Segments

- `rail_head`
  start: `(-1.2249266, -0.51401377)`
  end: `(-1.2249266, 0.51462537)`
  inward normal: `(1.0, 0.0)`
- `rail_foot`
  start: `(1.2258036, -0.5123930)`
  end: `(1.2258036, 0.5144738)`
  inward normal: `(-1.0, 0.0)`
- `rail_upper_left`
  start: `(-1.1487218, -0.5934566)`
  end: `(-0.062250376, -0.5934566)`
  inward normal: `(0.0, 1.0)`
- `rail_upper_right`
  start: `(0.062256694, -0.5929293)`
  end: `(1.1487315, -0.5929293)`
  inward normal: `(0.0, 1.0)`
- `rail_bottom_left`
  start: `(-1.1487218, 0.5953581)`
  end: `(-0.062250376, 0.5953581)`
  inward normal: `(0.0, -1.0)`
- `rail_bottom_right`
  start: `(0.062256455, 0.5953247)`
  end: `(1.1487312, 0.5953247)`
  inward normal: `(0.0, -1.0)`

## Hardcoded Jaw Segments

- `pocket_BR4_jaw_vertical`
  start: `(-1.22492660, -0.51401377)`
  end: `(-1.22393253, -0.60333737)`
  inward normal: `(0.99993808, 0.01112817)`
- `pocket_BR4_jaw_horizontal`
  start: `(-1.14872180, -0.59345660)`
  end: `(-1.22393253, -0.60333737)`
  inward normal: `(-0.13025526, 0.99148049)`
- `pocket_BL2_jaw_vertical`
  start: `(1.22580360, -0.51239300)`
  end: `(1.22393741, -0.60333741)`
  inward normal: `(-0.99978953, 0.02051580)`
- `pocket_BL2_jaw_horizontal`
  start: `(1.14873150, -0.59292930)`
  end: `(1.22393741, -0.60333741)`
  inward normal: `(0.13708829, 0.99055883)`
- `Pocket_TR5_jaw_vertical`
  start: `(-1.22492660, 0.51462537)`
  end: `(-1.22393254, 0.60333865)`
  inward normal: `(0.99993723, -0.01120459)`
- `Pocket_TR5_jaw_horizontal`
  start: `(-1.14872180, 0.59535810)`
  end: `(-1.22393254, 0.60333865)`
  inward normal: `(-0.10551682, -0.99441752)`
- `pocket_TL1_jaw_vertical`
  start: `(1.22580360, 0.51447380)`
  end: `(1.22221591, 0.60441188)`
  inward normal: `(-0.99920532, -0.03985895)`
- `pocket_TL1_jaw_horizontal`
  start: `(1.14873120, 0.59532470)`
  end: `(1.22221591, 0.60441188)`
  inward normal: `(0.12272608, -0.99244058)`
- `pocket_BM3_jaw_left`
  start: `(-0.062250376, -0.59345660)`
  end: `(0.00000277, -0.63400000)`
  inward normal: `(0.54573430, 0.83795827)`
- `pocket_BM3_jaw_right`
  start: `(0.062256694, -0.59292930)`
  end: `(0.00000277, -0.63400000)`
  inward normal: `(-0.55068445, 0.83471350)`
- `Pocket_TM6_jaw_left`
  start: `(-0.062250376, 0.59535810)`
  end: `(0.00000277, 0.63400000)`
  inward normal: `(0.52738273, -0.84962784)`
- `Pocket_TM6_jaw_right`
  start: `(0.062256455, 0.59532470)`
  end: `(0.00000277, 0.63400000)`
  inward normal: `(-0.52770837, -0.84942562)`

## Hardcoded Pocket Specs

- `pocket_TL1`
  kind: `Corner`
  center: `(1.2491074, 0.6177103)`
  capture radius seed: `0.0584`
  mouth center: `(1.1872674, 0.55489925)`
  mouth half-width: `0.05585030`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(0.70157703, 0.71259362)`
  funnel depth: `0.08814428`
- `pocket_BL2`
  kind: `Corner`
  center: `(1.2508457, -0.6166018)`
  capture radius seed: `0.0584`
  mouth center: `(1.18726755, -0.55266115)`
  mouth half-width: `0.05573644`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(0.70509383, -0.70911402)`
  funnel depth: `0.09016977`
- `pocket_BM3`
  kind: `Side`
  center: `(0.0000029057, -0.6650000)`
  capture radius seed: `0.0614`
  mouth center: `(0.0000031590, -0.59319295)`
  mouth half-width: `0.06225409`
  drop radius: `0.0440`
  max entry speed: `1.0`
  entry direction: `(-0.00000353, -1.0)`
  funnel depth: `0.07180705`
- `pocket_BR4`
  kind: `Corner`
  center: `(-1.2508408, -0.6166018)`
  capture radius seed: `0.0584`
  mouth center: `(-1.1868242, -0.55373519)`
  mouth half-width: `0.05504165`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(-0.71348622, -0.70066926)`
  funnel depth: `0.08972367`
- `Pocket_TR5`
  kind: `Corner`
  center: `(-1.2508408, 0.6166031)`
  capture radius seed: `0.0584`
  mouth center: `(-1.1868242, 0.55499174)`
  mouth half-width: `0.05550888`
  drop radius: `0.0405`
  max entry speed: `1.15`
  entry direction: `(-0.72051279, 0.69344165)`
  funnel depth: `0.08884867`
- `Pocket_TM6`
  kind: `Side`
  center: `(0.0000029057, 0.6650000)`
  capture radius seed: `0.0614`
  mouth center: `(0.0000030395, 0.5953414)`
  mouth half-width: `0.06225342`
  drop radius: `0.0440`
  max entry speed: `1.0`
  entry direction: `(-0.00000192, 1.0)`
  funnel depth: `0.06965860`

## Hardcoded Jaw Derivation

Jaw segments are part of the compile-time table spec.

- Corner jaw apex offset toward table center: `0.03`
- Side jaw apex offset toward table center: `0.031`
- Total derived jaw segments: `12`

Each jaw segment is derived from one adjacent hardcoded rail endpoint and an inward apex offset from the matching pocket center toward table center. The resulting exact segment values are listed above.

## Hardcoded Pocket Mouth Model

Pocket capture is no longer a single circle check.

- Mouth centers are derived from the midpoint between the two relevant jaw-start rail endpoints.
- Mouth half-widths are derived from half the distance between those two jaw-start points.
- Entry directions are derived from `Normalize(center - mouthCenter)`.
- Funnel depth is `Distance(mouthCenter, center)`.
- Balls are always accepted inside the `drop radius`.
- Balls inside the pocket funnel are only accepted when lane alignment and entry speed fit the hardcoded mouth model.

## Rules

- This file changes only when a code-facing seed value changes.
- If Blender source facts change without a code change, update `MODEL_REFERENCE.md` first.
- If code constants change, update both this file and the code in the same commit.
