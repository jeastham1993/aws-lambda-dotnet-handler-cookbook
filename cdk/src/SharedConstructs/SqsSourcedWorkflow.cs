using Amazon.CDK;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.StepFunctions;
using Constructs;

namespace SharedConstructs;

public class SqsSourcedWorkflow : Construct
{
    public IStateMachine Workflow { get; set; }
    
    public SqsSourcedWorkflow(Construct scope, string id, Chain iteratorChain) : base(scope, id)
    {
        Workflow = new StateMachine(this, id, new StateMachineProps
        {
            DefinitionBody = DefinitionBody.FromChainable(new Map(this, "LoopInputRecords", new MapProps
            {
                InputPath = JsonPath.EntirePayload
            }).Iterator(iteratorChain)),
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
                        LogGroupName = $"/aws/vendedlogs/states/{id}",
                        Retention = RetentionDays.ONE_DAY,
                        RemovalPolicy = RemovalPolicy.DESTROY
                    })
            },
            RemovalPolicy = RemovalPolicy.DESTROY
        });
    }
}