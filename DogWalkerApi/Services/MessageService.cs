using AutoMapper;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Models;
using DogWalker.Core.Requests;
using DogWalkerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Services;

public class MessageService : IMessageService
{
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public MessageService(DogWalkerDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MessageDto>> GetThreadAsync(Guid clientId, Guid walkerId, CancellationToken cancellationToken = default)
    {
        var thread = await _db.MessageThreads
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.ClientId == clientId && t.WalkerId == walkerId, cancellationToken);

        if (thread is null)
        {
            return Enumerable.Empty<MessageDto>();
        }

        var ordered = thread.Messages.OrderBy(m => m.SentAtUtc).ToList();
        return _mapper.Map<IEnumerable<MessageDto>>(ordered);
    }

    public async Task<MessageDto> SendMessageAsync(SendMessageRequest request, CancellationToken cancellationToken = default)
    {
        var thread = await _db.MessageThreads
            .Include(t => t.Messages)
            .FirstOrDefaultAsync(t => t.ClientId == request.ClientId && t.WalkerId == request.WalkerId, cancellationToken);

        if (thread is null)
        {
            thread = new MessageThread
            {
                ClientId = request.ClientId,
                WalkerId = request.WalkerId,
                BookingId = request.BookingId
            };
            await _db.MessageThreads.AddAsync(thread, cancellationToken);
        }

        var message = new Message
        {
            ThreadId = thread.Id,
            Body = request.Message,
            Direction = request.Direction,
            IsRead = request.Direction == MessageDirection.WalkerToClient
        };

        thread.Messages.Add(message);
        thread.LastMessageAtUtc = DateTimeOffset.UtcNow;

        await _db.SaveChangesAsync(cancellationToken);
        return _mapper.Map<MessageDto>(message);
    }
}
