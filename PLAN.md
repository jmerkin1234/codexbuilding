# Plan

This file is the execution tracker for the portable billiards engine. It exists to keep the build order explicit and prevent scope drift.

## Project Rules

- The physics engine stays portable and renderer-agnostic.
- Godot 4.6 is only the adapter/viewer layer.
- The Blender scene is the source for hardcoded geometry references.
- Old gameplay files are off-limits unless explicitly named by the user.
- Jump shots are out of scope.
- Every implementation step gets standalone tests before moving on.
- Markdown docs are updated with each milestone.

## Ordered Steps

1. Blender extraction and source-name preservation
Status: complete

2. Hardcoded table specification in portable C#
Status: complete

3. Deterministic fixed-step simulation shell
Status: complete
Scope:
- fixed-step accumulator
- simulation phases
- settle detection
- standalone tests for stepping behavior
Not included:
- billiards friction
- cue strike physics
- spin transfer
- collisions
- pocket capture

4. Cue strike and spin input model
Status: complete
Scope:
- cue-ball selection
- aim normalization
- tip-offset clamping
- initial velocity seeding
- initial spin seeding
Not included:
- cloth response to spin
- collision-side spin transfer

5. Cloth motion model
Status: complete
Scope:
- sliding
- rolling
- skid-to-roll transition
- spin decay

6. Ball-ball collisions
Status: complete
Scope:
- equal-mass collision response
- restitution
- overlap correction
- iterative pair solving

7. Cushion and pocket-jaw interactions
Status: complete
Scope:
- hardcoded jaw segment derivation from rail/pocket seeds
- boundary reflection against cushions
- boundary reflection against jaws
- overlap correction against table boundaries

8. Pocket capture and shot event expansion
Status: pending

9. Deterministic replay and regression coverage
Status: pending

10. Godot 4.6 visual/gameplay adapter
Status: pending

11. 8-ball rules layer
Status: pending

## Current Focus

Right now the repo is moving from step 7 to step 8. Table boundaries are in; the next increment is pocket capture and shot event expansion.
