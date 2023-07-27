using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SSM;
using Cdk.SharedConstructs;
using Constructs;

namespace Cdk;

public record StockPriceStackProps(
    StringParameter Parameter);

public class StockPriceAPIStack : Stack
{
    public ITable Table { get; private set; }
    
    internal StockPriceAPIStack(
        Construct scope,
        string id,
        StockPriceStackProps customProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var userPool = this.CreateUserPool();

        var api = new RestApi(
            this,
            "StockPriceApi",
            new RestApiProps());

        var idempotencyTracker = this.CreatePersistenceLayer();

        var eventBus = new EventBus(
            this,
            "StockSystemEventBus",
            new EventBusProps());
        
        var describeEventBusPolicy = new PolicyStatement(
            new PolicyStatementProps
            {
                Actions = new[] { "events:DescribeEventBus" },
                Resources = new[] { eventBus.EventBusArn }
            });

        var parameterReadPolicy = new PolicyStatement(
            new PolicyStatementProps
            {
                Actions = new[] { "ssm:GetParametersByPath" },
                Resources = new[] { customProps.Parameter.ParameterArn }
            });

        var setStockPriceFunction = this.CreateSetStockPriceEndpoint(customProps,
            Table,
            eventBus,
            idempotencyTracker,
            describeEventBusPolicy,
            parameterReadPolicy);

        var getStockPriceFunction = this.CreateGetStockPriceFunction(customProps,
            this.Table,
            eventBus,
            idempotencyTracker,
            parameterReadPolicy,
            describeEventBusPolicy);

        this.AddApiEndpoints(userPool,
            api,
            setStockPriceFunction,
            getStockPriceFunction);

        var tableNameOutput = new CfnOutput(
            this,
            "TableNameOutput",
            new CfnOutputProps
            {
                Value = this.Table.TableName,
                ExportName = "TableName",
                Description = "Name of the main DynamoDB table"
            });
    }

    private void AddApiEndpoints(
        UserPool userPool,
        RestApi api,
        LambdaFunction setStockPriceFunction,
        LambdaFunction getStockPriceFunction)
    {
        var userPoolAuthorizer = new CognitoUserPoolsAuthorizer(
            this,
            "CognitoAuthorizer",
            new CognitoUserPoolsAuthorizerProps
            {
                CognitoUserPools = new IUserPool[]
                {
                    userPool
                },
                AuthorizerName = "test_cognitoauthorizer",
                IdentitySource = "method.request.header.Authorization"
            });

        var priceResource = api.Root.AddResource("price");

        priceResource.AddMethod(
            "PUT",
            new LambdaIntegration(setStockPriceFunction.Function),
            new MethodOptions
            {
                AuthorizationType = AuthorizationType.COGNITO,
                Authorizer = userPoolAuthorizer
            });

        var getResource = priceResource.AddResource("{stockSymbol}");

        getResource.AddMethod(
            "GET",
            new LambdaIntegration(getStockPriceFunction.Function),
            new MethodOptions()
            {
                AuthorizationType = AuthorizationType.COGNITO,
                Authorizer = userPoolAuthorizer
            });
    }

    private LambdaFunction CreateGetStockPriceFunction(
        StockPriceStackProps customProps,
        ITable table,
        IEventBus eventBus,
        ITable idempotencyTracker,
        PolicyStatement parameterReadPolicy,
        PolicyStatement describeEventBusPolicy)
    {
        var getStockPriceFunction = new LambdaFunction(
            this,
            "GetStockPrice",
            "src/StockTrader.API",
            "StockTrader.API::StockTrader.API.Endpoints.GetStockPriceEndpoint_GetStockPrice_Generated::GetStockPrice",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", table.TableName },
                { "EVENT_BUS_NAME", eventBus.EventBusName },
                { "IDEMPOTENCY_TABLE_NAME", idempotencyTracker.TableName },
                { "ENV", "prod" },
                { "POWERTOOLS_SERVICE_NAME", "pricing" },
                { "POWERTOOLS_METRICS_NAMESPACE", "pricing" },
                { "CONFIGURATION_PARAM_NAME", customProps.Parameter.ParameterName }
            });

        table.GrantReadData(getStockPriceFunction.Function);
        idempotencyTracker.GrantReadWriteData(getStockPriceFunction.Function);
        customProps.Parameter.GrantRead(getStockPriceFunction.Function);

        getStockPriceFunction.Function.Role.AttachInlinePolicy(
            new Policy(
                this,
                "GetStockPriceGetParameters",
                new PolicyProps
                {
                    Statements = new[] { parameterReadPolicy, describeEventBusPolicy }
                }));
        
