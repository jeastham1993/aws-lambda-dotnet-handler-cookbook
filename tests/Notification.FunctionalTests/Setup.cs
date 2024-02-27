namespace Notification.FunctionalTests;

using Amazon;
using Amazon.CloudFormation;
using Amazon.CloudFormation.Model;
using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;
using Amazon.DynamoDBv2;
using Amazon.Runtime;
using Amazon.Runtime.CredentialManagement;
using Amazon.SQS;
using Amazon.StepFunctions;

public class Setup : IAsyncLifetime
{
    private AmazonCognitoIdentityProviderClient _cognitoIdentityProviderClient;
    
    private string? _userPoolId;
    private string _testUsername;

    public string ApiUrl { get; private set; } = default!;
    
    public string? AuthToken { get; private set; }
    
    public string? StockUpdateQueueUrl { get; private set; }
    
    public string? StockUpdateWorkflowArn { get; private set; }
    
    public string? TableName { get; private set; }

    public Dictionary<string, string> CreatedNotifications { get; } = new();
    
    public AmazonSQSClient SqsClient { get; private set; }
    
    public AmazonStepFunctionsClient StepFunctionsClient { get; private set; }
    
    public AmazonDynamoDBClient DynamoDbClient { get; private set; }

    public async Task InitializeAsync()
    {
        var stackPostfix = Environment.GetEnvironmentVariable("STACK_POSTFIX");
        
        var stackName = $"{(Environment.GetEnvironmentVariable("STACK_NAME") ?? "NotificationServiceStack")}{stackPostfix}";
        var authenticationStackName = $"{(Environment.GetEnvironmentVariable("STACK_NAME") ?? "AuthenticationStack")}{stackPostfix}";
        
        var region = Environment.GetEnvironmentVariable("AWS_REGION_NAME") ?? "eu-west-1";
        var endpoint = RegionEndpoint.GetBySystemName(region);
        
        var chain = new CredentialProfileStoreChain();

        AmazonCloudFormationClient cloudFormationClient;
        
        AWSCredentials awsCredentials;
        
        if (chain.TryGetAWSCredentials("dev", out awsCredentials) && Environment.GetEnvironmentVariable("USE_ENVIRONMENT_PROFILE") != "Y")
        {
            cloudFormationClient = new AmazonCloudFormationClient(awsCredentials, endpoint);
            this._cognitoIdentityProviderClient = new AmazonCognitoIdentityProviderClient(awsCredentials, endpoint);
            this.SqsClient = new AmazonSQSClient(awsCredentials, endpoint);
            this.StepFunctionsClient = new AmazonStepFunctionsClient(awsCredentials, endpoint);
        }
        else
        {
            cloudFormationClient = new AmazonCloudFormationClient(endpoint);
            this._cognitoIdentityProviderClient = new AmazonCognitoIdentityProviderClient(endpoint);
            this.SqsClient = new AmazonSQSClient(endpoint);
            this.StepFunctionsClient = new AmazonStepFunctionsClient(endpoint);
        }
        
        var response = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest() { StackName = stackName });
        var authStackResponse = await cloudFormationClient.DescribeStacksAsync(new DescribeStacksRequest() { StackName = authenticationStackName });

        var outputs = response.Stacks[0].Outputs;
        var authOutputs = authStackResponse.Stacks[0].Outputs;
        
        this._userPoolId = GetOutputVariableFromExportName(authOutputs, $"UserPoolId{stackPostfix}", authenticationStackName);
        var clientId = GetOutputVariableFromExportName(authOutputs, $"ClientId{stackPostfix}", authenticationStackName);

        this._testUsername = $"{Guid.NewGuid()}@example.com";

        var authToken = await this.CreateTestUser(clientId);
        this.DynamoDbClient = new AmazonDynamoDBClient(new AmazonDynamoDBConfig() { RegionEndpoint = endpoint });

        this.ApiUrl = GetOutputVariableFromExportName(outputs, $"NotificationEndpoint{stackPostfix}", stackName);
        this.StockUpdateQueueUrl = GetOutputVariableFromExportName(outputs, $"StockUpdateQueue{stackPostfix}", stackName);
        this.StockUpdateWorkflowArn = GetOutputVariableFromExportName(outputs, $"StockUpdateWorkflowName{stackPostfix}", stackName);
        
        this.AuthToken = authToken;
        this.TableName = GetOutputVariableFromExportName(outputs, $"NotificationTable{stackPostfix}", stackName);
    }

    private async Task<string> CreateTestUser(string userPoolClientId)
    {
        await this._cognitoIdentityProviderClient.AdminCreateUserAsync(new AdminCreateUserRequest()
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
    
        await this._cognitoIdentityProviderClient.AdminSetUserPasswordAsync(
            new AdminSetUserPasswordRequest()
            {
                UserPoolId = this._userPoolId,
                Username = this._testUsername,
                Permanent = true,
                Password = Environment.GetEnvironmentVariable("TEMPORARY_PASSWORD")
            });
    
        var authOutput = await this._cognitoIdentityProviderClient.AdminInitiateAuthAsync(
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
        await this._cognitoIdentityProviderClient.AdminDeleteUserAsync(
            new AdminDeleteUserRequest()
            {
                UserPoolId = this._userPoolId,
                Username = this._testUsername
            });
    }

    public async Task DisposeAsync()
    {
        await this.DisposeUserAsync();
        
        foreach (var id in this.CreatedNotifications)
        {
            try
            {
                await this.DynamoDbClient!.DeleteItemAsync(this.TableName!, new() { { "PK", new(id.Key) }, {"SK", new(id.Value)}  });
            }
            catch (Exception)
            {
                Console.WriteLine($"Failure deleting record {id}");
            }
        }
    }

    private static string GetOutputVariableFromExportName(List<Output> outputs, string name, string stackName) =>
        outputs.Find(o => o.ExportName == name)?.OutputValue ?? throw new Exception($"CloudFormation stack {stackName} does not have an output variable named '{name}'");
}