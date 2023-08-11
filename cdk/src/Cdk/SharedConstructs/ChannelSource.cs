namespace Cdk.SharedConstructs;

using Amazon.CDK.AWS.Pipes;

public abstract class ChannelSource
{
    public abstract string SourceArn { get; }
    
    public abstract CfnPipe.PipeSourceParametersProperty SourceParameters { get; }
}