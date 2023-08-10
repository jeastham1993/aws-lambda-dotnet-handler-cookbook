namespace Cdk.SharedConstructs;

using System;
using System.IO;

using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Pipes;
using Amazon.CDK.AWS.SNS;

using Constructs;

public class PointToPointChannel : Construct
{
    private string _id;
    private Construct _scope;
    private string _inputTransformer;

    public ITable TableSource { get; private set; }
    
    public ITopic Topic { get; private set; }
    
    public PointToPointChannel(Construct scope,
        string id) : base(
        scope,
        id)
    {
        this._scope = scope;
        this._id = id;
    }

    public PointToPointChannel WithSource(ITable table)
    {
        this.TableSource = table;

        return this;
    }

    public PointToPointChannel WithInputTransformerFromFile(string filePath)
    {
        if (!File.Exists(filePath))
        {
            throw new ArgumentException(
                "File not found",
                nameof(filePath));
        }

        this._inputTransformer = File.ReadAllText(filePath);

        return this;
    }

    public PointToPointChannel WithTarget(Topic topic)
    {
        this.Topic = topic;

        return this;
    }

    public PointToPointChannel WithTarget(PublishSubscribeChannel channel)
    {
        if (channel.Topic != null)
        {
            this.Topic = channel.Topic;
        }

        return this;
    }

    public PointToPointChannel Build()
    {
        var pipeRole = new Role(
            this,
            "PipeRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("pipes.amazonaws.com")
            });

        this.TableSource.GrantStreamRead(pipeRole);
        this.Topic.GrantPublish(pipeRole);

        var pipe = new CfnPipe(
            this,
            $"{this._id}-Pipe",
            new CfnPipeProps
            {
                Name = $"{this._id}Pipe",
                RoleArn = pipeRole.RoleArn,
                Source = this.TableSource.TableStreamArn,
                SourceParameters = new CfnPipe.PipeSourceParametersProperty
                {
                    DynamoDbStreamParameters = new CfnPipe.PipeSourceDynamoDBStreamParametersProperty()
                    {
                        StartingPosition = "LATEST",
                        BatchSize = 1
                    }
                },
                Target = this.Topic.TopicArn,
                TargetParameters = new CfnPipe.PipeTargetParametersProperty()
                {
                    InputTemplate = this._inputTransformer,
                },
            });

        return this;
    }
}