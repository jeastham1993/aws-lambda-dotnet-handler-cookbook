namespace Cdk;

using System.Collections.Generic;

using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;

using Cdk.SharedConstructs;

using Constructs;

public record StockPriceStackProps(
    string Postfix,
    StringParameter Parameter,
    UserPool UserPool);

public class StockPriceApiStack : Stack
{
    private ITable _table;
    private ITable _idempotency;

    internal StockPriceApiStack(
        Construct scope,
        string id,
        StockPriceStackProps apiProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        this.CreatePersistenceLayer(apiProps.Postfix);

        var endpointProps = new SharedLambdaProps(
            apiProps,
            this._table,
            this._idempotency);

        var setStockPriceEndpoint = new SetStockPriceEndpoint(
            this,
            "SetStockPriceEndpoint",
            endpointProps);

        var getStockPriceEndpoint = new GetStockPriceEndpoint(
            this,
            "GetStockPriceEndpoint",
            endpointProps);

        var getStockHistoryEndpoint = new GetStockHistoryEndpoint(
            this,
            "GetStockHistoryEndpoint",
            endpointProps);

        var api = new AuthorizedApi(
                this,
                $"StockPriceApi{apiProps.Postfix}",
                new RestApiProps
                {
                    RestApiName = $"StockPriceApi{apiProps.Postfix}"
                })
            .WithCognito(apiProps.UserPool)
            .WithEndpoint(
                "/price/{stockSymbol}",
                HttpMethod.GET,
                getStockPriceEndpoint.Function)
            .WithEndpoint(
                "/history/{stockSymbol}",
                HttpMethod.GET,
                getStockHistoryEndpoint.Function)
            .WithEndpoint(
                "/price",
                HttpMethod.POST,
                setStockPriceEndpoint.Function);

        var topic = new Topic(
            this,
            $"StockPriceUpdatedTopic{apiProps.Postfix}");

        topic.AddToResourcePolicy(
            new PolicyStatement(
                new PolicyStatementProps()
                {
                    Principals = new[] { new AccountPrincipal(this.Account) },
                    Actions = new[] { "sns:Subscribe" },
                    Resources = new[] { topic.TopicArn }
                }));

        new PointToPointChannel(
                this,
                $"StockPriceUpdatedChannel{apiProps.Postfix}")
            .From(new DynamoDbSource(this._table))
            .WithMessageFilter(
                "eventName",
                "INSERT")
            .WithMessageFilter(
                "dynamodb.NewImage.Type.S",
                "Stock")
            .WithMessageTranslation(
                "GenerateEvent",
                new Dictionary<string, object>(2)
                {
                    {
                        "Metadata", new Dictionary<string, object>
                        {
                            { "EventType", "StockPriceUpdated" }
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
            .To(new SnsTarget(topic));

        var stockPriceUpdatedTopicParameter = new StringParameter(
            this,
            $"StockPriceUpdatedChannelParameter{apiProps.Postfix}",
            new StringParameterProps()
            {
                ParameterName = $"/stocks/{apiProps.Postfix}/stock-price-updated-channel",
                StringValue = topic.TopicArn
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