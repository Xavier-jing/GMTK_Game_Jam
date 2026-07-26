# Isometric placement tool

Open `Tools/Jam Template/Isometric Placement`.

## Aligning the Scene View

1. Keep the gameplay camera selected as `Placement Camera`, or press
   `Use Main Camera`.
2. Press `Align Scene View To Placement Camera`.
3. Use this Scene View for visual placement. The camera and scene objects are not
   modified by alignment.

## Normalizing visual Y

1. Select one or more visual root objects. Avoid selecting gameplay colliders,
   the Player, Rails, or interactive object roots.
2. Set `Target World Y`, normally `0`.
3. Press `Normalize Selected Y (Preserve Game View)`.

The tool projects each selected object's existing Game View position onto the
horizontal target plane. Its world Y becomes the requested value while its
screen position remains unchanged. Parent-and-child selections are filtered so
children are not moved twice.

The operation supports Undo and marks touched scenes dirty, but never saves a
scene automatically.

## Sorting order

Select a SpriteRenderer or a visual root containing SpriteRenderers, set
`Sorting Order`, and press `Apply Order To Selected SpriteRenderers`.

Recommended temporary orders:

- environment base: `-30`;
- carpet and ground decoration: `-10`;
- player projection: `-1`;
- player: `0`;
- foreground occluders: `10`.
