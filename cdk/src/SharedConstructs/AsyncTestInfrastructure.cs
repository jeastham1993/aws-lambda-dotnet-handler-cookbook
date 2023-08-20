using Amazon.CDK;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.Pipes;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;

namespace Cdk.SharedConstructs;

public class AsyncTestInfrastructure : Construct
{
    public AsyncTestInfrastructure(Construct scope, string id, ITopic source) : base(scope, id)
    {
        var testInfrastructureTable = new Table(this, $"{id}TestTable", new TableProps
        {
            BillingMode = BillingMode.PAY_PER_REQUEST,
            PartitionKey = new Amazon.CDK.AWS.DynamoDB.Attribute
            {
                Name = "PK",
                Type = AttributeType.STRING
            },
            Stream = StreamViewType.NEW_AND_OLD_IMAGES,
            RemovalPolicy = RemovalPolicy.DESTROY
        });

        var queueSubscription = new Queue(this, $"{id}TestQueue");

        queueSubscription.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
        {
            Effect = Effect.ALLOW,
            Conditions = new Dictionary<string, object>(1)
            {
                {
                    "ArnEquals", new Dictionary<string, string>
                    {
                        { "aws:SourceArn", source.TopicArn }
                    }
                }
            },
            Resources = new[] { queueSubscription.QueueArn },
            Principals = new[] { new ServicePrincipal("sns.amazonaws.com") },
            Actions = new[] {"sqs:SendMessage"}
        }));

        source.AddSubscription(new SqsSubscription(queueSubscription));

        var messageProcessingChain = Chain.Start(new Pass(this, $"{id}FormatInput", new PassProps()
        {
            Parameters = new Dictionary<string, object>(1)
            {
                { "MessageData.$", "States.StringToJson($.body)" }
            }
        })).Next(new Pass(this, $"{id}FormatMessage", new PassProps()
        {
            Parameters = new Dictionary<string, object>(1)
            {
                { "MessageData.$", "States.StringToJson($.MessageData.Message)" }
            }
        }).Next(new DynamoPutItem(this, $"{id}DynamoPut",
            new DynamoPutItemProps
            {
                Item = new Dictionary<string, DynamoAttributeValue>(1)
                {
                    { "PK", DynamoAttributeValue.FromString(JsonPath.StringAt("$.MessageData.Data.StockSymbol")) }
                },
                Table = testInfrastructureTable
            }).AddCatch(new Succeed(this, $"{id}MapFallbackSuccess"))));

        var chain = Chain.Start(new Map(this, $"{id}TestMap")
            .Iterator(messageProcessingChain).AddCatch(new Succeed(this, $"{id}FallbackSuccess")));

        var integrationTestWorkflow = new StateMachine(this, $"{id}TestWorkflow", new StateMachineProps
        {
            StateMachineType = StateMachineType.EXPRESS,
            DefinitionBody = DefinitionBody.FromChainable(chain),
            Logs = new LogOptions
            {
                Level = LogLevel.ALL,
                IncludeExecutionData = true,
                Destination = new LogGroup(
                    this,
                    $"{id}AsyncTestLogGroup")
            }
        });

        var pipeRole = new Role(
            this,
            "PipeRole",
            new RoleProps
            {
                AssumedBy = new ServicePrincipal("pipes.amazonaws.com")
            });

        queueSubscription.GrantConsumeMessages(pipeRole);
        integrationTestWorkflow.GrantStartExecution(pipeRole);
        integrationTestWorkflow.GrantStartSyncExecution(pipeRole);

        var pipe = new CfnPipe(this, $"{id}TestPipe", new CfnPipeProps
        {
            RoleArn = pipeRole.RoleArn,
            Source = queueSubscription.QueueArn,
            SourceParameters = new CfnPipe.PipeSourceParametersProperty
            {
                SqsQueueParameters = new CfnPipe.PipeSourceSqsQueueParametersProperty
                {
                    BatchSize = 1
                }
            },
            Target = integrationTestWorkflow.StateMachineArn,
            TargetParameters = new CfnPipe.PipeTargetParametersProperty
            {
                StepFunctionStateMachineParameters = new CfnPipe.PipeTargetStateMachineParametersProperty()
                {
                }
            }
        });

        var output = new CfnOutput(this, $"{id}AsyncTableOutput", new CfnOutputProps()
        {
            ExportName = id,
            Value = testInfrastructureTable.TableName
        });
    }
}