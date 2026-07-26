# Boot Video Loading Setup

This setup changes only `Boot -> MainMenu`. `MainMenu -> GamePlay` and every
other transition continue to use the existing `LoadingScreen`.

## Unity setup

Use Unity `2022.3.62f3c1`.

1. Import the human-created loading video. Unity will generate and update its
   `.meta`; review and commit both files manually.
2. Open `Assets/_Project/Scenes/Boot.unity`.
3. Create a root GameObject named `BootVideo`.
4. Add `Video Player` and `Boot Video Loading` to `BootVideo`.
5. In `Boot Video Loading`, assign the same object's `Video Player` component
   to `Video Player`.
6. Configure `Video Player`:
   - Source: `Video Clip`
   - Video Clip: the human-created loading video
   - Play On Awake: off
   - Wait For First Frame: on
   - Loop: off
   - Render Mode: `Camera Near Plane`
   - Camera: the Boot scene root `Main Camera`
   - Alpha: `1`
   - Aspect Ratio: `Fit Inside` for letterboxing, or `Fit Outside` for cropping
   - Audio Output Mode: `Direct` if the video contains audio
   - Do not select `Render Texture` unless `Target Texture` is assigned and a
     visible `RawImage` displays that same texture. When that output is missing,
     `BootVideoLoading` falls back to the Boot Main Camera at runtime.
7. Keep the existing root `LoadingRoot` and its `LoadingScreen`. It is hidden
   immediately only when a valid Boot video is present, then remains available
   for later scene transitions.
8. Save `Assets/_Project/Scenes/Boot.unity`. This scene change and any generated
   `.meta` must be reviewed and committed by a human.

## Expected behavior

- Boot starts preparing and playing the video while MainMenu loads in the
  background.
- MainMenu activates after both the video has ended and the scene has reached
  Unity's activation-ready state.
- A video playback error is logged and MainMenu activates instead of hanging.
- If `BootVideo` is absent or has no clip/URL, Boot falls back to the existing
  LoadingScreen flow.
- MainMenu's Play button still uses the existing progress LoadingScreen when it
  loads `Assets/_Project/Scenes/GamePlay.unity`.

## Play Mode smoke test

1. Start Play Mode from `Assets/_Project/Scenes/Boot.unity`.
2. Confirm the video is visible, plays once, and MainMenu appears afterward.
3. Click Play in MainMenu and confirm the original progress LoadingScreen is
   shown before GamePlay.
4. Confirm HUD and Pause open correctly in GamePlay.
5. Return to MainMenu and confirm the existing LoadingScreen is still used.
6. Temporarily clear the Video Clip reference and repeat step 1; confirm the
   normal Boot LoadingScreen fallback appears and the Console contains no
   unhandled exception.