        return getStockPriceFunction;
    }

    private LambdaFunction CreateSetStockPriceEndpoint(
        StockPriceStackProps customProps,
        ITable table,
        IEventBus eventBus,
        ITable idempotencyTracker,
        PolicyStatement describeEventBusPolicy,
        PolicyStatement parameterReadPolicy)
    {
        var setStockPriceFunction = new LambdaFunction(
            this,
            "SetStockPrice",
            "src/StockTrader.API",
            "StockTrader.API::StockTrader.API.Endpoints.SetStockPriceEndpoint_SetStockPrice_Generated::SetStockPrice",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", table.TableName },
                { "EVENT_BUS_NAME", eventBus.EventBusName },
                { "IDEMPOTENCY_TABLE_NAME", idempotencyTracker.TableName },
                { "ENV", "prod" },
                { "POWERTOOLS_SERVICE_NAME", "pricing" },
                { "POWERTOOLS_METRICS_NAMESPACE", "pricing" },
                { "CONFIGURATION_PARAM_NAME", customProps.Parameter.ParameterName }
            });

        table.GrantReadWriteData(setStockPriceFunction.Function);
        idempotencyTracker.GrantReadWriteData(setStockPriceFunction.Function);
        eventBus.GrantPutEventsTo(setStockPriceFunction.Function);
        customProps.Parameter.GrantRead(setStockPriceFunction.Function);

        setStockPriceFunction.Function.Role.AttachInlinePolicy(
            new Policy(
                this,
                "DescribeEventBus",
                new PolicyProps
                {
                    Statements = new[] { describeEventBusPolicy, parameterReadPolicy }
                }));
        return setStockPriceFunction;
    }

    private Table CreatePersistenceLayer()
    {
        var idempotencyTracker = new Table(
            this,
            "Idempotency",
            new TableProps
            {
                BillingMode = BillingMode.PAY_PER_REQUEST,
                PartitionKey = new Attribute
                {
                    Name = "id",
                    Type = AttributeType.STRING
                },
                TimeToLiveAttribute = "expiration"
            });

        this.Table = new Table(
            this,
            "StockPriceTable",
            new TableProps
            {
                BillingMode = BillingMode.PAY_PER_REQUEST,
                PartitionKey = new Attribute
                {
                    Name = "StockSymbol",
                    Type = AttributeType.STRING
                }
            });
        
        return idempotencyTracker;
    }

    private UserPool CreateUserPool()
    {
        var userPool = new UserPool(
            this,
            "StockPriceUserPool",
            new UserPoolProps
            {
                UserPoolName = "stock-service-users",
                SelfSignUpEnabled = true,
                SignInAliases = new SignInAliases
                {
                    Email = true
                },
                AutoVerify = new AutoVerifiedAttrs
                {
                    Email = true
                },
                StandardAttributes = new StandardAttributes
                {
                    GivenName = new StandardAttribute
                    {
                        Required = true
                    },
                    FamilyName = new StandardAttribute
                    {
                        Required = true
                    }
                },
                PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 6,
                    RequireDigits = true,
                    RequireLowercase = true,
                    RequireSymbols = false,
                    RequireUppercase = false
                },
                AccountRecovery = AccountRecovery.EMAIL_ONLY,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

        var userPoolClient = new UserPoolClient(
            this,
            "StockPriceClient",
            new UserPoolClientProps()
            {
                UserPool = userPool,
                UserPoolClientName = "api-login",
                AuthFlows = new AuthFlow()
                {
                    AdminUserPassword = true,
                    Custom = true,
                    UserSrp = true
                },
                SupportedIdentityProviders = new[]
                {
                    UserPoolClientIdentityProvider.COGNITO,
                },
                ReadAttributes = new ClientAttributes().WithStandardAttributes(
                    new StandardAttributesMask()
                    {
                        GivenName = true,
                        FamilyName = true,
                        Email = true,
                        EmailVerified = true
                    }),
                WriteAttributes = new ClientAttributes().WithStandardAttributes(
                    new StandardAttributesMask()
                    {
                        GivenName = true,
                        FamilyName = true,
                        Email = true
                    })
            });

        var userPoolOutput = new CfnOutput(this, "UserPoolId", new CfnOutputProps()
        {
            Value = userPool.UserPoolId,
            ExportName = "UserPoolId"
        });

        var clientIdOutput = new CfnOutput(this, "ClientId", new CfnOutputProps()
        {
            Value = userPoolClient.UserPoolClientId,
            ExportName = "ClientId"
        });
        
        return userPool;
    }
}

