# Troubleshooting

## Common Issues

| Issue | Cause | Solution |
|-------|-------|----------|
| **Camera not starting** | Missing permissions | Use `SkiaCamera.RequestPermissionsAsync()` or set `NeedPermissionsSet` |
| **Black preview** | Camera enumeration failed | Check device camera availability |
| **Capture failures** | Storage permissions | Verify write permissions |
| **Performance issues** | Unoptimized preview processing | Cache controls, limit frame processing |
| **Manual selection fails** | Invalid `CameraIndex` | Verify index is 0 to `cameras.Count-1` |
| **Flash not working** | Flash not supported or wrong mode | Check `IsFlashSupported` and use correct `CaptureFlashMode` |
| **App crashes on camera switch** | Rapid camera changes | Add delays between camera operations |
| **OpenFileInGallery fails (Android)** | FileProvider not configured | Add FileProvider to AndroidManifest.xml (see setup) |
| **"Failed to find configured root"** | Invalid file_paths.xml | Check file is in declared FileProvider paths |
| **FileUriExposedException** | Missing FileProvider | Configure FileProvider with correct authority |
| **Memory leaks** | Event handlers not removed | Properly dispose and unsubscribe events |

## Debug Tips

```csharp
#if DEBUG
camera.OnError += (s, error) => Debug.WriteLine($"Camera Error: {error}");
camera.StateChanged += (s, state) => Debug.WriteLine($"Camera State: {state}");
#endif

// Check camera availability
var cameras = await camera.GetAvailableCamerasAsync();
Debug.WriteLine($"Available cameras: {cameras.Count}");
foreach (var cam in cameras)
{
    Debug.WriteLine($"  {cam.Index}: {cam.Name} ({cam.Position}) Flash: {cam.HasFlash}");
}

// Monitor performance
var stopwatch = Stopwatch.StartNew();
await camera.TakePicture();
Debug.WriteLine($"Capture took: {stopwatch.ElapsedMilliseconds}ms");
```

## Advanced Patterns & Best Practices

### Camera Selection UI

```csharp
public void ShowCameraPicker()
{
    MainThread.BeginInvokeOnMainThread(async () =>
    {
        var cameras = await CameraControl.GetAvailableCamerasAsync();

        var options = cameras.Select(c =>
            $"{c.Name} ({c.Position}){(c.HasFlash ? " Flash" : "")}"
        ).ToArray();

        var result = await App.Current.MainPage.DisplayActionSheet("Select Camera", "Cancel", null, options);
        if (!string.IsNullOrEmpty(result))
        {
            var selectedIndex = options.FindIndex(result);
            if (selectedIndex >= 0)
            {
                CameraControl.Facing = CameraPosition.Manual;
                CameraControl.CameraIndex = selectedIndex;
                CameraControl.IsOn = true;
            }
        }
    });
}
```

### Performance Optimization

```csharp
private readonly SemaphoreSlim _frameProcessingSemaphore = new(1, 1);

private void OnNewPreviewFrame(object sender, LoadedImageSource source)
{
    if (!_frameProcessingSemaphore.Wait(0))
        return;

    Task.Run(async () =>
    {
        try
        {
            await ProcessFrameAsync(source);
        }
        finally
        {
            _frameProcessingSemaphore.Release();
        }
    });
}
```

### Error Handling & Recovery

```csharp
private int _cameraRestartAttempts = 0;
private const int MaxRestartAttempts = 3;

private async void OnCameraError(object sender, string error)
{
    Debug.WriteLine($"Camera error: {error}");

    if (_cameraRestartAttempts < MaxRestartAttempts)
    {
        _cameraRestartAttempts++;
        await Task.Delay(1000);
        camera.IsOn = false;
        await Task.Delay(500);
        camera.IsOn = true;
    }
    else
    {
        await DisplayAlert("Camera Error",
            "Camera is not responding. Please restart the app.", "OK");
    }
}

private void OnCameraStateChanged(object sender, HardwareState newState)
{
    if (newState == HardwareState.On)
        _cameraRestartAttempts = 0;
}
```

## Platform-Specific Notes

### Android
- Requires Camera2 API (API level 21+)
- Some devices may have camera enumeration delays
- Test on various Android versions and manufacturers

### iOS/macOS
- AVFoundation framework required
- Camera permissions must be declared in Info.plist
- Some camera types only available on newer devices

### Windows
- UWP/WinUI MediaCapture APIs
- Desktop apps will ask user for camera permissions
