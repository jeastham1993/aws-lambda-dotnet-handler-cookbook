using Amazon.CDK.AWS.StepFunctions;

namespace Cdk.SharedConstructs;

using Amazon.CDK.AWS.Pipes;
using Amazon.CDK.AWS.SNS;

public class WorkflowTarget : ChannelTarget
{
    public IStateMachine Workflow { get; }
    public WorkflowTarget(IStateMachine workflow)
    {
        this.Workflow = workflow;
        this.TargetArn = workflow.StateMachineArn;
        this.TargetParameters = new CfnPipe.PipeTargetParametersProperty()
        {
            StepFunctionStateMachineParameters = new CfnPipe.PipeTargetStateMachineParametersProperty()
            {
                InvocationType = "FIRE_AND_FORGET"
            }
        };
    }
    /// <inheritdoc />
    public override string TargetArn { get; }

    /// <inheritdoc />
    public override CfnPipe.PipeTargetParametersProperty TargetParameters { get; }
}