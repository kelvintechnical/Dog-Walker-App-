using AutoMapper;
using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalkerApi.Data;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Services;

public class DogService : IDogService
{
    private readonly DogWalkerDbContext _db;
    private readonly IMapper _mapper;

    public DogService(DogWalkerDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<IEnumerable<DogDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var dogs = await _db.Dogs
            .AsNoTracking()
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<DogDto>>(dogs);
    }

    public async Task<IEnumerable<DogDto>> GetByClientAsync(Guid clientId, CancellationToken cancellationToken = default)
    {
        var dogs = await _db.Dogs
            .AsNoTracking()
            .Where(d => d.ClientId == clientId)
            .OrderBy(d => d.Name)
            .ToListAsync(cancellationToken);

        return _mapper.Map<IEnumerable<DogDto>>(dogs);
    }
}
