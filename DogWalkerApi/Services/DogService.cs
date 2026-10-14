using AutoMapper;
using DogWalkerApi.Security;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Enums;
using DogWalker.Core.Models;
using DogWalkerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Services;

public class DogService : IDogService
{
    private readonly AccountAccess _access;
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public DogService(DogWalkerDbContext db, IMapper mapper, AccountAccess access)
    {
        _db = db;
        _access = access;
        _mapper = mapper;
    }

    public async Task<IEnumerable<DogDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var actor = await _access.RequireAsync(cancellationToken);
        var dogs = await VisibleDogs(actor)
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<DogDto>>(dogs);
    }

    public async Task<IEnumerable<DogDto>> GetByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        await _access.RequireClientAsync(clientId, ct: cancellationToken);
        var actor = await _access.RequireAsync(cancellationToken);
        var dogs = await VisibleDogs(actor)
            .AsNoTracking()
            .Where(d => d.ClientId == clientId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<DogDto>>(dogs);
    }
    private IQueryable<Dog> VisibleDogs(Account actor) => _db.Dogs.Where(d =>
        actor.IsAdministrator || d.ClientId == actor.ClientId || _db.Bookings.Any(b =>
            b.DogId == d.Id && b.ClientId == d.ClientId && b.WalkerId == actor.WalkerId
            && (b.Status == BookingStatus.PendingConfirmation || b.Status == BookingStatus.Confirmed
                || b.Status == BookingStatus.InProgress)));
}
