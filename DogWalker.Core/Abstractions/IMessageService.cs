using DogWalker.Core.DTOs;

namespace DogWalker.Core.Abstractions;

public interface IMessageService
{
    Task<IEnumerable<MessageDto>> GetThreadAsync(Guid clientId, Guid walkerId, CancellationToken cancellationToken = default);
    Task<MessageDto> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default);
}
