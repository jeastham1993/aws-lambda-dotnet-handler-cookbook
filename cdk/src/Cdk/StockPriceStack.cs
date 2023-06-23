using Cdk.SharedConstructs;

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

        var setStockPriceFunction = new LambdaFunction(this, "GetStockPrice", "./src/SetStockPriceFunction",
            "SetStockPriceFunction::SetStockPriceFunction.Function_FunctionHandler_Generated::FunctionHandler",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", table.TableName },
                { "EVENT_BUS_NAME", eventBus.EventBusName },
                { "IDEMPOTENCY_TABLE_NAME", idempotencyTracker.TableName },
                { "ENV", "prod" },
                { "POWERTOOLS_SERVICE_NAME", "pricing" },
                { "POWERTOOLS_METRICS_NAMESPACE", "pricing" },
                { "CONFIGURATION_PARAM_NAME", customProps.Parameter.ParameterName },
            });

        table.GrantReadWriteData(setStockPriceFunction.Function);
        idempotencyTracker.GrantReadWriteData(setStockPriceFunction.Function);
        eventBus.GrantPutEventsTo(setStockPriceFunction.Function);
        customProps.Parameter.GrantRead(setStockPriceFunction.Function);

        var describeEventBusPolicy = new PolicyStatement(
            new PolicyStatementProps()
            {
                Actions = new[] { "events:DescribeEventBus" },
                Resources = new[] { eventBus.EventBusArn }
            });

        var parameterReadPolicy = new PolicyStatement(
            new PolicyStatementProps()
            {
                Actions = new[] { "ssm:GetParametersByPath" },
                Resources = new[] { customProps.Parameter.ParameterArn }
            });
        
        setStockPriceFunction.Function.Role.AttachInlinePolicy(new Policy(this, "DescribeEventBus", new PolicyProps()
        {
            Statements = new []{describeEventBusPolicy, parameterReadPolicy}
        }));

        var getStockPriceFunction = new LambdaFunction(this, "GetStockPrice", "./src/GetStockPriceFunction",
            "GetStockPriceFunction::GetStockPriceFunction.Function_FunctionHandler_Generated::FunctionHandler",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", table.TableName },
                { "EVENT_BUS_NAME", eventBus.EventBusName },
                { "IDEMPOTENCY_TABLE_NAME", idempotencyTracker.TableName },
                { "ENV", "prod" },
                { "POWERTOOLS_SERVICE_NAME", "pricing" },
                { "POWERTOOLS_METRICS_NAMESPACE", "pricing" },
                { "CONFIGURATION_PARAM_NAME", customProps.Parameter.ParameterName },
            });

        table.GrantReadData(getStockPriceFunction.Function);
        idempotencyTracker.GrantReadWriteData(getStockPriceFunction.Function);
        customProps.Parameter.GrantRead(getStockPriceFunction.Function);
        
        getStockPriceFunction.Function.Role.AttachInlinePolicy(new Policy(this, "GetStockPriceGetParameters", new PolicyProps()
        {
            Statements = new []{parameterReadPolicy, describeEventBusPolicy}
        }));

        var priceResource = api.Root.AddResource("price");

        priceResource.AddMethod(
            "PUT",
            new LambdaIntegration(setStockPriceFunction.Function));

        var getResource = priceResource.AddResource("{stockSymbol}");

        getResource.AddMethod("GET", new LambdaIntegration(getStockPriceFunction.Function));

        var tableNameOutput = new CfnOutput(
            this,
            "TableNameOutput",
            new CfnOutputProps()
            {
                Value = table.TableName,
                ExportName = "TableName",
                Description = "Name of the main DynamoDB table"
            });
    }
}