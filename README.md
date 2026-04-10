# SkiaCamera

Camera control, rendered with SkiaSharp and DrawnUI for .NET MAUI enabling real-time video processing, photo capture with metadata, and audio recording, AI/ML capture-friendly.
Use as Camera or a standalone Audio recorder inside any MAUI app by wrapping with a `Canvas`.  

## Features

- Cross-platform (Android, iOS, MacCatalyst, Windows) with hardware-accelerated SkiaSharp rendering
- Real-time preview effects (Sepia, B&W, Pastel) and custom SKSL shaders
- Photo capture with post-processing and metadata
- Video recording with real-time frame processing — overlays/effects baked in without post-processing
- Audio-only recording mode with real-time sample visualization
- [Pre-recording buffer](PreRecording.md) — capture seconds before the live recording started
- Abort recording without saving
- Manual and automatic camera selection with enumeration
- Capture format management with quality presets and manual format selection
- Zoom control with configurable limits
- Dual-channel flash control (preview torch + capture flash)
- GPS injection and custom EXIF for both photos and videos
- Built-in permission handling

Read the [blog article](https://taublast.github.io/posts/VideoRecording) about the sample app coming along with this repo.

---


![vlc_0Y0bMKzuHM](https://github.com/user-attachments/assets/21ced7c4-7a05-44bc-ad39-9cfb44c3a4b4)

## What's New for 1.9.7.3

### Android 

Fixes:
- Fixed stale preview dispayed for a few frames after recording ends.

### Apple

Performance optimizations:
- During recording: Eliminated 2-3 synchronous GPU→CPU readbacks per frame → now 1 small ReadPixels at preview resolution only
- Non-recording preview: MetalPreviewScaler no longer stalls on WaitUntilCompleted() → async double-buffered, CPU reads the previous frame while GPU renders the current one

Fixes:
- Fixed unprocessed preview dispayed when pre-recording canceled.

### Windows

Performance optimizations:
- Fixed recording processing to go GPU
- Removed preview GPU→CPU readback

### Shared

- Updated DrawnUI nuget dependency to fix occasional GPU cache corruption 
- Sample App added button to cancel pre-recording 
 
## Sample Apps

- [SkiaCamera Demo](https://github.com/taublast/DrawnUi.Maui.Camera/tree/main/src/Sample) - This repo: recording with processing, shaders, AI captions.
- [Filters Camera](https://github.com/taublast/ShadersCamera) - Still photo-camera with realtime SKSL shaders as photo-filters.
- [SolTempo](https://github.com/taublast/SolTempo) - Audio visualizer and BPM detector using SkiaCamera's audio monitoring capabilities.

## Installation

```bash
dotnet add package DrawnUi.Maui.Camera
```

Initialize DrawnUi inside `MauiProgram.cs`:

```csharp
builder.UseDrawnUi();
```

[Read more about DrawnUi initialization](https://taublast.github.io/DrawnUi/articles/getting-started.html).

## Quick Start

### XAML

```xml
xmlns:draw="http://schemas.appomobi.com/drawnUi/2023/draw"
xmlns:camera="clr-namespace:DrawnUi.Camera;assembly=DrawnUi.Maui.Camera"
```

```xml
<draw:Canvas
    HorizontalOptions="Fill"
    VerticalOptions="Fill"
    Gestures="Lock"
    RenderingMode="Accelerated">

    <camera:SkiaCamera
        x:Name="CameraControl"
        BackgroundColor="Black"
        PhotoQuality="Medium"
        Facing="Default"
        HorizontalOptions="Fill"
        VerticalOptions="Fill"
        ZoomLimitMax="10"
        ZoomLimitMin="1" />

</draw:Canvas>
```

### Code-Behind

```csharp
var camera = new SkiaCamera
{
    BackgroundColor = Colors.Black,
    PhotoQuality = CaptureQuality.Medium,
    Facing = CameraPosition.Default,
    HorizontalOptions = LayoutOptions.Fill,
    VerticalOptions = LayoutOptions.Fill,
    ZoomLimitMax = 10,
    ZoomLimitMin = 1
};

camera.IsOn = true;
```

## Permissions

You need to set up permissions for camera, microphone (for video with sound) and storage/gallery access.

### Windows

No specific setup needed.

### Apple

Add to `Platforms/iOS/Info.plist` and `Platforms/MacCatalyst/Info.plist` inside `<dict>`:

```xml
<key>NSCameraUsageDescription</key>
<string>This app uses camera so You could take photos</string>
<key>NSPhotoLibraryAddUsageDescription</key>
<string>We need access to the library to save photos</string>
<key>NSPhotoLibraryUsageDescription</key>
<string>We need access to the library to save photos</string>
```

For video with audio:
```xml
<key>NSMicrophoneUsageDescription</key>
<string>In case You want to save videos with sound</string>
```

For GPS geotagging:
```xml
<key>NSLocationWhenInUseUsageDescription</key>
<string>In case You want to be able to geotag taken photos and videos</string>
```

### Android

Add to `Platforms/Android/AndroidManifest.xml` inside `<manifest>`:

```xml
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" android:maxSdkVersion="32" />
<uses-permission android:name="android.permission.READ_MEDIA_VIDEO" />
<uses-permission android:name="android.permission.READ_MEDIA_IMAGES" />
<uses-permission android:name="android.permission.READ_MEDIA_AUDIO" />
<uses-permission android:name="android.permission.CAMERA" />
```

For video with audio:
```xml
<uses-permission android:name="android.permission.RECORD_AUDIO" />
```

For GPS geotagging:
```xml
<uses-permission android:name="android.permission.ACCESS_FINE_LOCATION" />
```

#### FileProvider Setup (for OpenFileInGallery)

Add inside `<application>` in `AndroidManifest.xml`:

```xml
<provider
    android:name="androidx.core.content.FileProvider"
    android:authorities="${applicationId}.fileprovider"
    android:exported="false"
    android:grantUriPermissions="true">
    <meta-data
        android:name="android.support.FILE_PROVIDER_PATHS"
        android:resource="@xml/file_paths" />
</provider>
```

Create `Platforms/Android/Resources/xml/file_paths.xml`:

```xml
<?xml version="1.0" encoding="utf-8"?>
<paths xmlns:android="http://schemas.android.com/apk/res/android">
    <external-files-path name="my_images" path="Pictures" />
    <external-files-path name="my_movies" path="Movies" />
    <cache-path name="my_cache" path="." />
</paths>
```

### Runtime Permissions

SkiaCamera has built-in permission handling. When you set `IsOn = true`, it automatically checks and requests permissions defined by `NeedPermissionsSet`.

```csharp
// Quick approach
camera.NeedPermissionsSet = NeedPermissions.Camera | NeedPermissions.Gallery | NeedPermissions.Microphone;
camera.IsOn = true;

// Or manual async approach
bool ok = await SkiaCamera.RequestPermissionsAsync(
    NeedPermissions.Camera | NeedPermissions.Gallery | NeedPermissions.Microphone);
if (ok) camera.IsOn = true;
```

## Documentation

| Document | Description |
|----------|-------------|
| [Usage Guide](docs/usage-guide.md) | Setup, properties, lifecycle, flash, capture, zoom, effects, live processing, permissions, MVVM example |
| [Video Recording](docs/video-recording.md) | Recording, audio control, real-time processing, GPS & metadata, AudioSampleConverter |
| [API Reference](docs/api-reference.md) | Properties, methods, events, data classes, enums |
| [Troubleshooting](docs/troubleshooting.md) | Common issues, debug tips, best practices, platform notes |
| [AI Agent Guide](docs/ai-agent-guide.md) | Integration patterns for AI agents |
| [Pre-Recording](PreRecording.md) | Pre-recording buffer feature |

## ToDo

- [ ] Manual camera controls (focus, exposure, ISO, white balance)
- [ ] Camera capability detection (zoom ranges, supported formats)
- [ ] Preview format customization

## References

iOS:
* [Manual Camera Controls in Xamarin.iOS](https://github.com/MicrosoftDocs/xamarin-docs/blob/0506e3bf14b520776fc7d33781f89069bbc57138/docs/ios/user-interface/controls/intro-to-manual-camera-controls.md) by David Britch
