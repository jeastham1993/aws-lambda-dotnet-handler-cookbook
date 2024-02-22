using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Pipes;
using Amazon.CDK.AWS.SNS;
using Constructs;

namespace SharedConstructs;

public class TableToSnsChannel : Construct
{
    public Topic SnsTopic { get; private set; }
    
    public TableToSnsChannel(Construct scope, string id, ITable table, string topicName, string transformerFile = null) : base(scope, id)
    {
        this.SnsTopic = new Topic(this, topicName, new TopicProps());

        var pipeRole = new Role(
            this,
            "PipeRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("pipes.amazonaws.com")
            });

        table.GrantStreamRead(pipeRole);
        SnsTopic.GrantPublish(pipeRole);

        var pipe = new CfnPipe(
            this,
            $"{id}-Pipe",
            new CfnPipeProps
            {
                Name = $"{id}Pipe",
                RoleArn = pipeRole.RoleArn,
                Source = table.TableStreamArn,
                SourceParameters = new CfnPipe.PipeSourceParametersProperty
                {
                    DynamoDbStreamParameters = new CfnPipe.PipeSourceDynamoDBStreamParametersProperty()
                    {
                        StartingPosition = "LATEST",
                        BatchSize = 1
                    }
                },
                Target = SnsTopic.TopicArn,
                TargetParameters = new CfnPipe.PipeTargetParametersProperty()
                {
                    InputTemplate = transformerFile == null ? null : File.ReadAllText(transformerFile),
                },
            });
    }
}