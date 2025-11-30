namespace DogWalker.Core.Requests;

public record CreateClientRequest(
    string FullName,
    string Email,
    string PhoneNumber,
    string Line1,
    string? Line2,
    string City,
    string State,
    string PostalCode,
    string Country
);
