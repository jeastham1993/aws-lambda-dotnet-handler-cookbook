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
    private AmazonCognitoIdentityProviderClient _cognitoIdentityProviderClient;
    
    private string? _userPoolId;
    private string _testUsername;

    public string ApiUrl { get; private set; } = default!;
    
    public string? AuthToken { get; private set; }

    public List<string> CreatedStockSymbols { get; } = new();

    public async Task InitializeAsync()
    {
        var stackPostfix = Environment.GetEnvironmentVariable("STACK_POSTFIX");
        
        var stackName = $"{(Environment.GetEnvironmentVariable("STACK_NAME") ?? "StockPriceStack")}{stackPostfix}";
        var authenticationStackName = $"{(Environment.GetEnvironmentVariable("STACK_NAME") ?? "AuthenticationStack")}{stackPostfix}";
        
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
        var outputs = response.Stacks[0].Outputs;
        var authOutputs = authStackResponse.Stacks[0].Outputs;
        
        this._userPoolId = GetOutputVariable(authOutputs, $"UserPoolId{stackPostfix}");
        var clientId = GetOutputVariable(authOutputs, $"ClientId{stackPostfix}");

        this._testUsername = $"{Guid.NewGuid()}@example.com";

        var authToken = await CreateTestUser(clientId);

        ApiUrl = GetOutputVariable(outputs, $"APIEndpointOutput{stackPostfix}");
        AuthToken = authToken;
        _tableName = GetOutputVariable(outputs, $"TableNameOutput{stackPostfix}");
        _dynamoDbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig() { RegionEndpoint = endpoint });
    }

    private async Task<string> CreateTestUser(string userPoolClientId)
    {
        var createdUser = await _cognitoIdentityProviderClient.AdminCreateUserAsync(new AdminCreateUserRequest()
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
    
        var setUserPassword = await _cognitoIdentityProviderClient.AdminSetUserPasswordAsync(
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
            catch
            {
            }
        }
    }

    private static string GetOutputVariable(List<Output> outputs, string name) =>
        outputs.FirstOrDefault(o => o.OutputKey.StartsWith(name))?.OutputValue
        ?? throw new Exception($"CloudFormation stack does not have an output variable named '{name}'");
}