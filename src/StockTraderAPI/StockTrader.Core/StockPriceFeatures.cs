namespace StockTrader.Core.StockAggregate;

using SharedKernel.Features;

public class StockPriceFeatures : IStockPriceFeatures
{
    private readonly IFeatureFlags featureFlags;

    public StockPriceFeatures(IFeatureFlags featureFlags)
    {
        this.featureFlags = featureFlags;
    }
    
    /// <inheritdoc />
    public bool ShouldIncreaseStockPrice()
    {
        var shouldIncreaseStockPrice = this.featureFlags.Evaluate("ten_percent_share_increase");

        return shouldIncreaseStockPrice != null && shouldIncreaseStockPrice.ToString() == "True";
    }

    /// <inheritdoc />
    public bool DoesStockCodeHaveDecrease(string stockCode)
    {
        var isCustomerInlineForDecrease = this.featureFlags.Evaluate(
            "reduce_stock_price_for_company",
            new Dictionary<string, object>(1)
            {
                { "stock_symbol", stockCode }
            });

        return isCustomerInlineForDecrease != null && isCustomerInlineForDecrease.ToString() == "True";
    }
}