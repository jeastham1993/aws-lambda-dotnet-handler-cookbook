namespace StockTrader.Infrastructure;

public class InfrastructureSettings
{
    public string? TableName { get; init; }
    
    public string? EventBusName { get; init; }
    
    public string? ServiceName { get; init; }
}