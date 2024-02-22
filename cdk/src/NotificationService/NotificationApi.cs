using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SQS;
using Constructs;
using SharedConstructs;

namespace NotificationService;

public record NotificationApiProps(string Postfix, ITable StockNotificationTable, IUserPool UserPool);

public class NotificationApi : Construct
{
    public RestApi Api { get; private set; }
    
    public NotificationApi(Construct scope, string id, NotificationApiProps props) : base(scope, id)
    {
        var sqsWorkflow = new SqsSourcedWorkflow(this, $"ApiStateMachine{props.Postfix}",
            WorkflowStep.ParseSQSInput(this)
                .Next(WorkflowStep.StoreApiData(this, props.StockNotificationTable))
            );

        props.StockNotificationTable.GrantReadWriteData(sqsWorkflow.Workflow);

        var requestNotificationQueue = new Queue(this, $"RequestNotification{props.Postfix}");

        this.Api = new StorageFirstApi(
            this,
            $"StockNotificationApi{props.Postfix}", new StorageFirstApiProps(requestNotificationQueue, props.UserPool)).Api;

        new PointToPointChannel(this, $"StockNotificationWorkflowChannel{props.Postfix}")
            .From(new SqsQueueSource(requestNotificationQueue))
            .To(new WorkflowTarget(sqsWorkflow.Workflow));
    }
}