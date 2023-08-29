using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace SharedConstructs;

public record StorageFirstApiProps(IQueue Queue, IUserPool UserPool);

public class StorageFirstApi : Construct
{
    private readonly Construct _scope;
    private readonly string _id;
    private IQueue _targetQueue;
    private IUserPool _userPool;
    
    public StorageFirstApi(
        Construct scope,
        string id) : base(
        scope,
        id)
    {
        _scope = scope;
        _id = id;
    }

    public StorageFirstApi WithAuth(IUserPool userPool)
    {
        this._userPool = userPool;

        return this;
    }

    public StorageFirstApi WithTarget(IQueue sqsQueue)
    {
        this._targetQueue = sqsQueue;

        return this;
    }

    public RestApi Build()
    {
     var integrationRole = new Role(
            _scope,
            $"{_id}GatewayIntegrationRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("apigateway.amazonaws.com")
            });

     if (this._targetQueue != null)
     {
         this._targetQueue.GrantSendMessages(integrationRole);   
     }

     var integration = new AwsIntegration(
            new AwsIntegrationProps
            {
                Service = "sqs",
                Path = $"{Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")}/{this._targetQueue.QueueName}",
                IntegrationHttpMethod = "POST",
                Options = new IntegrationOptions
                {
                    CredentialsRole = integrationRole,
                    RequestParameters = new Dictionary<string, string>(1)
                    {
                        { "integration.request.header.Content-Type", "'application/x-www-form-urlencoded'" }
                    },
                    RequestTemplates = new Dictionary<string, string>(1)
                    {
                        { "application/json", "Action=SendMessage&MessageBody=$input.body" }
                    },
                    IntegrationResponses = new List<IIntegrationResponse>(3)
                    {
                        new IntegrationResponse
                        {
                            StatusCode = "200"
                        },
                        new IntegrationResponse
                        {
                            StatusCode = "400"
                        },
                        new IntegrationResponse
                        {
                            StatusCode = "500"
                        },
                    }.ToArray()
                }
            });

        var frontendApi = new RestApi(
            _scope,
            $"{_id}Api",
            new RestApiProps());

        frontendApi.Root.AddMethod(
            "POST",
            integration,
            new MethodOptions
            {
                MethodResponses = new[]
                {
                    new MethodResponse { StatusCode = "200" },
                    new MethodResponse { StatusCode = "400" },
                    new MethodResponse { StatusCode = "500" }
                },
                AuthorizationType = _userPool != null ? AuthorizationType.COGNITO : null,
                Authorizer = _userPool != null ? new CognitoUserPoolsAuthorizer(this, "CognitoAuthorizer", new CognitoUserPoolsAuthorizerProps()
                {
                    CognitoUserPools = new IUserPool[]
                    {
                        this._userPool
                    },
                    AuthorizerName = "cognitoauthorizer",
                    IdentitySource = "method.request.header.Authorization"
                }) : null
            });

        return frontendApi;
    }
}