namespace DogWalker.Core.DTOs;

public record DogDto(
    Guid Id,
    Guid ClientId,
    string Name,
    string Breed,
    DateTime? BirthDate,
    double WeightKg,
    string SpecialNeeds,
    bool RequiresMuzzle,
    bool IsReactive
);
