using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Stocks.Tests.Shared;

namespace Stocks.FunctionalTests;

public class Setup : IAsyncLifetime
{
    private string? _tableName;
    private AmazonDynamoDBClient? _dynamoDbClient;
    private AmazonCognitoIdentityProviderClient _cognitoIdentityProviderClient;
    
    private string? _userPoolId;
    private string _testUsername;

    public string ApiUrl { get; private set; } = default!;
    
    public string? AuthToken { get; private set; }

    public List<string> CreatedStockSymbols { get; } = new();
    
    public AsyncTestManager AsyncTestManager { get; private set; }

    public async Task InitializeAsync()
    {
        var stackPostfix = Environment.GetEnvironmentVariable("STACK_POSTFIX");
        
        var stackName = $"{(Environment.GetEnvironmentVariable("STACK_NAME") ?? "StockPriceStack")}{stackPostfix}";
        var authenticationStackName = $"{(Environment.GetEnvironmentVariable("STACK_NAME") ?? "AuthenticationStack")}{stackPostfix}";
        var testInfrastructureStackName = $"{(Environment.GetEnvironmentVariable("STACK_NAME") ?? "StockTestInfrastructure")}{stackPostfix}";
        
        var region = Environment.GetEnvironmentVariable("AWS_REGION_NAME") ?? "eu-west-1";
        var endpoint = RegionEndpoint.GetBySystemName(region);
        
        var chain = new CredentialProfileStoreChain();

        AmazonCloudFormationClient cloudFormationClient;
        
        AWSCredentials awsCredentials;
        
        if (chain.TryGetAWSCredentials("dev", out awsCredentials))
        {
            cloudFormationClient = new AmazonCloudFormationClient(awsCredentials, endpoint);
            _cognitoIdentityProviderClient = new AmazonCognitoIdentityProviderClient(awsCredentials, endpoint);
        }
        else
        {
            cloudFormationClient = new AmazonCloudFormationClient(endpoint);
            _cognitoIdentityProviderClient = new AmazonCognitoIdentityProviderClient();
        }
        
        var response = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest() { StackName = stackName });
        var authStackResponse = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest() { StackName = authenticationStackName });
        var testStackResponse = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest() { StackName = testInfrastructureStackName });
        
        var outputs = response.Stacks[0].Outputs;
        var authOutputs = authStackResponse.Stacks[0].Outputs;
        var testOutputs = testStackResponse.Stacks[0].Outputs;
        
        this._userPoolId = GetOutputVariable(authOutputs, $"UserPoolId{stackPostfix}");
        var clientId = GetOutputVariable(authOutputs, $"ClientId{stackPostfix}");

        this._testUsername = $"{Guid.NewGuid()}@example.com";

        var authToken = await CreateTestUser(clientId);
        _dynamoDbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig() { RegionEndpoint = endpoint });

        ApiUrl = GetOutputVariable(outputs, $"APIEndpointOutput{stackPostfix}");
        var asyncTestTable = GetOutputVariable(testOutputs, $"StockPriceTest{stackPostfix}");

        AsyncTestManager = new AsyncTestManager(_dynamoDbClient, asyncTestTable);
        
        AuthToken = authToken;
        _tableName = GetOutputVariable(outputs, $"TableNameOutput{stackPostfix}");
    }

    private async Task<string> CreateTestUser(string userPoolClientId)
    {
        await _cognitoIdentityProviderClient.AdminCreateUserAsync(new AdminCreateUserRequest()
        {
            UserPoolId = this._userPoolId,
            Username = this._testUsername,
            UserAttributes = new List<AttributeType>(2)
            {
                new()
                {
                    Name = "given_name",
                    Value = "John"
                },
                new()
                {
                    Name = "family_name",
                    Value = "Doe"
                }
            }
        });
    
        await _cognitoIdentityProviderClient.AdminSetUserPasswordAsync(
            new AdminSetUserPasswordRequest()
            {
                UserPoolId = this._userPoolId,
                Username = this._testUsername,
                Permanent = true,
                Password = Environment.GetEnvironmentVariable("TEMPORARY_PASSWORD")
            });
    
        var authOutput = await _cognitoIdentityProviderClient.AdminInitiateAuthAsync(
            new AdminInitiateAuthRequest()
            {
                UserPoolId = this._userPoolId,
                ClientId = userPoolClientId,
                AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
                AuthParameters = new Dictionary<string, string>(2)
                {
                    {"USERNAME", this._testUsername},
                    {"PASSWORD", Environment.GetEnvironmentVariable("TEMPORARY_PASSWORD")},
                }
            });

        return authOutput.AuthenticationResult.IdToken;
    }

    private async Task DisposeUserAsync()
    {
        await _cognitoIdentityProviderClient.AdminDeleteUserAsync(
            new AdminDeleteUserRequest()
            {
                UserPoolId = this._userPoolId,
                Username = this._testUsername
            });
    }

    public async Task DisposeAsync()
    {
        await this.DisposeUserAsync();
        
        foreach (var id in this.CreatedStockSymbols)
        {
            try
            {
                await _dynamoDbClient!.DeleteItemAsync(_tableName!, new() { ["StockSymbol"] = new(id) });
            }
            catch (Exception)
            {
                Console.WriteLine($"Failure deleting record {id}");
            }
        }
    }

    private static string GetOutputVariable(List<Output> outputs, string name) =>
        outputs.Find(o => o.OutputKey.StartsWith(name.Replace("-", "")))?.OutputValue
        ?? throw new Exception($"CloudFormation stack does not have an output variable named '{name}'");
}