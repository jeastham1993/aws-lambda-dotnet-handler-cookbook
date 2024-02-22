using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Pipes;

namespace SharedConstructs;

public class DynamoDbSource : ChannelSource
{
    public ITable Table { get; }

    public DynamoDbSource(ITable table)
    {
        this.Table = table;
        this.SourceParameters = new CfnPipe.PipeSourceParametersProperty
        {
            DynamoDbStreamParameters = new CfnPipe.PipeSourceDynamoDBStreamParametersProperty
            {
                StartingPosition = "LATEST",
                BatchSize = 1
            }
        };
    }

    /// <inheritdoc />
    public override string SourceArn => this.Table.TableStreamArn;

    /// <inheritdoc />
    public override CfnPipe.PipeSourceParametersProperty SourceParameters { get; }
}