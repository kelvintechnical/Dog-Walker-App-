namespace DogWalker.Core.Models;

// The trusted issuer + subject identifies a login. Profile IDs never come from token claims.
public class Account : BaseEntity
{
    public string Issuer { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public Guid? ClientId { get; set; }
    public Client? Client { get; set; }
    public Guid? WalkerId { get; set; }
    public Walker? Walker { get; set; }
    public bool IsAdministrator { get; set; }
    public bool IsDisabled { get; set; }
}
