using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.SSM;
using Amazon.CDK.AWS.StepFunctions;
using Cdk.SharedConstructs;
using Constructs;

namespace NotificationService;

public record NotificationServiceStackProps(
    string Postfix,
    ITopic StockPriceUpdatedTopic);

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
        var parameter =
            StringParameter.FromStringParameterName(this, "ConfigurationParameter",
                $"/{apiProps.Postfix}/configuration");

        var userPoolParameterValue =
            StringParameter.ValueForStringParameter(this, $"/authentication/{apiProps.Postfix}/user-pool-id");
        
        var userPoolClientParameterValue =
            StringParameter.ValueForStringParameter(this, $"/authentication/{apiProps.Postfix}/user-pool-client-id");

        var userPool = UserPool.FromUserPoolArn(this, "UserPool", userPoolParameterValue);

        var stockPriceUpdatedQueue = new Queue(this, "StockPriceUpdatedQueue", new QueueProps());

        new PublishSubscribeChannel(this, "StockPriceUpdatedSubscription")
            .SubscribeTo(apiProps.StockPriceUpdatedTopic)
            .Targeting(stockPriceUpdatedQueue)
            .Build();

        var stockPriceUpdatedWorkflow =
            new StateMachine(this, "StockPriceUpdateNotificationWorkflow", new StateMachineProps
            {
                StateMachineType = StateMachineType.EXPRESS,
                TracingEnabled = true,
                DefinitionBody = DefinitionBody.FromChainable(Chain.Start(new Pass(this,
                    "DummyWorkflow"))),
                Logs = new LogOptions
                {
                    Level = LogLevel.ALL,
                    IncludeExecutionData = true,
                    Destination = new LogGroup(
                        this,
                        $"{id}StockPriceUpdateLogs")
                },
                RemovalPolicy = RemovalPolicy.DESTROY,
            });

        new PointToPointChannel(this, "StockPriceUpdateChannel")
            .From(new SqsQueueSource(stockPriceUpdatedQueue))
            .To(new WorkflowTarget(stockPriceUpdatedWorkflow));
    }
}