dotnet lambda package --function-architecture arm64 -pl src/StockTraderAPI/StockTrader.API
cdk deploy --profile dev --all