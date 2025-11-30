namespace DogWalker.Core.DTOs;

public record ClientDto(
    Guid Id,
    string FullName,
    string Email,
    string PhoneNumber,
    string AddressLine1,
    string City,
    string State,
    string PostalCode,
    int ActiveDogs,
    double LifetimeSpend
);
