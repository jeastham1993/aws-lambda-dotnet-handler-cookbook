using Shared.Events;

namespace Notification.FunctionalTests;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

using Amazon.SQS;

public class NotificationDriver
{
    private readonly HttpClient httpClient;
    private readonly IAmazonSQS sqsClient;
    private readonly string queueUrl;

    public NotificationDriver(HttpClient httpClient, IAmazonSQS SqsClient, string queueUrl)
    {
        this.httpClient = httpClient;
        this.sqsClient = SqsClient;
        this.queueUrl = queueUrl;
    }

    public async Task<HttpResponseMessage> RegisterForNotification(string customerId, string stockSymbol)
    {
        var body = new AddStockNotification()
        {
            CustomerId = customerId,
            StockSymbol = stockSymbol
        }.ToString();

        var response = await this.httpClient.PostAsync("", new StringContent(body, Encoding.UTF8, "application/json"));

        return response;
    }

    public async Task PublishStockUpdateMessage(string stockSymbol)
    {
        await this.sqsClient.SendMessageAsync(
            this.queueUrl,
            JsonSerializer.Serialize(
                new EventWrapper(
                    new StockUpdateEvent()
                    {
                        StockSymbol = stockSymbol,
                        Price = 100
                    })));
    }
}

public record AddStockNotification
{
    [JsonPropertyName("customerId")]
    public string CustomerId { get; set; }
    
    [JsonPropertyName("stockSymbol")]
    public string StockSymbol { get; set; }

    /// <inheritdoc />
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }
}

public class StockUpdateEvent : Event
{
    public string StockSymbol { get; set; }
    
    public decimal Price { get; set; }

    /// <inheritdoc />
    public override string EventType => "StockUpdate";

    /// <inheritdoc />
    public override string EventVersion => "Sample";
}