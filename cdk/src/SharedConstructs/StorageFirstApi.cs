namespace SharedConstructs;

using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SQS;

using Constructs;

public record StorageFirstApiProps(IQueue Queue, IUserPool UserPool);

public class StorageFirstApi : Construct
{
    public RestApi Api { get; }

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
                    PassthroughBehavior = PassthroughBehavior.NEVER,
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
                            StatusCode = "200",
                            ResponseTemplates = new Dictionary<string, string>(1)
                            {
                                {
                                    "application/json", @"  #set($inputRoot = $input.path('$'))
    #set($sndMsgResp = $inputRoot.SendMessageResponse)
    #set($metadata = $sndMsgResp.ResponseMetadata)
    #set($sndMsgRes = $sndMsgResp.SendMessageResult)
    {
        ""RequestId"" : ""$metadata.RequestId"",
        ""MessageId"" : ""$sndMsgRes.MessageId""
    }"
                                }
                            }
                        }
                    }.ToArray()
                }
            });

        var frontendApi = new RestApi(
            this,
            $"{id}Api",
            new RestApiProps());

        frontendApi.Root.AddMethod(
            "POST",
            integration,
            new MethodOptions
            {
                MethodResponses = new[]
                {
                    new MethodResponse { StatusCode = "200" }
                },
                AuthorizationType = AuthorizationType.COGNITO,
                Authorizer = new CognitoUserPoolsAuthorizer(
                    this,
                    "CognitoAuth",
                    new CognitoUserPoolsAuthorizerProps
                    {
                        CognitoUserPools = new[] { props.UserPool }
                    })
            });

        this.Api = frontendApi;
    }
}