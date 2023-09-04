using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.AWS.StepFunctions;
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
        // Load values stored in SSM from other stacks, for the User Pool and the SNS topic
        var userPoolParameterValue =
            StringParameter.ValueForStringParameter(this, $"/authentication/{apiProps.Postfix}/user-pool-id");

        var userPool = UserPool.FromUserPoolArn(this, "UserPool", userPoolParameterValue);

        var stockPriceUpdatedTopic = GetStockPriceUpdatedTopic(apiProps);

        var stockPriceUpdatedQueue = new Queue(this, $"StockPriceUpdatedQueue{apiProps.Postfix}", new QueueProps());

        // Create a new subscription to the Stock Price Updated topic
        new PublishSubscribeChannel(this, $"StockPriceUpdatedSubscription{apiProps.Postfix}")
            .SubscribeTo(stockPriceUpdatedTopic)
            .Targeting(stockPriceUpdatedQueue)
            .Build();

        var stockNotificationTable = this.CreateStockNotificationTable(apiProps);

        var api = new NotificationApi(this, $"NotificationApi{apiProps.Postfix}",
            new NotificationApiProps(apiProps.Postfix, stockNotificationTable, userPool));

        var stockPriceNotificationWorkflow = new SqsSourcedWorkflow(this,
            $"StockPriceUpdateNotificationWorkflow{apiProps.Postfix}", WorkflowStep
                .QueryForStockNotificationRequests(this, stockNotificationTable)
                .Next(new Map(this, "IterateResults", new MapProps
                {
                    ItemsPath = "$.queryResults.Items"
                }).Iterator(WorkflowStep.SendNotification(this, stockNotificationTable))));

        stockNotificationTable.GrantReadData(stockPriceNotificationWorkflow.Workflow);

        new PointToPointChannel(this, $"PriceNotification{apiProps.Postfix}")
            .From(new SqsQueueSource(stockPriceUpdatedQueue))
            .WithMessageTranslation("FormatSQSObject", new Dictionary<string, object>(1)
            {
                { "Body.$", "States.StringToJson($.body)" }
            })
            .To(new WorkflowTarget(stockPriceNotificationWorkflow.Workflow));

        var apiEndpointOutput = new CfnOutput(this, "ApiOutput", new CfnOutputProps
        {
            ExportName = $"NotificationEndpoint{apiProps.Postfix}",
            Value = api.Api.Url
        });

        var workflowNameOutput = new CfnOutput(
            this,
            "StockUpdateWorkflowName",
            new CfnOutputProps()
            {
                ExportName = $"StockUpdateWorkflowName{apiProps.Postfix}",
                Value = stockPriceNotificationWorkflow.Workflow.StateMachineArn
            });

        var stockPriceUpdatedQueueOutput = new CfnOutput(
            this,
            "StockPriceUpdatedQueueOutput",
            new CfnOutputProps()
            {
                ExportName = $"StockUpdateQueue{apiProps.Postfix}",
                Value = stockPriceUpdatedQueue.QueueUrl
            });
    }

    private Table CreateStockNotificationTable(NotificationServiceStackProps apiProps)
    {
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

        stockNotificationTable.AddGlobalSecondaryIndex(
            new GlobalSecondaryIndexProps
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

        var tableNameOutput = new CfnOutput(
            this,
            "TableNameOutput",
            new CfnOutputProps()
            {
                ExportName = $"NotificationTable{apiProps.Postfix}",
                Value = stockNotificationTable.TableName
            });
        
        return stockNotificationTable;
    }

    private ITopic GetStockPriceUpdatedTopic(NotificationServiceStackProps apiProps)
    {
        if (apiProps.Postfix != "Dev" &&
            apiProps.Postfix != "Prod")
            // If not an integration environment return a topic created by this stack
            return new Topic(this, "StockPriceUpdatedTopic");

        var topicArn =
            StringParameter.ValueForStringParameter(this, $"/stocks/{apiProps.Postfix}/stock-price-updated-channel");

        var stockPriceUpdatedTopic = Topic.FromTopicArn(this, "StockPriceUpdatedTopic", topicArn);

        return stockPriceUpdatedTopic;
    }
}