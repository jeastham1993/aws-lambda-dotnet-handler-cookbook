using Amazon.CDK.AWS.Pipes;
using Amazon.CDK.AWS.SNS;

namespace SharedConstructs;

public class SnsTarget : ChannelTarget
{
    public ITopic Topic { get; }
    public SnsTarget(ITopic topic)
    {
        this.Topic = topic;
        this.TargetArn = topic.TopicArn;
        this.TargetParameters = null;
    }
    /// <inheritdoc />
    public override string TargetArn { get; }

    /// <inheritdoc />
    public override CfnPipe.PipeTargetParametersProperty? TargetParameters { get; }
}