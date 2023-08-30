using Amazon.CDK.AWS.Lambda.EventSources;

namespace Cdk.SharedConstructs;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.Pipes;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.StepFunctions;

using Constructs;

public enum Comparator
{
    GreaterThan,
    LessThan,
    EqualTo
}

public class PointToPointChannel : Construct
{
    private readonly string _id;
    private List<IChainable> _enrichmentSteps { get; }
    private Succeed _enrichmentSuccess { get; }

    private Pass _skipToEnd { get; }
    
    private ChannelSource Source { get; set; }
    
    private ChannelTarget Target { get; set; }
    

    public PointToPointChannel(
        Construct scope,
        string id) : base(
        scope,
        id)
    {
        this._id = id;
        this._enrichmentSteps = new List<IChainable>();
        this._enrichmentSuccess = new Succeed(
            this,
            "EnrichmentSuccess");

        this._skipToEnd = new Pass(
            this,
            $"{id}SkipToEnd");
    }

    public PointToPointChannel From(ChannelSource source)
    {
        this.Source = source;

        return this;
    }

    public PointToPointChannel WithMessageTranslation(string translationIdentifier, Dictionary<string, object> translatedMessage)
    {
        this._enrichmentSteps.Add(
            new Pass(
                this,
                $"{this._id}{translationIdentifier}Translator",
                new PassProps
                {
                    Parameters = translatedMessage
                }));

        return this;
    }

    public PointToPointChannel WithMessageFilter(
        string keyField,
        Comparator comparator,
        double value)
    {
        Condition comparisonCondition = null;

        switch (comparator)
        {
            case Comparator.GreaterThan:
                comparisonCondition = Condition.NumberGreaterThan(
                    $"$.{keyField}",
                    value);
                break;
            case Comparator.LessThan:
                comparisonCondition = Condition.NumberLessThan(
                    $"$.{keyField}",
                    value);
                break;
            default:
                comparisonCondition = Condition.NumberEquals(
                    $"$.{keyField}",
                    value);
                break;
        }

        var filterComplete = new Pass(
            this,
            $"{keyField}FilterComplete");

        var choice = new Choice(
                this,
                $"{keyField.Replace(".", "-")}Filter")
            .When(
                Condition.And(
                    Condition.IsPresent($"$.{keyField}"),
                    comparisonCondition),
                new Pass(
                    this,
                    $"{keyField.Replace(".", "-")}FilterPass").Next(filterComplete))
            .Otherwise(
                new Pass(
                    this,
                    $"{keyField.Replace(".", "-")}EmptyState",
                    new PassProps
                    {
                        Result = new Result("{}")
                    }).Next(_skipToEnd))
            .Afterwards();

        this._enrichmentSteps.Add(choice);

        return this;
    }

    public PointToPointChannel WithMessageFilter(string filterName, Dictionary<string, string[]> filterValue)
    {
        var filterComplete = new Pass(
            this,
            $"{filterName}FilterComplete");

        var conditions = new List<Condition>(filterValue.Count * 2);

        foreach (var filter in filterValue)
        {
            foreach (var filterString in filter.Value)
            {
                conditions.Add(Condition.StringEquals($"$.{filter.Key}", filterString));
            }
        }
        
        var checkValuesChoice = new Choice(
                this,
                $"{filterName.Replace(".", "-")}FilterValues")
            .When(
                Condition.Or(conditions.ToArray()),
                new Pass(
                    this,
                    $"{filterName.Replace(".", "-")}FilterValuesPass").Next(filterComplete))
            .Otherwise(
                new Pass(
                    this,
                    $"{filterName.Replace(".", "-")}EmptyValuesState",
                    new PassProps
                    {
                        Result = new Result("{}")
                    }).Next(_skipToEnd))
            .Afterwards();

        this._enrichmentSteps.Add(checkValuesChoice);

        return this;
    }

    public void To(ChannelTarget target)
    {
        this.Target = target;

        this.Build();
    }

    private void Build()
    {
        var pipeRole = new Role(
            this,
            "PipeRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("pipes.amazonaws.com")
            });

        switch (this.Source.GetType().Name)
        {
            case nameof(DynamoDbSource):
                (this.Source as DynamoDbSource).Table.GrantStreamRead(pipeRole);
                break;
            case nameof(SqsQueueSource):
                (this.Source as SqsQueueSource).Queue.GrantConsumeMessages(pipeRole);
                break;
        }
        
        switch (this.Target.GetType().Name)
        {
            case nameof(SnsTarget):
                (this.Target as SnsTarget).Topic.GrantPublish(pipeRole);
                break;
            case nameof(WorkflowTarget):
                (this.Target as WorkflowTarget).Workflow.GrantStartExecution(pipeRole);
                (this.Target as WorkflowTarget).Workflow.GrantStartSyncExecution(pipeRole);
                break;
        }

        StateMachine enrichment = null;

        if (this._enrichmentSteps.Any())
        {
            Chain chain = null;
            IChainable previousStep = null;

            foreach (var step in this._enrichmentSteps)
            {
                if (previousStep == null)
                {
                    previousStep = step;
                    continue;
                }

                previousStep.EndStates[1].Next(step);

                previousStep = step;
            }

            chain = Chain.Start(this._enrichmentSteps[0]);

            var loopInputRecords = new Map(
                this,
                "LoopRecords",
                new MapProps()).Iterator(chain);

            enrichment = new StateMachine(
                this,
                "StateMachine",
                new StateMachineProps
                {
                    DefinitionBody = new ChainDefinitionBody(loopInputRecords.Next(this._enrichmentSuccess)),
                    StateMachineType = StateMachineType.EXPRESS,
                    Logs = new LogOptions
                    {
                        Level = LogLevel.ALL,
                        IncludeExecutionData = true,
                        Destination = new LogGroup(
                            this,
                            $"{this._id}EnrichmentLogGroup",
                            new LogGroupProps()
                            {
                                LogGroupName = $"/aws/vendedlogs/states/{this._id}EnrichmentLogGroup"
                            })
                    }
                });

            enrichment.GrantStartSyncExecution(pipeRole);
        }

        var pipe = new CfnPipe(
            this,
            $"{this._id}-Pipe",
            new CfnPipeProps
            {
                Name = $"{this._id}Pipe",
                RoleArn = pipeRole.RoleArn,
                Source = this.Source.SourceArn,
                SourceParameters = this.Source.SourceParameters,
                Enrichment = enrichment?.StateMachineArn,
                Target = this.Target.TargetArn,
                TargetParameters = this.Target.TargetParameters
            });
    }
}