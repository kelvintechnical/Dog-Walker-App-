namespace DogWalker.Core.Requests;

public record RoutePointRequest(
    double Latitude,
    double Longitude,
    double TotalDistanceMeters,
    double? SpeedMph
);
