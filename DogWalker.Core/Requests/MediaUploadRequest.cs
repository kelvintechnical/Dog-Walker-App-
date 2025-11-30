namespace DogWalker.Core.Requests;

public record MediaUploadRequest(
    string Url,
    string? ThumbnailUrl,
    string MediaType,
    string? Caption
);
