using DogWalker.Core.Enums;

namespace DogWalkerApp.Models;

public record ServiceOption(string Title, string Description, double Price, ServiceType ServiceType);
