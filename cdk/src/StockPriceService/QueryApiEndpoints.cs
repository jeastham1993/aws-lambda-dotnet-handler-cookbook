using System.Collections.Generic;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using SharedConstructs;

namespace Cdk.StockPriceApi;

public class QueryApiEndpoints : Construct
{
    public Function Function { get; }

    public QueryApiEndpoints(
        Construct scope,
        string id,
        SharedLambdaProps props) : base(
        scope,
        id)
    {
        this.Function = new LambdaFunction(
            this,
            $"StockQueryEndpoints{props.StackProps.Postfix}",
            new LambdaFunctionProps("./src/StockTraderAPI/StockTrader.API/bin/Release/net8.0/StockTrader.API.zip")
            {
                Handler = "bootstrap",
            Environment = new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", props.Table.TableName },
                { "IDEMPOTENCY_TABLE_NAME", props.Idempotency.TableName },
                { "ENV", props.StackProps.Postfix },
                { "POWERTOOLS_SERVICE_NAME", $"StockPriceApi{props.StackProps.Postfix}" },
                { "CONFIGURATION_PARAM_NAME", props.ConfigurationParameter.ParameterName }
            },
            IsNativeAot = true
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