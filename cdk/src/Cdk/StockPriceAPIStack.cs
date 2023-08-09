namespace Cdk;

using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SSM;

using Cdk.SharedConstructs;

using Constructs;

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
        var api = new CognitoAuthorizedApi(
            this,
            $"StockPriceApi{apiProps.Postfix}",
            new RestApiProps
            {
                RestApiName = $"StockPriceApi{apiProps.Postfix}"
            },
            apiProps.UserPool);

        var idempotencyTracker = this.CreatePersistenceLayer(apiProps.Postfix);

        var endpointProps = new EndpointProps(
            apiProps,
            api,
            this.Table,
            idempotencyTracker);

        var setStockPriceEndpoint = new SetStockPriceEndpoint(
            this,
            "SetStockPriceEndpoint",
            endpointProps);

        var getStockPriceEndpoint = new GetStockPriceEndpoint(
            this,
            "GetStockPriceEndpoint",
            endpointProps);

        var topicPublisher = new TableToSNSChannel(
            this,
            $"StockPriceUpdatedChannel{apiProps.Postfix}",
            this.Table,
            $"stock-price-updated{apiProps.Postfix}",
            "./cdk/src/Cdk/input-transformers/stock-price-updated-transformer.json");

        var tableNameOutput = new CfnOutput(
            this,
            $"TableNameOutput{apiProps.Postfix}",
            new CfnOutputProps
            {
                Value = this.Table.TableName,
                ExportName = $"TableName{apiProps.Postfix}",
                Description = "Name of the main DynamoDB table"
            });

        var apiEndpointOutput = new CfnOutput(
            this,
            $"APIEndpointOutput{apiProps.Postfix}",
            new CfnOutputProps
            {
                Value = api.Url,
                ExportName = $"ApiEndpoint{apiProps.Postfix}",
                Description = "Endpoint of the Stock price API"
            });
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
                TableName = $"StockPriceTable{postfix}",
                Stream = StreamViewType.NEW_AND_OLD_IMAGES,
            });

        return idempotencyTracker;
    }
}

public record EndpointProps(
    StockPriceStackProps StackProps,
    CognitoAuthorizedApi Api,
    ITable Table,
    ITable Idempotency);

public class GetStockPriceEndpoint : Construct
{
    public Function Function { get; }

    public GetStockPriceEndpoint(
        Construct scope,
        string id,
        EndpointProps props) : base(
        scope,
        id)
    {
        this.Function = new LambdaFunction(
            this,
            $"GetStockPriceEndpoint{props.StackProps.Postfix}",
            "src/StockTraderAPI/StockTrader.API",
            "StockTrader.API::StockTrader.API.Endpoints.GetStockPriceEndpoint_GetStockPrice_Generated::GetStockPrice",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", props.Table.TableName },
                { "IDEMPOTENCY_TABLE_NAME", props.Idempotency.TableName },
                { "ENV", props.StackProps.Postfix },
                { "POWERTOOLS_SERVICE_NAME", $"StockPriceApi{props.StackProps.Postfix}" },
                { "POWERTOOLS_METRICS_NAMESPACE", "StockPriceApi{props.StackProps.Postfix}" },
                { "CONFIGURATION_PARAM_NAME", props.StackProps.Parameter.ParameterName }
            }).Function;

        props.Table.GrantReadWriteData(this.Function);
        props.Idempotency.GrantReadWriteData(this.Function);
        props.StackProps.Parameter.GrantRead(this.Function);

        this.Function.Role.AttachInlinePolicy(
            new Policy(
                this,
                "DescribeEventBus",
                new PolicyProps
                {
                    Statements = new[]
                    {
                        new PolicyStatement(
                            new PolicyStatementProps
                            {
                                Actions = new[] { "ssm:GetParametersByPath" },
                                Resources = new[] { props.StackProps.Parameter.ParameterArn }
                            })
                    }
                }));

        props.Api.AddLambdaEndpoint(
            this.Function,
            "price",
            "GET");
    }
}

public class SetStockPriceEndpoint : Construct
{
    public Function Function { get; }

    public SetStockPriceEndpoint(
        Construct scope,
        string id,
        EndpointProps props) : base(
        scope,
        id)
    {
        this.Function = new LambdaFunction(
            this,
            $"SetStockPriceEndpoint{props.StackProps.Postfix}",
            "src/StockTraderAPI/StockTrader.API",
            "StockTrader.API::StockTrader.API.Endpoints.SetStockPriceEndpoint_SetStockPrice_Generated::SetStockPrice",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", props.Table.TableName },
                { "IDEMPOTENCY_TABLE_NAME", props.Idempotency.TableName },
                { "ENV", props.StackProps.Postfix },
                { "POWERTOOLS_SERVICE_NAME", $"StockPriceApi{props.StackProps.Postfix}" },
                { "POWERTOOLS_METRICS_NAMESPACE", "StockPriceApi{props.StackProps.Postfix}" },
                { "CONFIGURATION_PARAM_NAME", props.StackProps.Parameter.ParameterName }
            }).Function;

        props.Table.GrantReadWriteData(this.Function);
        props.Idempotency.GrantReadWriteData(this.Function);
        props.StackProps.Parameter.GrantRead(this.Function);

        this.Function.Role.AttachInlinePolicy(
            new Policy(
                this,
                "DescribeEventBus",
                new PolicyProps
                {
                    Statements = new[]
                    {
                        new PolicyStatement(
                            new PolicyStatementProps
                            {
                                Actions = new[] { "ssm:GetParametersByPath" },
                                Resources = new[] { props.StackProps.Parameter.ParameterArn }
                            })
                    }
                }));

        props.Api.AddLambdaEndpoint(
            this.Function,
            "price",
            "POST");
    }
}