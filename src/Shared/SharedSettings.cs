using System.Text.Json.Serialization;

namespace Shared;

public class SharedSettings
{
    public string? EventBusName { get; init; }
    
    public string? ServiceName { get; init; }
}