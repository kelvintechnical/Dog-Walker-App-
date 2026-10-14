using AutoMapper;
using DogWalkerApi.Security;
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
    private readonly AccountAccess _access;
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public MessageService(DogWalkerDbContext db, IMapper mapper, AccountAccess access)
    {
        _db = db;
        _access = access;
        _mapper = mapper;
    }

    public async Task<IEnumerable<MessageDto>> GetThreadAsync(Guid clientId, Guid walkerId, CancellationToken cancellationToken = default)
    {
        await _access.RequireThreadAsync(clientId, walkerId, cancellationToken);
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
        await _access.RequireThreadAsync(request.ClientId, request.WalkerId, cancellationToken);
        var actor = await _access.RequireAsync(cancellationToken);
        var direction = actor.WalkerId == request.WalkerId ? MessageDirection.WalkerToClient : MessageDirection.ClientToWalker;
        if (actor.IsAdministrator || request.Direction != direction)
            throw new ResourceAccessException(StatusCodes.Status403Forbidden);
        if (request.BookingId is Guid bookingId)
        {
            var booking = await _access.RequireBookingAsync(bookingId, ct: cancellationToken);
            if (booking.ClientId != request.ClientId || booking.WalkerId != request.WalkerId)
                throw new ResourceAccessException(StatusCodes.Status404NotFound);
        }
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
            SenderId = actor.Id,
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
