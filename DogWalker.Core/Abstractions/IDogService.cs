using DogWalker.Core.DTOs;

namespace DogWalker.Core.Abstractions;

public interface IDogService
{
    Task<IEnumerable<DogDto>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<IEnumerable<DogDto>> GetByClientAsync(Guid clientId, CancellationToken cancellationToken = default);
}
