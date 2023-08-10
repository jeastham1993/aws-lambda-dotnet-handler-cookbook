namespace Stocks.UnitTests;

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
    public async Task CanSetStockPrice_WhenRequestIsValid_ShouldStoreAndPublishEvent()
    {
        var mockFeatureFlags = FeatureFlagMocks.Default;

        var testHarness = new MockTestHarness(mockFeatureFlags);

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

        A.CallTo(() => testHarness.MockStockRepository.AddHistory(A<StockHistory>._)).MustHaveHappened();
    }
}