using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Requests;
using Microsoft.AspNetCore.Mvc;

namespace DogWalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClientsController : ControllerBase
{
    private readonly IClientService _clientService;

    public ClientsController(IClientService clientService)
    {
        _clientService = clientService;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<ClientDto>>> GetClients(CancellationToken cancellationToken)
    {
        var clients = await _clientService.GetAllAsync(cancellationToken);
        return Ok(clients);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ClientDto>> GetClient(Guid id, CancellationToken cancellationToken)
    {
        var client = await _clientService.GetByIdAsync(id, cancellationToken);
        return client is null ? NotFound() : Ok(client);
    }

    [HttpPost]
    public async Task<ActionResult<ClientDto>> CreateClient([FromBody] CreateClientRequest request, CancellationToken cancellationToken)
    {
        var client = await _clientService.CreateAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetClient), new { id = client.Id }, client);
    }

    [HttpPost("{clientId:guid}/dogs")]
    public async Task<ActionResult<DogDto>> AddDog(Guid clientId, [FromBody] CreateDogRequest request, CancellationToken cancellationToken)
    {
        var dog = await _clientService.AddDogAsync(clientId, request, cancellationToken);
        return Ok(dog);
    }
}
