using Amazon.DynamoDBv2;
using Amazon.Lambda.Serialization.SystemTextJson;
using AotAspNet;
using Microsoft.Extensions.Options;
using StockTrader.Infrastructure;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver = CustomSerializationContext.Default;
});

builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi, options =>
{
    options.Serializer = new SourceGeneratorLambdaJsonSerializer<CustomSerializationContext>();
});

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "hh:mm:ss ";
});

builder.Services.AddSharedServices();

var infrastructureSettings = new InfrastructureSettings
{
    TableName = Environment.GetEnvironmentVariable("TABLE_NAME"),
};
        
var dynamoClient = new AmazonDynamoDBClient();
        
var stockRepository = new StockRepository(Options.Create(infrastructureSettings), dynamoClient);

var getStockEndpoints = new GetStockEndpoints(stockRepository);

var app = builder.Build();

app.MapGet("/", () => "Welcome to running AOT compiled ASP.NET Core Minimal API on AWS Lambda");

app.MapGet("/_health", () => "We are healthy");

app.MapGet("/asp/price/{stockSymbol}", async (string stockSymbol) =>
{
    var res = await getStockEndpoints.GetStockPrice(stockSymbol);

    return res;
});

app.MapGet("/asp/history/{stockSymbol}", async (string stockSymbol) =>
{
    var res = await getStockEndpoints.GetStockPrice(stockSymbol);

    return res;
});

app.Run();
