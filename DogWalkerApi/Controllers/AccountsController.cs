using DogWalker.Core.DTOs;
using DogWalker.Core.Models;
using DogWalkerApi.Data;
using DogWalkerApi.Security;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DogWalkerApi.Controllers;

[ApiController]
[Route("api/accounts")]
public sealed class AccountsController(AccountAccess access, DogWalkerDbContext db, IConfiguration configuration, ILogger<AccountsController> logger) : ControllerBase
{
    [HttpGet("me")]
    public async Task<ActionResult<AccountProfileDto>> Me(CancellationToken ct)
    {
        var actor = await access.RequireAsync(ct);
        return Ok(new AccountProfileDto(actor.Id, actor.ClientId, actor.WalkerId, actor.IsAdministrator));
    }

    // Existing-profile claims require prior human verification; never match by email.
    [HttpPost("bindings")]
    public async Task<ActionResult<AccountProfileDto>> Bind(BindAccountRequest request, CancellationToken ct)
    {
        await access.RequireAdministratorAsync(ct);
        if (request.Issuer != configuration["Authentication:Authority"] || string.IsNullOrWhiteSpace(request.Subject)
            || request.Subject.Length > 256 || request.Issuer.Length > 256
            || (request.ClientId is null && request.WalkerId is null))
            return BadRequest();
        if ((request.ClientId is Guid clientId && !await db.Clients.AnyAsync(c => c.Id == clientId, ct))
            || (request.WalkerId is Guid walkerId && !await db.Walkers.AnyAsync(w => w.Id == walkerId, ct)))
            return NotFound();
        var account = await db.Accounts.SingleOrDefaultAsync(a => a.Issuer == request.Issuer && a.Subject == request.Subject, ct);
        // A SQL match must not bind a different opaque subject (for example, trailing-space padding).
        if (account is not null && (!string.Equals(account.Issuer, request.Issuer, StringComparison.Ordinal)
            || !string.Equals(account.Subject, request.Subject, StringComparison.Ordinal)
            || account.IsDisabled
            || (account.ClientId is not null && account.ClientId != request.ClientId)
            || (account.WalkerId is not null && account.WalkerId != request.WalkerId)))
            return Conflict();
        if (account is null)
        {
            account = new Account { Issuer = request.Issuer, Subject = request.Subject };
            db.Accounts.Add(account);
        }
        account.ClientId = request.ClientId;
        account.WalkerId = request.WalkerId;
        account.UpdatedUtc = DateTime.UtcNow;
        await db.SaveChangesAsync(ct);
        logger.LogInformation("Account binding {AccountId} verified by administrator {AdministratorId}; client {ClientId}, caregiver {WalkerId}",
            account.Id, (await access.RequireAsync(ct)).Id, account.ClientId, account.WalkerId);
        return Ok(new AccountProfileDto(account.Id, account.ClientId, account.WalkerId, account.IsAdministrator));
    }
}

public record BindAccountRequest(string Issuer, string Subject, Guid? ClientId, Guid? WalkerId);
