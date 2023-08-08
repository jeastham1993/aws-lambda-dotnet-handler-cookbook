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

using Cdk.Extensions;

public record StockPriceStackProps(
    string Postfix,
    StringParameter Parameter,
    UserPool UserPool);

public class StockPriceAPIStack : Stack
{
    public ITable Table { get; private set; }
    
    internal StockPriceAPIStack(
        Construct scope,
        string id,
        StockPriceStackProps apiProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var api = new RestApi(
            this,
            $"StockPriceApi{apiProps.Postfix}",
            new RestApiProps()
            {
                RestApiName = $"StockPriceApi{apiProps.Postfix}"
            });
        
        var authorizer = new CognitoUserPoolsAuthorizer(
            this,
            "CognitoAuthorizer",
            new CognitoUserPoolsAuthorizerProps
            {
                CognitoUserPools = new IUserPool[]
                {
                    apiProps.UserPool
                },
                AuthorizerName = "test_cognitoauthorizer",
                IdentitySource = "method.request.header.Authorization"
            });

        var idempotencyTracker = this.CreatePersistenceLayer(apiProps.Postfix);

        var eventBus = new EventBus(
            this,
            "StockSystemEventBus",
            new EventBusProps()
            {
                EventBusName = $"StockSystemEventBus{apiProps.Postfix}"
            });
        
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
                Resources = new[] { apiProps.Parameter.ParameterArn }
            });

        var setStockPriceFunction = this.CreateSetStockPriceEndpoint(apiProps,
            Table,
            eventBus,
            idempotencyTracker,
            describeEventBusPolicy,
            parameterReadPolicy);

        var getStockPriceFunction = this.CreateGetStockPriceFunction(apiProps,
            this.Table,
            eventBus,
            idempotencyTracker,
            parameterReadPolicy,
            describeEventBusPolicy);

        api.AddLambdaEndpoint(
            setStockPriceFunction.Function,
            authorizer,
            "price", "POST");
        
        api.AddLambdaEndpoint(
            getStockPriceFunction.Function,
            authorizer,
            "price/{stockSymbol}", "GET");

        var tableNameOutput = new CfnOutput(
            this,
            $"TableNameOutput{apiProps.Postfix}",
            new CfnOutputProps
            {
                Value = this.Table.TableName,
                ExportName = $"TableName{apiProps.Postfix}",
                Description = "Name of the main DynamoDB table"
            });

        var apiEndpointOutput = new CfnOutput(this, $"APIEndpointOutput{apiProps.Postfix}", new CfnOutputProps()
        {
            Value = api.Url,
            ExportName = $"ApiEndpoint{apiProps.Postfix}",
            Description = "Endpoint of the Stock price API"
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
            $"GetStockPrice{customProps.Postfix}",
            "src/StockTraderAPI/StockTrader.API",
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
            $"SetStockPrice{customProps.Postfix}",
            "src/StockTraderAPI/StockTrader.API",
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

    private Table CreatePersistenceLayer(string postfix)
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
                TimeToLiveAttribute = "expiration",
                TableName = $"StockPriceIdempotency{postfix}"
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
                },
                TableName = $"StockPriceTable{postfix}"
            });
        
        return idempotencyTracker;
    }
}

