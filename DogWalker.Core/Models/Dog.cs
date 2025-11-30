namespace DogWalker.Core.Models;

public class Dog : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Breed { get; set; } = string.Empty;
    public DateTime? BirthDate { get; set; }
    public double WeightKg { get; set; }
    public string SpecialNeeds { get; set; } = string.Empty;
    public Guid ClientId { get; set; }
    public bool RequiresMuzzle { get; set; }
    public bool IsReactive { get; set; }
    public string VetContact { get; set; } = string.Empty;
}
