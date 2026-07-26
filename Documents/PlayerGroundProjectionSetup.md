# Player ground projection setup

`PlayerGroundProjection` shows a human-authored Sprite directly below the player
while the player is airborne. It raycasts against configured ground layers,
places the Sprite on the detected surface, and reduces its scale and opacity as
the player moves farther from the ground.

## Unity hookup

1. Open `Assets/_Project/Scenes/GamePlay.unity` in Unity `2022.3.62f3c1`.
2. Under `Player`, create a child named `GroundProjection`.
3. Add a `SpriteRenderer` to `GroundProjection`.
4. Assign a human-authored soft ellipse/blob Sprite:
   - the source image should have a transparent background;
   - use a dark neutral color;
   - keep the SpriteRenderer color alpha at `1`, because runtime opacity is
     controlled by `PlayerGroundProjection`;
   - use sorting order `-1` initially so it renders below the player.
5. Add `PlayerGroundProjection` to `Player`.
6. Assign the `GroundProjection` SpriteRenderer.
7. Set `Ground Layers` to the same ground layer used by `PlayerData.whatIsGround`.
8. Recommended initial values:
   - Max Projection Distance: `20`;
   - Minimum Air Height: `0.15`;
   - Surface Offset: `0.02`;
   - Face Camera: enabled;
   - Camera Facing Depth Offset: `0.5`;
   - Full Fade Height: `6`;
   - Near Scale Multiplier: `1`;
   - Far Scale Multiplier: `0.45`;
   - Near Alpha Multiplier: `0.5`;
   - Far Alpha Multiplier: `0.15`.
9. Let Unity generate and review the new script `.meta`, then save the scene.

## Play Mode checks

1. Standing and walking on the ground: projection hidden.
2. Normal jump: projection appears on the ground below the player.
3. Ascending and floating: projection remains on the closest configured ground.
4. Carrying an object and sinking: projection grows/darkens as the player nears
   the ground, then hides after landing.
5. Moving above a gap with no configured ground within range: projection hidden.

For a hand-drawn ellipse or blob, keep `Face Camera` enabled and do not manually
force `GroundProjection` to X rotation `-90`. The component controls rotation at
runtime. Disable `Face Camera` only when the supplied Sprite is specifically
designed to lie flat on a 3D ground surface.

If only half of the projection is visible, the ground mesh is clipping the
camera-facing Sprite. Increase `Camera Facing Depth Offset` gradually from
`0.5` to `0.75`. In an orthographic camera this moves the Sprite toward the
camera without changing its screen position.
