using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SQS;
using Constructs;

namespace SharedConstructs;

public record StorageFirstApiProps(IQueue Queue, IUserPool UserPool);

public class StorageFirstApi : Construct
{
    public RestApi Api { get; private set; }
    
    public StorageFirstApi(
        Construct scope,
        string id,
        StorageFirstApiProps props) : base(
        scope,
        id)
    {
        var integrationRole = new Role(
            this,
            "SqsApiGatewayIntegrationRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("apigateway.amazonaws.com")
            });

        props.Queue.GrantSendMessages(integrationRole);

        var integration = new AwsIntegration(
            new AwsIntegrationProps
            {
                Service = "sqs",
                Path = $"{Environment.GetEnvironmentVariable("CDK_DEFAULT_ACCOUNT")}/{props.Queue.QueueName}",
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
            this,
            "FrontendApi",
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
                AuthorizationType = AuthorizationType.COGNITO,
                Authorizer = new CognitoUserPoolsAuthorizer(this, "CognitoAuthorizer", new CognitoUserPoolsAuthorizerProps()
                {
                    CognitoUserPools = new IUserPool[]
                    {
                        props.UserPool
                    },
                    AuthorizerName = "cognitoauthorizer",
                    IdentitySource = "method.request.header.Authorization"
                })
            });

        this.Api = frontendApi;
    }
}