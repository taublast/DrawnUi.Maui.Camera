using CameraTests.UI;
using DrawnUi;
using DrawnUi.Camera;
using System.Diagnostics;
using AppoMobi.Specials;
using TestFaces.Services;

namespace TestFaces;

public partial class MainPage : ContentPage
{
    private AppCamera.PreviewDetectionMetrics? _lastPreviewMetrics;

    #region XAML HotReload

    // We need this to handle XAML hot reload propely.
    // It would just re-create a new instance of AppCanvas without disconnecting handler on the old one.
    // So we need some hacks to assure be behave like a singleton.

 
    protected override void OnHandlerChanged()
    {
        base.OnHandlerChanged();

        if (Handler == null)
        {
            AppCanvas.WasReloaded -= XamlHotReloadDetected;
            MainCanvas?.DisconnectHandlers();
            MainCanvas?.Dispose();
        }
        else
        {
            AppCanvas.WasReloaded += XamlHotReloadDetected;
            InitUi();
        }
    }

    private void XamlHotReloadDetected(object? sender, EventArgs e)
    {
        InitUi();
    }

    #endregion

    void InitUi()
    {
        Tasks.StartDelayed(TimeSpan.FromMilliseconds(500), () =>
        {
            OnUiLoaded();
        });
    }

    private readonly IFaceLandmarkDetector _detector;

    public MainPage(IFaceLandmarkDetector detector)
    {
        _detector = detector;

        try
        {
            InitializeComponent();
        }
        catch (Exception e)
        {
            Super.DisplayException(this, e);
        }
    }

    private void OnHotReload(object? sender, EventArgs eventArgs)
    {
         Debug.WriteLine("HOTRELOAD !!!");

         AttachHardware(false);
    }

    public void OnUiLoaded()
    {
        CameraControl.Detector = _detector;
        _detector.MaxFaces = CameraControl.MaxNumFaces;
        AttachHardware(true);
        OnModeChanged(null, EventArgs.Empty);
    }

    // Fallback for Shell DataTemplate resolution (bypasses DI)
    public MainPage()
        : this(ResolveDependency<IFaceLandmarkDetector>())
    {
    }

    private static T ResolveDependency<T>() where T : notnull
    {
        return Application.Current!.Handler!.MauiContext!.Services.GetRequiredService<T>();
    }

