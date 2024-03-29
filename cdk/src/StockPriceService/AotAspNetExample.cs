using System.Collections.Generic;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Cdk.StockPriceApi;
using Constructs;
using SharedConstructs;

namespace StockPriceService;

public class AotAspNetExample: Construct
{
    public Function Function { get; }

    public AotAspNetExample(
        Construct scope,
        string id,
        SharedLambdaProps props) : base(
        scope,
        id)
    {
        this.Function = new LambdaFunction(
            this,
            $"AspnetAot{props.StackProps.Postfix}",
            new LambdaFunctionProps("./src/StockTraderAPI/AotAspNet")
            {
                Handler = "AotAspNet",
                IsNativeAot = true,
                Environment = new Dictionary<string, string>(1)
                {
                    { "TABLE_NAME", props.Table.TableName },
                    { "IDEMPOTENCY_TABLE_NAME", props.Idempotency.TableName },
                    { "ENV", props.StackProps.Postfix },
                    { "POWERTOOLS_SERVICE_NAME", $"StockPriceApi{props.StackProps.Postfix}" },
                    { "CONFIGURATION_PARAM_NAME", props.ConfigurationParameter.ParameterName }
                },
                MemorySize = 2048
            }).Function;

        props.Table.GrantReadWriteData(this.Function);
        props.Idempotency.GrantReadWriteData(this.Function);
        props.ConfigurationParameter.GrantRead(this.Function);

        this.Function.Role.AttachInlinePolicy(
            new Policy(
                this,
                "DescribeEventBus",
                new PolicyProps
                {
                    Statements = new[]
                    {
                        new PolicyStatement(
                            new PolicyStatementProps
                            {
                                Actions = new[] { "ssm:GetParametersByPath" },
                                Resources = new[] { props.ConfigurationParameter.ParameterArn }
                            })
                    }
                }));
    }
}