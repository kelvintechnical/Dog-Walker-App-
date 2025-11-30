using AutoMapper;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Models;
using DogWalker.Core.Requests;
using DogWalkerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Services;

public class ClientService : IClientService
{
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public ClientService(DogWalkerDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<ClientDto> CreateAsync(CreateClientRequest request, CancellationToken cancellationToken = default)
    {
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

        await _db.Clients.AddAsync(entity, cancellationToken);
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<ClientDto>(entity);
    }

    public async Task<IEnumerable<ClientDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var clients = await _db.Clients.Include(c => c.Dogs).ToListAsync(cancellationToken);
        return _mapper.Map<IEnumerable<ClientDto>>(clients);
    }

    public async Task<ClientDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var client = await _db.Clients.Include(c => c.Dogs).FirstOrDefaultAsync(c => c.Id == id, cancellationToken);
        return client is null ? null : _mapper.Map<ClientDto>(client);
    }

    public async Task<DogDto> AddDogAsync(Guid clientId, CreateDogRequest request, CancellationToken cancellationToken = default)
    {
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
        await _db.SaveChangesAsync(cancellationToken);

        return _mapper.Map<DogDto>(dog);
    }
}
