using Amazon.DynamoDBv2.Model;

namespace Notification.FunctionalTests;

using Amazon.StepFunctions.Model;

using FluentAssertions;

public class NotificationTests : IClassFixture<Setup>, IDisposable
{
    private readonly Setup _setup;
    private readonly HttpClient _client;
    private readonly NotificationDriver driver;
    private bool disposed;

    public NotificationTests(Setup setup)
    {
        _setup = setup;
        _client = new HttpClient()
        {
            BaseAddress = new(setup.ApiUrl),
            DefaultRequestHeaders =
            {
                {"Authorization", $"Bearer {setup.AuthToken}"}
            },
        };

        this.driver = new NotificationDriver(this._client, setup.SqsClient, setup.StockUpdateQueueUrl);
    }
    
    [Fact]
    public async Task AddNotificationAndPublishEvent_WhenEventIsForNotificationStock_ShouldSendNotification()
    {
        var customerId = Guid.NewGuid().ToString();
        var stockId = Guid.NewGuid().ToString();

        var notification = await this.driver.RegisterForNotification(
            customerId,
            stockId);

        notification.IsSuccessStatusCode.Should().BeTrue($"Status code should be success, not {notification.StatusCode}. Response body is {await notification.Content.ReadAsStringAsync()}");

        this._setup.CreatedNotifications.Add(
            customerId,
            stockId);

        await this.driver.PublishStockUpdateMessage(stockId);

        await Task.Delay(TimeSpan.FromSeconds(3));

        var auditResult = await _setup.DynamoDbClient.GetItemAsync(_setup.TableName, new Dictionary<string, AttributeValue>(2)
        {
            { "PK", new($"AUDIT#{customerId}") },
            { "SK", new(stockId) },
        });

        auditResult.IsItemSet.Should().BeTrue($"Customer {customerId} and {stockId} should be found");
        
        this._setup.CreatedNotifications.Add(
            $"AUDIT#{customerId}",
            stockId);
    }
    
    [Fact]
    public async Task AddNotificationAndPublishEvent_WhenEventIsForAnUnregisteredStock_NoAuditRecord()
    {
        var customerId = Guid.NewGuid().ToString();
        var stockId = Guid.NewGuid().ToString();

        await this.driver.PublishStockUpdateMessage(stockId);

        await Task.Delay(TimeSpan.FromSeconds(3));

        var auditResult = await _setup.DynamoDbClient.GetItemAsync(_setup.TableName, new Dictionary<string, AttributeValue>(2)
        {
            { "PK", new($"AUDIT#{customerId}") },
            { "SK", new(stockId) },
        });

        auditResult.IsItemSet.Should().BeFalse($"Customer {customerId} and {stockId} should be found");
    }

    void IDisposable.Dispose()
    {
        if (disposed)
        {
            return;
        }

        disposed = true;
        _client.Dispose();
    }
}