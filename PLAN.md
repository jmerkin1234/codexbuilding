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
Status: complete
Scope:
- pocket capture from hardcoded pocket centers and radii
- cue-ball scratch detection
- first-contact event emission
- cushion and jaw contact events
- pocketed-ball events

9. Deterministic replay and regression coverage
Status: complete
Scope:
- portable replay trace capture
- frame-by-frame ball and event snapshots
- deterministic fingerprint builder
- locked straight-shot regression signature
- standalone replay repeatability tests

10. Godot 4.6 visual/gameplay adapter
Status: complete
Scope:
- standard 8-ball rack seeding from the portable core
- Godot scene graph with Blender-facing node names
- ball-state transform sync from `SimulationWorld`
- keyboard aim, speed, and spin controls
- procedural fallback table visual when no imported table scene is present
- Godot adapter compile verification

11. 8-ball rules layer
Status: complete
Scope:
- replay-trace shot summary builder
- 8-ball match state and turn resolution
- break legality and foul detection
- open-table group assignment and ball-in-hand
- legal and illegal 8-ball endgame handling
- separate portable training-mode state and shot resolution
- standalone rules-engine tests

12. Godot rules/training integration
Status: complete
Scope:
- live shot-trace capture inside the Godot adapter
- 8-ball and training-mode switching from the adapter
- HUD display for player, groups, winner, and rules state
- cue-ball-in-hand placement controls
- 8-ball respot and post-shot layout reset flow
- Godot adapter compile verification after rules wiring

13. Training/presentation expansion
Status: complete
Scope:
- predictive cue aim line from cloned portable simulations
- cue continuation line after first bounce or collision
- contacted object-ball guide line after first hit
- practice-mode ball selection and free layout movement
- selected-ball visual emphasis in practice mode
- Godot adapter compile verification after presentation changes

14. Presentation/polish backlog
Status: in_progress
Scope:
- hardcoded-table debug overlay lines from `TableSpec`
- live debug-mode engine data panel in the Godot adapter
- continued scenario tooling
- broader presentation polish

## Current Focus

Right now the repo is in step 14. The Godot adapter is rules-aware, training-aware, has predictive guide visuals, and now exposes the hardcoded table geometry as a toggleable overlay; the remaining backlog is broader presentation polish and scenario tooling.
