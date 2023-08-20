using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.StepFunctions;
using Cdk.SharedConstructs;
using Constructs;
using SharedConstructs;

namespace NotificationService;

public record NotificationApiProps(string Postfix, ITable StockNotificationTable, IUserPool UserPool);

public class NotificationApi : Construct
{
    public RestApi Api { get; private set; }
    public NotificationApi(Construct scope, string id, NotificationApiProps props) : base(scope, id)
    {
        // Define the business workflow to integrate with the HTTP request, generate the case id
        // store and publish.
        // Abstract the complexities of each Workflow Step behind a method call of legibility
        var stepFunction = new StateMachine(this, "ApiStateMachine", new StateMachineProps
        {
            DefinitionBody = DefinitionBody.FromChainable(new Map(this, "LoopInputRecords", new MapProps
            {
                InputPath = JsonPath.EntirePayload
            }).Iterator(
                new Pass(this, "ParseSQSInput", new PassProps
                    {
                        Parameters = new Dictionary<string, object>(1)
                        {
                            { "parsed.$", "States.StringToJson($.body)" }
                        },
                        OutputPath = JsonPath.StringAt("$.parsed")
                    })
                    // Store the API data
                    .Next(WorkflowStep.StoreApiData(this, props.StockNotificationTable)))),
            StateMachineType = StateMachineType.EXPRESS,
            TracingEnabled = true,
            Logs = new LogOptions
            {
                Level = LogLevel.ALL,
                IncludeExecutionData = true,
                Destination = new LogGroup(
                    this,
                    $"{id}StockPriceNotification", new LogGroupProps
                    {
                        LogGroupName = $"/aws/vendedlogs/states/StockNotificationLogs{props.Postfix}",
                        Retention = RetentionDays.ONE_DAY,
                        RemovalPolicy = RemovalPolicy.DESTROY
                    })
            },
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        props.StockNotificationTable.GrantReadWriteData(stepFunction);

        var requestNotificationQueue = new Queue(this, $"RequestNotification{props.Postfix}");

        this.Api = new StorageFirstApi(this, $"StockNotificationApi{props.Postfix}",
            new StorageFirstApiProps(requestNotificationQueue, props.UserPool)).Api;

        new PointToPointChannel(this, $"StockNotificationWorkflowChannel{props.Postfix}")
            .From(new SqsQueueSource(requestNotificationQueue))
            .To(new WorkflowTarget(stepFunction));
    }
}