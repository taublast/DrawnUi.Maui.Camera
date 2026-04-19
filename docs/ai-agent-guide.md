# AI Agent Integration Guide

This section provides specific guidance for AI agents working with SkiaCamera.

## Quick Start for AI Agents

```csharp
// 1. Setup dual-channel camera
var camera = new SkiaCamera
{
    Facing = CameraPosition.Default,
    PhotoQuality = CaptureQuality.Medium,
    IsOn = true
};

// 2. CHANNEL 1: Raw frame processing for AI/ML
public class MyCamera : SkiaCamera
{
    private readonly byte[] _rgba = new byte[224 * 224 * 4];

    protected override void OnRawFrameAvailable(RawCameraFrame frame)
    {
        if (!frame.TryGetRgba(224, 224, _rgba))
            return;

        QueueInference(_rgba, frame.Rotation);
    }
}

// Hosted multimodal API path: export standard image bytes instead of raw RGBA.
public class HostedVisionCamera : SkiaCamera
{
    protected override void OnRawFrameAvailable(RawCameraFrame frame)
    {
        if (!frame.TryGetJpeg(1024, 1024, out var jpegBytes, 100))
            return;

        QueueHostedVisionRequest(jpegBytes, "image/jpeg");
    }
}

// Alternative preview channel: use NewPreviewSet only when you need the final displayed preview.
camera.NewPreviewSet += (s, source) =>
{
    source.ProtectFromDispose = true;
    Task.Run(() =>
    {
        try { AnalyzeDisplayedPreview(source.Image); }
        finally
        {
            source.ProtectFromDispose = false;
            source.Dispose();
        }
    });
};

// 3. CHANNEL 2: Captured photo processing
camera.CaptureSuccess += (s, captured) => {
    ProcessCapturedPhoto(captured.Image, captured.Metadata);
};

// 4. Manual camera selection
var cameras = await camera.GetAvailableCamerasAsync();
camera.Facing = CameraPosition.Manual;
camera.CameraIndex = 2;

// 5. Take photo (triggers Channel 2 processing)
await camera.TakePicture();
```

## Key Patterns for AI Agents

1. **Understand dual channels**: Preview processing != Capture processing
2. **Channel 1 (Raw preview for AI/ML)**: Override `OnRawFrameAvailable(RawCameraFrame frame)` and call `frame.TryGetRgba(...)`
3. **Raw export choice matters**: `TryGetRgbaBytes(...)` is for custom raw-pixel backends, `TryGetJpeg(...)` / `TryGetPng(...)` for hosted multimodal APIs
4. **Channel 1B (Displayed preview)**: Use `NewPreviewSet` only when you need processed preview exactly as shown on screen
5. **Channel 2 (Capture)**: Use `CaptureSuccess` event for high-quality processing
6. **Always check `camera.State == HardwareState.On` before operations**
7. **Use `camera.IsOn = true/false` for lifecycle management**
8. **Subscribe to events before starting camera**
9. **Handle permissions with `SkiaCamera.RequestPermissionsAsync()` (request) or `SkiaCamera.RequestPermissionsGrantedAsync()` (silent check)**
10. **Treat `RawCameraFrame.RawImage` as optional/advanced only**
11. **Use `ConfigureAwait(false)` for async operations**

## Choosing Between Raw Frames and NewPreviewSet

- Use `OnRawFrameAvailable(RawCameraFrame frame)` for face detection, landmarks, QR, OCR, and any model that must ignore overlays/effects.
- Use `frame.TryGetJpeg(...)` or `frame.TryGetPng(...)` when the destination is a hosted multimodal API expecting a standard image payload.
- Use `frame.TryGetRgbaBytes(...)` only when the server explicitly accepts raw `RGBA8888` bytes plus width/height.
- Use `NewPreviewSet` for QA, visual inspection, or workflows that intentionally analyze the post-processed preview.
- Remember: during recording with `UseRecordingFramesForPreview = true`, `NewPreviewSet` can already include `ProcessFrame` overlays.

## Common AI Agent Mistakes to Avoid

**Don't confuse the channels:**
```csharp
// WRONG: Feeding final preview into a model that should ignore overlays/effects
camera.NewPreviewSet += (s, source) => RunFaceDetector(source.Image);

// CORRECT
protected override void OnRawFrameAvailable(RawCameraFrame frame)
{
    frame.TryGetRgba(224, 224, _rgba);
}

// ALSO WRONG: Thinking preview effects affect captured photos
camera.Effect = SkiaImageEffect.Sepia;  // Only affects preview
await camera.TakePicture(); // Photo is NOT sepia unless you process it separately
```

**Don't bypass lifecycle management:**
```csharp
// WRONG
camera.Start();
camera.TakePicture(); // Without checking state

// CORRECT
camera.Effect = SkiaImageEffect.Sepia;  // Preview shows sepia

camera.CaptureSuccess += (s, captured) => {
    var sepiaPhoto = ApplySepia(captured.Image);
};

camera.IsOn = true;
if (camera.State == HardwareState.On && !camera.IsBusy)
    await camera.TakePicture().ConfigureAwait(false);
```
