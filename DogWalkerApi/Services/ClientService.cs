using AutoMapper;
using DogWalkerApi.Security;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Models;
using DogWalker.Core.Requests;
using DogWalkerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Services;

public class ClientService : IClientService
{
    private readonly AccountAccess _access;
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public ClientService(DogWalkerDbContext db, IMapper mapper, AccountAccess access)
    {
        _db = db;
        _access = access;
        _mapper = mapper;
    }

    public async Task<ClientDto> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
    {
        var actor = await _access.FindAsync(cancellationToken);
        if (actor?.ClientId is not null) throw new ResourceAccessException(StatusCodes.Status409Conflict);
        if (actor is null)
        {
            var identity = _access.CurrentIdentity();
            actor = new Account { Issuer = identity.Issuer, Subject = identity.Subject };
            _db.Accounts.Add(actor);
        }
        var entity = new Client
        {
            FullName = request.FullName,
            Email = request.Email,
            PhoneNumber = request.PhoneNumber,
            Address = new Address
            {
                Line1 = request.Line1,
                Line2 = request.Line2,
                City = request.City,
                State = request.State,
                PostalCode = request.PostalCode,
                Country = request.Country
            }
        };

        actor.Client = entity;
        actor.ClientId = entity.Id;
        await _db.Clients.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ClientDto>(entity);
    }

    public async Task<IEnumerable<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var actor = await _access.RequireAsync(cancellationToken);
        var clients = await _db.Clients.Include(c => c.Dogs)
            .Where(c => actor.IsAdministrator || c.Id == actor.ClientId || _db.Bookings.Any(b =>
                b.ClientId == c.Id && b.WalkerId == actor.WalkerId
                && (b.Status == DogWalker.Core.Enums.BookingStatus.PendingConfirmation
                    || b.Status == DogWalker.Core.Enums.BookingStatus.Confirmed
                    || b.Status == DogWalker.Core.Enums.BookingStatus.InProgress)))
            .ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ClientDto>>(clients);
    }

    public async Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await _access.RequireClientAsync(id, ct: cancellationToken);
        var client = await _db.Clients.Include(c => c.Dogs).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return client is null ? null : _mapper.Map<ClientDto>(client);
    }

    public async Task<DogDto> AddDogAsync(Guid clientId, CreateDogRequest request, CancellationToken cancellationToken = default)
    {
        await _access.RequireClientAsync(clientId, write: true, ct: cancellationToken);
        var client = await _db.Clients.Include(c => c.Dogs).FirstOrDefaultAsync(c => c.Id == clientId, cancellationToken);
        if (client is null)
        {
            throw new KeyNotFoundException($"Client {clientId} not found");
        }

        var dog = new Dog
        {
            ClientId = clientId,
            Name = request.Name,
            Breed = request.Breed,
            BirthDate = request.BirthDate,
            WeightKg = request.WeightKg,
            SpecialNeeds = request.SpecialNeeds ?? string.Empty,
            RequiresMuzzle = request.RequiresMuzzle,
            IsReactive = request.IsReactive
        };

        client.Dogs.Add(dog);
        _db.Dogs.Add(dog);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DogDto>(dog);
    }
}
