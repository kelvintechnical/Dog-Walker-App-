namespace DogWalker.Core.DTOs;

public record AccountProfileDto(Guid AccountId, Guid? ClientId, Guid? WalkerId, bool IsAdministrator);
