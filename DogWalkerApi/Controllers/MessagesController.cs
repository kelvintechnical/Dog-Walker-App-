using DogWalker.Core.Abstractions;
using DogWalker.Core.DTOs;
using DogWalker.Core.Requests;
using Microsoft.AspNetCore.Mvc;

namespace DogWalkerApi.Controllers;

[ApiController]
[Route("api/[controller]")]
public class MessagesController : ControllerBase
{
    private readonly IMessageService _messageService;

    public MessagesController(IMessageService messageService)
    {
        _messageService = messageService;
    }

    [HttpGet("{clientId:guid}/{walkerId:guid}")]
    public async Task<ActionResult<IEnumerable<MessageDto>>> GetThread(Guid clientId, Guid walkerId, CancellationToken cancellationToken)
    {
        var messages = await _messageService.GetThreadAsync(clientId, walkerId, cancellationToken);
        return Ok(messages);
    }

    [HttpPost]
    public async Task<ActionResult<MessageDto>> SendMessage([FromBody] SendMessageRequest request, CancellationToken cancellationToken)
    {
        var message = await _messageService.SendMessageAsync(request, cancellationToken);
        return Ok(message);
    }
}
