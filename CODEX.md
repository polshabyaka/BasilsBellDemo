# Basil’s Bell Practice Demo — Codex Rules

## Project Context

This repository is for **Basil’s Bell Practice Demo**, a new Unity 3D URP vertical slice.

The old prototype is considered **legacy**. Do not reuse old architecture, systems, or folders unless the user explicitly asks for it.

The demo focus is:

* fixed-camera point-and-click shop;
* 3D diorama shop without free player movement;
* camera anchors and hotspot navigation;
* handmade candle crafting with scripted interactions;
* simple forest material gathering;
* short dialogue choices;
* 7-day demo structure.

The goal is a small, playable, stable demo, not a large reusable framework.

## Main Development Rule

Work in **small, safe steps**.

Each task should implement only what was requested. Do not expand the system “just in case”.

Prefer simple, readable, maintainable Unity code over heavy architecture.

Avoid overengineering.

## What Codex Should Do

When implementing a task:

1. Read the current project structure before making changes.
2. Identify the smallest possible change that satisfies the request.
3. Create or modify only the necessary files.
4. Keep scene hierarchy and object names readable.
5. Use serialized fields where Unity Inspector setup is useful.
6. Add clear comments only where they explain non-obvious behavior.
7. Preserve existing working behavior unless the task explicitly asks to change it.
8. Summarize all created, modified, or deleted files after completing the task.
9. Mention any required Unity Inspector setup.
10. Mention how to test the result in Play Mode.

## What Codex Should Not Do

Do not:

* write a full architecture before it is needed;
* add dependency injection, service locators, event buses, or complex managers unless specifically requested;
* introduce Rigidbody-based physics for crafting interactions;
* implement free player movement in the shop;
* implement real fluid simulation, wax physics, or chaotic object physics;
* create a large inventory system with many tabs;
* create a full economy system;
* create procedural shop generation;
* import assets without being asked;
* rename large folders without being asked;
* move or delete existing assets unless clearly necessary;
* pull systems from the legacy prototype by default.

## Camera and Navigation Rules

The shop uses fixed camera points.

Preferred structure:

* `CameraAnchor_Overview`
* `CameraAnchor_WorkTable`
* `CameraAnchor_Shelves`
* `CameraAnchor_Counter`
* `CameraAnchor_PersonalCorner`
* `CameraAnchor_ForestExit`

Hotspots should move the camera to a target anchor.

A Back button should return the player to the previous camera anchor.

Do not add Cinemachine unless it is already installed and the user agrees it is useful.

For the first milestone, simple smooth Transform movement is enough.

## Interaction Rules

Interactions should be scripted and predictable.

Use:

* hotspots;
* click detection;
* snap points;
* item states;
* simple visual feedback;
* controlled drag/place behavior if needed.

Avoid relying on unstable physics behavior.

Rigidbody physics should not be required for gameplay correctness.

## Crafting Rules

Crafting should feel physical, but remain controlled.

For the practice demo, candle crafting is the main complex mechanic.

Examples of acceptable scripted crafting interactions:

* drag material to mortar;
* grind material with progress feedback;
* change material state from raw to powder;
* place component into a snap slot;
* pour or confirm wax through scripted animation;
* create candle state after the correct steps.

Do not simulate liquid wax physically.

Do not create a huge recipe system before the first working craft action exists.

## Scene Rules

Scenes should be clean and easy to inspect.

Use parent objects such as:

* `_Scene`
* `_Cameras`
* `_CameraAnchors`
* `_Hotspots`
* `_Blockout`
* `_Lighting`
* `_UI`
* `_Systems`

Use clear object names. Avoid leaving important objects named `Cube`, `GameObject`, or `Sphere`.

For blockouts, Unity primitives are acceptable.

The user can adjust positions manually in the Unity Editor after the technical setup is created.

## UI Rules

Keep UI minimal unless requested.

For the first milestone, only create UI that is needed for testing:

* Back button;
* simple interaction prompt if necessary;
* simple feedback text if necessary.

Do not create final UI style, full HUD, inventory screens, menus, or settings unless requested.

## Git Rules

Recommend a commit after a small feature is working and tested in Play Mode. Wait until my approval (I will test project in Unity)

Good commit examples:

* `Create ShopBlockout scene foundation`
* `Add fixed camera anchor navigation`
* `Add hotspot camera transitions`
* `Add back button navigation`
* `Add first worktable interaction`
* `Prototype mortar grinding interaction`

Do not suggest committing broken or untested changes unless the user explicitly wants a WIP commit.

## Error Handling

If Unity errors appear:

1. Identify the error message and affected file.
2. Explain the likely cause in plain language.
3. Suggest the smallest fix.
4. Avoid rewriting unrelated systems.

## Unity Scene Editing Safety Rules

Do not directly edit `.unity`, `.prefab`, `.mat`, `.asset`, or other Unity serialized YAML files as plain text unless the user explicitly asks for it.

For scene setup or hierarchy changes, prefer one of these approaches:

1. Create a small Unity Editor menu tool that performs changes through Unity Editor APIs.
2. Ask the user to make simple Inspector/Hierarchy setup manually.
3. Create runtime scripts only and provide clear Inspector assignment instructions.

When creating Editor tools:
- make them idempotent where possible;
- use clear menu names;
- preserve world position and rotation when reparenting objects;
- do not regenerate or overwrite scenes unless the user explicitly confirms;
- never silently delete existing scene objects;
- always list what the tool will create, move, or modify.

Avoid modifying Unity scene YAML directly, because broken parent-child references can corrupt scene hierarchy.

## Output Format for Codex

After completing a task, respond with:

1. **Summary**

   * What was created or changed.

2. **Files changed**

   * List of modified/created files.

3. **Unity setup**

   * What the user must assign or check in Inspector.

4. **Play Mode test**

   * Step-by-step test checklist.

5. **Risks / notes**

   * Any limitations or things intentionally left for later.

## Current First Milestone

The current technical milestone is:

1. Create `ShopBlockout`.
2. Add fixed camera anchors.
3. Add hotspots that move the camera to target anchors.
4. Add Back button.
5. Add first interactive worktable object.
6. Prototype one action: grinding herb in a mortar with simple visual feedback.

Do not skip ahead to full crafting, inventory, dialogue, forest, or day cycle until this milestone works.