    void ShowAlert(string title, string message)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            await DisplayAlertAsync(title, message, "OK");
        });
    }


    #region DETECT FACE LANDMARKS

    private async void OnModeChanged(object? sender, EventArgs e)
    {
        if (LandmarkDraw != null && ModePicker != null)
        {
            var drawMode = ModePicker.SelectedIndex switch
            {
                1 => DetectionType.Rectangle,
                2 => DetectionType.Mask, // Spider-Man
                3 => DetectionType.Mask, // Cake Hat
                _ => DetectionType.Landmark
            };

            LandmarkDraw.DrawMode = drawMode;
            CameraControl.DrawMode = drawMode;

            MaskConfiguration? config = null;

            if (LandmarkDraw.DrawMode == DetectionType.Mask)
            {
                try
                {
                    config = ModePicker.SelectedIndex switch
                    {
                        3 => new MaskConfiguration
                        {
                            Filename = "hat_cake.png",
                            Position = MaskPosition.Top,
                            WidthMultiplier = 1.6f,
                            YOffsetRatio = 0.05f
                        },
                        _ => new MaskConfiguration
                        {
                            Filename = "mask_spiderman.png",
                            Position = MaskPosition.Inside,
                            WidthMultiplier = 1.25f,
                            YOffsetRatio = -0.2f
                        }
                    };

                    if (LandmarkDraw.ActiveMaskConfig?.Filename != config.Filename || LandmarkDraw.MaskImage == null)
                    {
                        LandmarkDraw.ActiveMaskConfig = config;
                        using var stream = await FileSystem.OpenAppPackageFileAsync(config.Filename);
                        LandmarkDraw.MaskImage = Microsoft.Maui.Graphics.Platform.PlatformImage.FromStream(stream);
                    }

                    await CameraControl.SetMaskConfigurationAsync(config);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Failed to load mask image: {ex}");
                }
            }
            else
            {
                LandmarkDraw.ActiveMaskConfig = null;
                await CameraControl.SetMaskConfigurationAsync(null);
            }

            if (LandmarkOverlay.IsVisible)
            {
                LandmarkOverlay.Invalidate();
            }
        }
    }

    private async void OnPickPhotoClicked(object? sender, EventArgs e)
    {
        try
        {
            var results = await MediaPicker.Default.PickPhotosAsync(new MediaPickerOptions
            {
                Title = "Pick a Photo",
            });
            var result = results?.FirstOrDefault();
            if (result is null)
                return;

            // Open two separate streams: one for display, one for detection
            var displayStream = await result.OpenReadAsync();
            SelectedImage.Source = ImageSource.FromStream(() => displayStream);
            SelectedImage.IsVisible = true;

            // Reset overlay
            LandmarkDraw.Update(null);
            LandmarkOverlay.IsVisible = false;

            // Show spinner
            StatusLabel.Text = "Detecting landmarks...";
            //Spinner.IsRunning = true;
            //Spinner.IsVisible = true;
            PickPhotoBtn.IsEnabled = false;

            _detector.MaxFaces = CameraControl.MaxNumFaces;

            FaceLandmarkResult detection;
            using (var detectionStream = await result.OpenReadAsync())
            {
                detection = await _detector.DetectAsync(detectionStream);
            }

            // Update overlay
            LandmarkDraw.Update(detection);
            LandmarkOverlay.IsVisible = true;
            LandmarkOverlay.Invalidate();

            var faceCount = detection.Faces.Count;
            StatusLabel.Text = faceCount switch
            {
                0 => "No faces detected.",
                1 => "1 face detected.",
                _ => $"{faceCount} faces detected.",
            };
        }
        catch (PlatformNotSupportedException ex)
        {
            StatusLabel.Text = ex.Message;
        }
        catch (Exception ex)
        {
            Debug.WriteLine(ex);
            StatusLabel.Text = $"Error: {ex.Message}";
        }
        finally
        {
            //Spinner.IsRunning = false;
            //Spinner.IsVisible = false;
            PickPhotoBtn.IsEnabled = true;
        }
    }

    #endregion

    #region CAMERA

    public void AttachHardware(bool subscribe)
    {
        if (subscribe)
        {
            AttachHardware(false);

            CameraControl.PermissionsResult += OnPermissionsResultChanged;
            CameraControl.StateChanged += CameraControlOnStateChanged;
            CameraControl.OnError += OnCameraError;
            CameraControl.PreviewDetectionMeasured += OnPreviewDetectionMeasured;
            CameraControl.PreviewDetectionUpdated += OnPreviewDetectionUpdated;
            CameraControl.PreviewDetectionFailed += OnPreviewDetectionFailed;

            CameraControl.IsOn = true;

            Debug.WriteLine($"Camera attached {CameraControl.Uid}");
        }
        else
        {
            if (CameraControl != null)
            {
                CameraControl.PermissionsResult -= OnPermissionsResultChanged;
                CameraControl.StateChanged -= CameraControlOnStateChanged;
                CameraControl.OnError -= OnCameraError;
                CameraControl.PreviewDetectionMeasured -= OnPreviewDetectionMeasured;
                CameraControl.PreviewDetectionUpdated -= OnPreviewDetectionUpdated;
                CameraControl.PreviewDetectionFailed -= OnPreviewDetectionFailed;

                Debug.WriteLine($"Camera detached {CameraControl.Uid}");
            }
        }
    }

    private void OnCameraError(object? sender, string e)
    {
        ShowAlert("Camera Error", e);
    }

    private void OnPermissionsResultChanged(object? sender, bool e)
    {
        if (!e)
        {
            ShowAlert("Error", "The application does not have the required permissions to access all the camera features.");
        }
    }

    private void CameraControlOnStateChanged(object? sender, HardwareState e)
    {
        if (e == HardwareState.On)
        {
            StatusLabel.Text = "Camera ready";
        }
        else
        {
            StatusLabel.Text = "Camera stopped";
        }
    }

    private void OnPreviewDetectionUpdated(object? sender, FaceLandmarkResult detection)
    {
        var faceCount = detection.Faces.Count;
        var facesText = faceCount switch
        {
            0 => "No faces detected.",
            1 => "1 face.",
            _ => $"{faceCount} faces detected."
        };

        if (_lastPreviewMetrics == null)
        {
            StatusLabel.Text = facesText;
            return;
        }

        var metrics = _lastPreviewMetrics;
        var sourceText = metrics.ReusedCachedFrame ? "cached" : "live";
        var backendText = detection.InferenceMilliseconds > 0
            ? $", conv {detection.ConversionMilliseconds:F1}, mp {detection.InferenceMilliseconds:F1}, {(detection.UsedGpuDelegate ? "gpu" : "cpu")}"
            : string.Empty;

        StatusLabel.Text = $"{facesText} size {metrics.ResizeMilliseconds:F1}, det {metrics.DetectionMilliseconds:F1}{backendText}, {metrics.Width}x{metrics.Height}, {sourceText}";
    }

    private void OnPreviewDetectionMeasured(object? sender, AppCamera.PreviewDetectionMetrics metrics)
    {
        _lastPreviewMetrics = metrics;
    }

    private void OnPreviewDetectionFailed(object? sender, Exception ex)
    {
        Debug.WriteLine(ex);
        StatusLabel.Text = $"Detection error: {ex.Message}";
    }


    #endregion

}




