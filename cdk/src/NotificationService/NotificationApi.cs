using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SQS;
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
        var sqsWorkflow = new SqsSourcedWorkflow(this, $"ApiStateMachine{props.Postfix}",
            WorkflowStep.ParseSQSInput(this).Next(WorkflowStep.StoreApiData(this, props.StockNotificationTable)));

        props.StockNotificationTable.GrantReadWriteData(sqsWorkflow.Workflow);

        var requestNotificationQueue = new Queue(this, $"RequestNotification{props.Postfix}");

        this.Api = new StorageFirstApi(this, $"StockNotificationApi{props.Postfix}")
            .WithAuth(props.UserPool)
            .WithTarget(requestNotificationQueue)
            .Build();

        new PointToPointChannel(this, $"StockNotificationWorkflowChannel{props.Postfix}")
            .From(new SqsQueueSource(requestNotificationQueue))
            .To(new WorkflowTarget(sqsWorkflow.Workflow));
    }
}