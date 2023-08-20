using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Cdk.SharedConstructs;
using Constructs;
using SharedConstructs;

namespace NotificationService;

public record NotificationServiceStackProps(
    string Postfix);

public class NotificationServiceStack : Stack
{
    internal NotificationServiceStack(
        Construct scope,
        string id,
        NotificationServiceStackProps apiProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var userPoolParameterValue =
            StringParameter.ValueForStringParameter(this, $"/authentication/{apiProps.Postfix}/user-pool-id");

        var userPool = UserPool.FromUserPoolArn(this, "UserPool", userPoolParameterValue);
        
        var stockPriceUpdatedTopic = GetStockPriceUpdatedTopic(apiProps);

        var stockPriceUpdatedQueue = new Queue(this, $"StockPriceUpdatedQueue{apiProps.Postfix}", new QueueProps());

        new PublishSubscribeChannel(this, $"StockPriceUpdatedSubscription{apiProps.Postfix}")
            .SubscribeTo(stockPriceUpdatedTopic)
            .Targeting(stockPriceUpdatedQueue)
            .Build();

        var stockNotificationTable = new Table(
            this,
            "StockNotificationTable",
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
                TableName = $"StockNotification-{apiProps.Postfix}",
                Stream = StreamViewType.NEW_AND_OLD_IMAGES,
                RemovalPolicy = RemovalPolicy.DESTROY
            });
        
        stockNotificationTable.AddGlobalSecondaryIndex(new GlobalSecondaryIndexProps
        {
            IndexName = "GSI1",
            ProjectionType = ProjectionType.ALL,
            PartitionKey = new Attribute
            {
                Name = "GSI1PK",
                Type = AttributeType.STRING
            },
            SortKey = new Attribute
            {
                Name = "GSI1SK",
                Type = AttributeType.STRING
            }
        });

        var api = new NotificationApi(this, $"NotificationApi{apiProps.Postfix}",
            new NotificationApiProps(apiProps.Postfix, stockNotificationTable, userPool));

        var stockPriceUpdatedWorkflow =
            new StateMachine(this, $"StockPriceUpdateNotificationWorkflow{apiProps.Postfix}", new StateMachineProps
            {
                StateMachineType = StateMachineType.STANDARD,
                TracingEnabled = true,
                DefinitionBody = DefinitionBody.FromChainable(Chain.Start(new CallAwsService(this,
                    "QueryStockItems", new CallAwsServiceProps
                    {
                        Action = "query",
                        IamResources = new string[]
                        {
                            stockNotificationTable.TableArn
                        },
                        Service = "dynamodb",
                        Parameters = new Dictionary<string, object>()
                        {
                            { "TableName", stockNotificationTable.TableName },
                            { "KeyConditionExpression", "PK = :pk"},
                            {
                                "ExpressionAttributeValues", new Dictionary<string, object>()
                                {
                                    {":pk.$", JsonPath.StringAt("$.body.Data.StockSymbol")}
                                }
                            }
                        }
                    }))),
                Logs = new LogOptions
                {
                    Level = LogLevel.ALL,
                    IncludeExecutionData = true,
                    Destination = new LogGroup(
                        this,
                        $"{id}StockPriceUpdateLogs", new LogGroupProps
                        {
                            LogGroupName = $"/aws/vendedlogs/states/StockPriceUpdateLogs{apiProps.Postfix}"
                        })
                },
                RemovalPolicy = RemovalPolicy.DESTROY
            });

        new PointToPointChannel(this, $"StockPriceUpdateChannel{apiProps.Postfix}")
            .From(new SqsQueueSource(stockPriceUpdatedQueue))
            .WithMessageTranslation("FormatSQSObject", new Dictionary<string, object>(1)
            {
                { "Body.$", "States.StringToJson($.body)" }
            })
            .To(new WorkflowTarget(stockPriceUpdatedWorkflow));

        var apiEndpointOutput = new CfnOutput(this, "ApiOutput", new CfnOutputProps()
        {
            ExportName = $"NotificationApiEndpoint{apiProps.Postfix}",
            Value = api.Api.Url
        });
    }

    private ITopic GetStockPriceUpdatedTopic(NotificationServiceStackProps apiProps)
    {
        if (System.Environment.GetEnvironmentVariable("STACK_POSTFIX") != "Dev" &&
            System.Environment.GetEnvironmentVariable("STACK_POSTFIX") != "Prod")
            // If not an integration environment return a topic created by this stack
            return new Topic(this, "StockPriceUpdatedTopic");

        var topicArn =
            StringParameter.ValueForStringParameter(this, $"/stocks/{apiProps.Postfix}/stock-price-updated-channel");

        var stockPriceUpdatedTopic = Topic.FromTopicArn(this, "StockPriceUpdatedTopic", topicArn);

        return stockPriceUpdatedTopic;
    }
}