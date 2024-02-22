using Amazon.CDK.AWS.Pipes;

namespace SharedConstructs;

public abstract class ChannelTarget
{
    public abstract string TargetArn { get; }
    
    public abstract CfnPipe.PipeTargetParametersProperty TargetParameters { get; }
}