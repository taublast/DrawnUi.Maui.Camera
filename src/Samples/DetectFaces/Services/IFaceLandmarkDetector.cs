namespace TestFaces.Services;

public interface IFaceLandmarkDetector
{
    int MaxFaces { get; set; }

    event EventHandler<PreviewDetectionCompletedEventArgs>? PreviewDetectionCompleted;
    event EventHandler<PreviewDetectionFailedEventArgs>? PreviewDetectionFailed;

    Task<FaceLandmarkResult> DetectAsync(Stream imageStream);
    void EnqueuePreviewDetection(byte[] rgbaBytes, PreviewDetectionRequest request);
}

public enum PreviewLandmarkDetail
{
    Full,
    Lite,
}

public sealed record PreviewDetectionRequest(
    int Width,
    int Height,
    int Rotation,
    double ResizeMilliseconds,
    bool ReusedCachedFrame,
    long EnqueuedAtTicks,
    PreviewLandmarkDetail LandmarkDetail = PreviewLandmarkDetail.Full);

public sealed class PreviewDetectionCompletedEventArgs : EventArgs
{
    public PreviewDetectionCompletedEventArgs(PreviewDetectionRequest request, FaceLandmarkResult result)
    {
        Request = request;
        Result = result;
    }

    public PreviewDetectionRequest Request { get; }

    public FaceLandmarkResult Result { get; }
}

public sealed class PreviewDetectionFailedEventArgs : EventArgs
{
    public PreviewDetectionFailedEventArgs(PreviewDetectionRequest request, Exception exception)
    {
        Request = request;
        Exception = exception;
    }

    public PreviewDetectionRequest Request { get; }

    public Exception Exception { get; }
}
