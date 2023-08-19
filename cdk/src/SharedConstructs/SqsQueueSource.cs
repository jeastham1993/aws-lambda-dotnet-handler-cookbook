using Amazon.CDK.AWS.SQS;

namespace Cdk.SharedConstructs;

using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Pipes;

public class SqsQueueSource : ChannelSource
{
    public IQueue Queue { get; }

    public SqsQueueSource(IQueue queue)
    {
        this.Queue = queue;
        this.SourceParameters = new CfnPipe.PipeSourceParametersProperty
        {
            SqsQueueParameters = new CfnPipe.PipeSourceSqsQueueParametersProperty()
            {
                BatchSize = 1
            }
        };
    }

    /// <inheritdoc />
    public override string SourceArn => this.Queue.QueueArn;

    /// <inheritdoc />
    public override CfnPipe.PipeSourceParametersProperty SourceParameters { get; }
}