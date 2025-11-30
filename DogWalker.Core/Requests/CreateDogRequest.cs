namespace DogWalker.Core.Requests;

public record CreateDogRequest(
    string Name,
    string Breed,
    DateTime? BirthDate,
    double WeightKg,
    string? SpecialNeeds,
    bool RequiresMuzzle,
    bool IsReactive
);
