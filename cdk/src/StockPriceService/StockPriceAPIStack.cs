using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;
using Constructs;
using SharedConstructs;
using StockPriceService;

namespace Cdk.StockPriceApi;

public record StockPriceStackProps(
    string Postfix);

public class StockPriceApiStack : Stack
{
    private ITable _table;
    private ITable _idempotency;
    
    public ITopic StockUpdatedTopic { get; private set; }

    internal StockPriceApiStack(
        Construct scope,
        string id,
        StockPriceStackProps apiProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var parameter =
            StringParameter.FromStringParameterName(this, "ConfigurationParameter",
                $"/{apiProps.Postfix}/configuration");

        var userPoolParameterValue =
            StringParameter.ValueForStringParameter(this, $"/authentication/{apiProps.Postfix}/user-pool-id");

        var userPool = UserPool.FromUserPoolArn(this, "UserPool", userPoolParameterValue);
        
        this.CreatePersistenceLayer(apiProps.Postfix);

        var endpointProps = new SharedLambdaProps(
            apiProps,
            this._table,
            this._idempotency,
            parameter);

        var setStockPriceEndpoint = new SetStockPriceEndpoint(
            this,
            "SetStockPriceEndpoint",
            endpointProps);

        var queryApiEndpoints = new QueryApiEndpoints(
            this,
            "QueryApiEndpoints",
            endpointProps);
        
        var aspAotExample = new AotAspNetExample(this, "AotAspNetExample", endpointProps);

        var api = new AuthorizedApi(
                this,
                $"StockPriceApi{apiProps.Postfix}",
                new RestApiProps
                {
                    RestApiName = $"StockPriceApi{apiProps.Postfix}"
                })
            .WithCognito(userPool)
            .WithEndpoint("/{proxy+}", HttpMethod.ALL, aspAotExample.Function)
            .WithEndpoint(
                "/price/{stockSymbol}",
                HttpMethod.GET,
                queryApiEndpoints.Function)
            .WithEndpoint(
                "/history/{stockSymbol}",
                HttpMethod.GET,
                queryApiEndpoints.Function)
            .WithEndpoint(
                "/price",
                HttpMethod.POST,
                setStockPriceEndpoint.Function);

        this.StockUpdatedTopic = new Topic(
            this,
            $"StockPriceUpdatedTopic{apiProps.Postfix}");

        new PointToPointChannel(
                this,
                $"StockPriceUpdatedChannel{apiProps.Postfix}")
            .From(new DynamoDbSource(this._table))
            .WithMessageFilter(
                "EventNameFilter",
                new Dictionary<string, string[]>(2)
                {
                    {"eventName", new []{"MODIFY", "INSERT"}}
                })
            .WithMessageFilter(
                "StockRecordType",
                new Dictionary<string, string[]>(2)
                {
                    {"dynamodb.NewImage.Type.S", new []{"Stock"}},
                })
            .WithMessageTranslation(
                "GenerateEvent",
                new Dictionary<string, object>(2)
                {
                    {
                        "Metadata", new Dictionary<string, object>
                        {
                            { "EventType", "StockPriceUpdated" },
                            { "TraceParent", "$.dynamodb.NewImage.TraceIdentifier.S" },
                        }
                    },
                    {
                        "Data", new Dictionary<string, object>(2)
                        {
                            { "StockSymbol.$", "$.dynamodb.NewImage.StockSymbol.S" },
                            { "Price.$", "$.dynamodb.NewImage.Price.N" },
                        }
                    }
                })
            .To(new SnsTarget(this.StockUpdatedTopic));

        var stockPriceUpdatedTopicParameter = new StringParameter(
            this,
            $"StockPriceUpdatedChannelParameter{apiProps.Postfix}",
            new StringParameterProps()
            {
                ParameterName = $"/stocks/{apiProps.Postfix}/stock-price-updated-channel",
                StringValue = this.StockUpdatedTopic.TopicArn
            });

        var tableNameOutput = new CfnOutput(
            this,
            $"TableNameOutput{apiProps.Postfix}",
            new CfnOutputProps
            {
                Value = this._table.TableName,
                ExportName = $"TableNameOutput{apiProps.Postfix}",
                Description = "Table name for storing stocks"
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

    private void CreatePersistenceLayer(string postfix)
    {
        this._idempotency = new Table(
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

        this._table = new Table(
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
    }
}