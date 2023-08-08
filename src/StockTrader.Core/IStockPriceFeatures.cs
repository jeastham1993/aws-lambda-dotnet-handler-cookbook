namespace StockTrader.Core;

public interface IStockPriceFeatures
{
    bool ShouldIncreaseStockPrice();

    bool DoesStockCodeHaveDecrease(string stockCode);
}