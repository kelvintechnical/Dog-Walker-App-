using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace DogWalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class DogsController : ControllerBase
{
    private readonly IDogService _dogService;

    public DogsController(IDogService dogService)
    {
        _dogService = dogService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<DogDto>>> GetDogs(CancellationToken cancellationToken)
    {
        var dogs = await _dogService.GetAllAsync(cancellationToken);
        return Ok(dogs);
    }

    [HttpGet("client/{clientId:guid}")]
    public async Task<ActionResult<IEnumerable<DogDto>>> GetDogsForClient(Guid clientId, CancellationToken cancellationToken)
    {
        var dogs = await _dogService.GetByClientAsync(clientId, cancellationToken);
        return Ok(dogs);
    }
}
