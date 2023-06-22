using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.DynamoDBv2;

namespace ApiTests.IntegrationTest;

public class Setup : IAsyncLifetime
{
    private string? _tableName;
    private AmazonDynamoDBClient? _dynamoDbClient;

    public string ApiUrl { get; private set; } = default!;

    public List<string> CreatedStockSymbols { get; } = new();

    public async Task InitializeAsync()
    {
        var stackName = Environment.GetEnvironmentVariable("AWS_SAM_STACK_NAME") ?? "StockPriceStack";
        var region = Environment.GetEnvironmentVariable("AWS_SAM_REGION_NAME") ?? "eu-west-1";
        var endpoint = RegionEndpoint.GetBySystemName(region);
        var cloudFormationClient = new AmazonCloudFormationClient(new AmazonCloudFormationConfig() { RegionEndpoint = endpoint });
        var response = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest() { StackName = stackName });
        var outputs = response.Stacks[0].Outputs;

        ApiUrl = GetOutputVariable(outputs, "StockPriceApiEndpoint");
        _tableName = GetOutputVariable(outputs, "TableNameOutput");
        _dynamoDbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig() { RegionEndpoint = endpoint });
    }

    public async Task DisposeAsync()
    {
        foreach (var id in this.CreatedStockSymbols)
        {
            try
            {
                await _dynamoDbClient!.DeleteItemAsync(_tableName!, new() { ["StockSymbol"] = new(id) });
            }
            catch
            {
            }
        }
    }

    private static string GetOutputVariable(List<Output> outputs, string name) =>
        outputs.FirstOrDefault(o => o.OutputKey.StartsWith(name))?.OutputValue
        ?? throw new Exception($"CloudFormation stack does not have an output variable named '{name}'");
}