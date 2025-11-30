using DogWalker.Core.DTOs;
using DogWalker.Core.Requests;

namespace DogWalker.Core.Abstractions;

public interface IClientService
{
    Task<ClientDto> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken = default);
    Task<IEnumerable<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DogDto> AddDogAsync(Guid clientId, CreateDogRequest request, CancellationToken cancellationToken = default);
}
