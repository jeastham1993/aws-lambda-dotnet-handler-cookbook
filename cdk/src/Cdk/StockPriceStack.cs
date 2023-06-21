namespace Cdk;

using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SSM;

using Constructs;

using XaasKit.CDK.AWS.Lambda.DotNet;

public record StockPriceStackProps(
    StringParameter Parameter);

public class StockPriceStack : Stack
{
    internal StockPriceStack(
        Construct scope,
        string id,
        StockPriceStackProps customProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var api = new RestApi(
            this,
            "StockPriceApi",
            new RestApiProps());

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

        var table = new Table(
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
            });

        var eventBus = new EventBus(
            this,
            "StockSystemEventBus",
            new EventBusProps());

        var setStockPriceFunction = new DotNetFunction(
            this,
            "SetStockPrice",
            new DotNetFunctionProps
            {
                Runtime = Runtime.DOTNET_6,
                MemorySize = 1024,
                LogRetention = RetentionDays.ONE_DAY,
                Handler =
                    "SetStockPriceFunction::SetStockPriceFunction.Function_FunctionHandler_Generated::FunctionHandler",
                ProjectDir = "../src/SetStockPriceFunction",
                Environment = new Dictionary<string, string>(1)
                {
                    { "TABLE_NAME", table.TableName },
                    { "IDEMPOTENCY_TABLE_NAME", idempotencyTracker.TableName },
                    { "EVENT_BUS_NAME", eventBus.EventBusName },
                    { "ENV", "prod" },
                    { "POWERTOOLS_SERVICE_NAME", "pricing" },
                    { "POWERTOOLS_METRICS_NAMESPACE", "pricing"},
                    { "CONFIGURATION_PARAM_NAME", customProps.Parameter.ParameterName },
                },
                Tracing = Tracing.ACTIVE,
            });

        table.GrantReadWriteData(setStockPriceFunction);
        idempotencyTracker.GrantReadWriteData(setStockPriceFunction);
        eventBus.GrantPutEventsTo(setStockPriceFunction);
        customProps.Parameter.GrantRead(setStockPriceFunction);

        var describeEventBusPolicy = new PolicyStatement(
            new PolicyStatementProps()
            {
                Actions = new[] { "events:DescribeEventBus" },
                Resources = new[] { eventBus.EventBusArn }
            });

        var appConfigPolicy = new PolicyStatement(
            new PolicyStatementProps()
            {
                Actions = new[] { "ssm:GetParametersByPath" },
                Resources = new[] { customProps.Parameter.ParameterArn }
            });
        
        setStockPriceFunction.Role.AttachInlinePolicy(new Policy(this, "DescribeEventBus", new PolicyProps()
        {
            Statements = new []{describeEventBusPolicy, appConfigPolicy}
        }));

        var priceResource = api.Root.AddResource("price");

        priceResource.AddMethod(
            "PUT",
            new LambdaIntegration(setStockPriceFunction));
    }
}