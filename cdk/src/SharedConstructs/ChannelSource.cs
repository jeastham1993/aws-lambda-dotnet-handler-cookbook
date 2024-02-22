using Amazon.CDK.AWS.Pipes;

namespace SharedConstructs;

public abstract class ChannelSource
{
    public abstract string SourceArn { get; }
    
    public abstract CfnPipe.PipeSourceParametersProperty SourceParameters { get; }
}