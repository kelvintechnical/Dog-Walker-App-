namespace DogWalkerApp.Services.Media;

public interface IMediaCaptureService
{
    Task<FileResult?> CapturePhotoAsync();
    Task<FileResult?> CaptureVideoAsync();
}

public class MediaCaptureService : IMediaCaptureService
{
    public Task<FileResult?> CapturePhotoAsync() => MediaPicker.CapturePhotoAsync();

    public Task<FileResult?> CaptureVideoAsync() => MediaPicker.CaptureVideoAsync();
}
