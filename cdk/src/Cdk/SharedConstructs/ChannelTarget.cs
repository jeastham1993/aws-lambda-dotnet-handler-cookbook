namespace Cdk.SharedConstructs;

using Amazon.CDK.AWS.Pipes;

public abstract class ChannelTarget
{
    public abstract string TargetArn { get; }
    
    public abstract CfnPipe.PipeTargetParametersProperty TargetParameters { get; }
}