using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.SignalR;

namespace DogWalkerApi.Security;

public sealed class AccountUserIdProvider : IUserIdProvider
{
    public static string Key(string issuer, string subject)
        => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(issuer + "\0" + subject)));

    public string? GetUserId(HubConnectionContext connection)
    {
        try
        {
            var identity = AccountAccess.Identity(connection.User);
            return Key(identity.Issuer, identity.Subject);
        }
        catch (ResourceAccessException) { return null; }
    }
}
