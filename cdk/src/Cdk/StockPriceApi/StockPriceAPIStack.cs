namespace Cdk;

using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;

using Cdk.SharedConstructs;

using Constructs;

using Policy = Amazon.CDK.AWS.IAM.Policy;

public record StockPriceStackProps(
    string Postfix,
    StringParameter Parameter,
    UserPool UserPool);

public class StockPriceApiStack : Stack
{
    public ITable Table { get; private set; }

    internal StockPriceApiStack(
        Construct scope,
        string id,
        StockPriceStackProps apiProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var idempotencyTracker = this.CreatePersistenceLayer(apiProps.Postfix);
        
        var endpointProps = new SharedLambdaProps(
            apiProps,
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

        var stockHistoryFunction = new AddStockHistoryFunction(
            this,
            "StockHistoryHandler",
            endpointProps);
        
        var api = new AuthorizedApi(this, $"StockPriceApi{apiProps.Postfix}",
            new RestApiProps
            {
                RestApiName = $"StockPriceApi{apiProps.Postfix}"
            })
            .WithCognito(apiProps.UserPool)
            .WithEndpoint("/price", HttpMethod.GET, getStockPriceEndpoint.Function)
            .WithEndpoint("/price", HttpMethod.POST, setStockPriceEndpoint.Function);

        var pubSubChannel = new PublishSubscribeChannel(this, "StockPriceUpdatedChannel")
            .WithTopicName($"StockPriceUpdatedTopic{apiProps.Postfix}")
            .WithSubscriber(stockHistoryFunction.Function)
            .Build();

        var messageChannel = new PointToPointChannel(this, $"StockPriceUpdatedChannel{apiProps.Postfix}")
            .WithSource(this.Table)
            .WithInputTransformerFromFile("./cdk/src/Cdk/input-transformers/stock-price-updated-transformer.json")
            .WithTarget(pubSubChannel)
            .Build();

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
                TableName = $"StockPriceIdempotency{postfix}",
                RemovalPolicy = RemovalPolicy.DESTROY
            });

        this.Table = new Table(
            this,
            "StockPriceTable",
            new TableProps
            {
                BillingMode = BillingMode.PAY_PER_REQUEST,
                PartitionKey = new Attribute
                {
                    Name = "PK",
                    Type = AttributeType.STRING
                },
                SortKey = new Attribute
                {
                    Name = "SK",
                    Type = AttributeType.STRING
                },
                TableName = $"StockPriceTable{postfix}",
                Stream = StreamViewType.NEW_AND_OLD_IMAGES,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

        return idempotencyTracker;
    }
}

public record SharedLambdaProps(
    StockPriceStackProps StackProps,
    ITable Table,
    ITable Idempotency);

public class GetStockPriceEndpoint : Construct
{
    public Function Function { get; }

    public GetStockPriceEndpoint(
        Construct scope,
        string id,
        SharedLambdaProps props) : base(
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
    }
}

public class SetStockPriceEndpoint : Construct
{
    public Function Function { get; }

    public SetStockPriceEndpoint(
        Construct scope,
        string id,
        SharedLambdaProps props) : base(
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
    }
}

public class AddStockHistoryFunction : Construct
{
    public Function Function { get; }

    public AddStockHistoryFunction(
        Construct scope,
        string id,
        SharedLambdaProps props) : base(
        scope,
        id)
    {
        this.Function = new LambdaFunction(
            this,
            $"AddStockHistoryHandler{props.StackProps.Postfix}",
            "src/StockTraderAPI/StockTrader.HistoryManager",
            "StockTrader.HistoryManager::StockTrader.HistoryManager.AddStockHistoryFunction_UpdateHistory_Generated::UpdateHistory",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", props.Table.TableName },
                { "ENV", props.StackProps.Postfix },
                { "IDEMPOTENCY_TABLE_NAME", props.Idempotency.TableName },
                { "POWERTOOLS_SERVICE_NAME", $"StockPriceApi{props.StackProps.Postfix}" },
                { "CONFIGURATION_PARAM_NAME", props.StackProps.Parameter.ParameterName }
            }).Function;

        props.Table.GrantWriteData(this.Function);
        props.Idempotency.GrantReadWriteData(this.Function);
        props.StackProps.Parameter.GrantRead(this.Function);

        this.Function.Role.AttachInlinePolicy(
            new Policy(
                this,
                "CustomPolicy",
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
    }
}