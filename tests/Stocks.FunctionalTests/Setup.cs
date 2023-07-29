using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;

namespace Stocks.FunctionalTests;

public class Setup : IAsyncLifetime
{
    private string? _tableName;
    private AmazonDynamoDBClient? _dynamoDbClient;

    public string ApiUrl { get; private set; } = default!;
    
    public string? AuthToken { get; private set; }

    public List<string> CreatedStockSymbols { get; } = new();

    public async Task InitializeAsync()
    {
        var stackName = Environment.GetEnvironmentVariable("AWS_SAM_STACK_NAME") ?? "StockPriceStack";
        var region = Environment.GetEnvironmentVariable("AWS_SAM_REGION_NAME") ?? "us-east-1";
        var endpoint = RegionEndpoint.GetBySystemName(region);
        
        var chain = new CredentialProfileStoreChain();

        AmazonCloudFormationClient cloudFormationClient;
        AmazonCognitoIdentityProviderClient cognitoIdentityProviderClient;
        
        AWSCredentials awsCredentials;
        
        if (chain.TryGetAWSCredentials("dev", out awsCredentials))
        {
            cloudFormationClient = new AmazonCloudFormationClient(awsCredentials, endpoint);
            cognitoIdentityProviderClient = new AmazonCognitoIdentityProviderClient(awsCredentials, endpoint);
        }
        else
        {
            cloudFormationClient = new AmazonCloudFormationClient(endpoint);
            cognitoIdentityProviderClient = new AmazonCognitoIdentityProviderClient();
        }
        
        var response = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest() { StackName = stackName });
        var outputs = response.Stacks[0].Outputs;
        
        var userPoolId = GetOutputVariable(outputs, "UserPoolId");
        var clientId = GetOutputVariable(outputs, "ClientId");

        var auth = cognitoIdentityProviderClient.AdminInitiateAuthAsync(new AdminInitiateAuthRequest()
        {
            UserPoolId = userPoolId,
            ClientId = clientId,
            AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
            AuthParameters = new Dictionary<string, string>(2)
            {
                { "USERNAME", "john@example.com" },
                { "PASSWORD", Environment.GetEnvironmentVariable("USER_PASSWORD") ?? "mypassword123" },
            }
        }).GetAwaiter().GetResult();

        ApiUrl = GetOutputVariable(outputs, "StockPriceApiEndpoint");
        AuthToken = auth.AuthenticationResult.IdToken;
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