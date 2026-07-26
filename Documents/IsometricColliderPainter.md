# Isometric collider painter

Open `Tools/Jam Template/Isometric Collider Painter`.

## Matching a camera-occlusion surface to a background sprite

Use this for a wall or floor that is already a single rotated background image:

1. Select the GameObject containing the background `SpriteRenderer`.
2. Choose the dedicated camera-occlusion layer in `Collider Layer`.
3. Set `Surface Depth`, normally `0.1` to `0.3`.
4. Press `Match Collider To Selected Sprite`.

The generated child collider inherits the sprite's complete transform, including
its original X-axis 45-degree rotation and scale. It is a trigger and therefore
does not physically block the player. `CameraOcclusionFader` detects these
triggers through its `Occluder Layers` mask.

## Matching an item interaction collider

1. Put the item visual `SpriteRenderer` under the GameObject containing
   `WorldStoryInteractable`.
2. Select the visual `SpriteRenderer`, not the logical root.
3. Choose an interaction layer in `Collider Layer`.
4. Set `Item Depth` and `Item Padding`.
5. Press `Match Item Collider To Selected Sprite`.

The tool creates an `ItemInteractionCollider` child that inherits the visual's
complete Scene transform, including rotations and scale. It is a 3D trigger and
is automatically added to the parent's `Interaction Colliders` array. Pressing
the button again updates the existing collider after the visual changes instead
of creating a duplicate. The scene is never saved automatically.

For isometric visuals whose apparent Game View position does not match their
world position, use `Project Item Collider To Ground` instead:

1. Assign the gameplay camera as `Placement Camera`.
2. Set `Ground World Y`, normally `0`.
3. Set `Grounded Item Size`, normally `(1, 1, 1)`.
4. Select the item visual and press `Project Item Collider To Ground`.

The tool projects the sprite center through the gameplay camera onto the ground
plane, creates an upright `ItemGroundInteractionCollider`, and assigns it as the
item's `Interaction Point`. This is the recommended mode for player interaction;
the sprite remains visually unchanged.

## Before drawing

1. Exit Play Mode.
2. Select the scene object that should contain generated colliders, normally
   `====Collider====`, and drag it into `Collider Parent`.
3. Set `Ground World Y` to the player's standing ground height, normally `0`.
4. Use a gameplay collision layer. Do not use the camera-occlusion layer unless
   the same collider intentionally serves both purposes.

Recommended starting values:

- wall height: `2.5`;
- wall thickness: `0.25`;
- floor thickness: `0.25`.
- floor Y rotation: `45` for a diamond-shaped isometric footprint.

## Drawing walls

1. Align the Scene View with the gameplay camera using
   `Tools/Jam Template/Isometric Placement`.
2. Press `Draw Wall (Two Endpoints)`.
3. Click the two ground endpoints of each wall.

The tool creates a vertical 3D `BoxCollider`; no manual 45-degree rotation is
required.

## Drawing floors

1. Press `Draw Floor (Two Corners)`.
2. Click two opposite ground-plane corners.

The generated collider sits immediately below `Ground World Y`, so its top face
matches the standing plane. `Floor Y Rotation` rotates the footprint around the
world Y axis. Keep the collider horizontal; do not copy the background sprite's
X-axis tilt onto the gameplay floor.

Press `Esc` to cancel drawing. Every creation supports Undo, marks the scene
dirty, and never saves the scene automatically.

## Player ground projection

`PlayerGroundProjection` still prioritizes real colliders on its configured
Ground Layers. If no collider is found, it falls back to the configured world-Y
floor heights. The project defaults are `0` for the lower floor and `6` for the
upper layer. Keep these values synchronized with the actual gameplay heights.
