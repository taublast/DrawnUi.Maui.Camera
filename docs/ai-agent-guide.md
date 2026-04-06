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

// 2. CHANNEL 1: Live preview processing for AI/ML
camera.NewPreviewSet += (s, source) =>
{
    source.ProtectFromDispose = true;
    
    if (!_mlSemaphore.Wait(0))
    {
        source.ProtectFromDispose = false;
        return;
    }

    Task.Run(async () =>
    {
        try
        {
            await RunMLAsync(source.Image);
        }
        finally
        {
            source.ProtectFromDispose = false;
            source.Dispose();
            _mlSemaphore.Release();
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
2. **Channel 1 (Preview)**: Use `NewPreviewSet` event for real-time AI/ML
3. **Channel 2 (Capture)**: Use `CaptureSuccess` event for high-quality processing
4. **Always check `camera.State == HardwareState.On` before operations**
5. **Use `camera.IsOn = true/false` for lifecycle management**
6. **Subscribe to events before starting camera**
7. **Handle permissions with `SkiaCamera.RequestPermissionsAsync()` (request) or `SkiaCamera.RequestPermissionsGrantedAsync()` (silent check)**
8. **Use `ConfigureAwait(false)` for async operations**

## Common AI Agent Mistakes to Avoid

**Don't confuse the channels:**
```csharp
// WRONG: Thinking preview effects affect captured photos
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
