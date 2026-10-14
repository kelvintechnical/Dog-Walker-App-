namespace DogWalkerApi.Security;

public sealed class ResourceAccessException(int statusCode) : Exception
{
    public int StatusCode { get; } = statusCode;
}
