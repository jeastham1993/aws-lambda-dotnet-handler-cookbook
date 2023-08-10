namespace Stocks.IntegrationTests;

using System.Text.Json;

using Amazon.Lambda.SNSEvents;

using SharedKernel.Events;

using Stocks.Tests.Shared;

using StockTrader.Core.StockAggregate;
using StockTrader.HistoryManager;

public class StockPriceHistoryTests
{
    public StockPriceHistoryTests()
    {
        Environment.SetEnvironmentVariable("POWERTOOLS_TRACE_DISABLED", "true");
    }
    
    [Fact]
    public async Task CanSetStockHistory_WhenRequestIsValid_ShouldStore()
    {
        var mockFeatureFlags = FeatureFlagMocks.Default;

        var testHarness = new TestHarness(mockFeatureFlags);

        var function = testHarness.GetService<AddStockHistoryFunction>();

        var testEvt = new SNSEvent()
        {
            Records = new List<SNSEvent.SNSRecord>()
        };
        testEvt.Records.Add(new SNSEvent.SNSRecord()
        {
            Sns = new SNSEvent.SNSMessage()
            {
                Message = JsonSerializer.Serialize(new EventWrapper<StockPriceUpdatedEvent>(new StockPriceUpdatedEvent(){StockSymbol = "AMZ", Price = "100"}))
            }
        });

        // Act
        await function.UpdateHistory(testEvt);
        
        // Assert
        // No errors indicates successfull processing. If an error occurs, the Lambda function will fail and send the error to a DLQ.
    }
}