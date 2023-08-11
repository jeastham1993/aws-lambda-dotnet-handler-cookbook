namespace Cdk;

using System.Collections.Generic;

using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;

using Cdk.SharedConstructs;

using Constructs;

public class GetStockPriceEndpoint : Construct
{
    public Function Function { get; }

    public GetStockPriceEndpoint(
        Construct scope,
        string id,
        SharedLambdaProps props) : base(
        scope,
        id)
    {
        this.Function = new LambdaFunction(
            this,
            $"GetStockPriceEndpoint{props.StackProps.Postfix}",
            "./src/StockTraderAPI/StockTrader.API/bin/Release/net7.0/StockTrader.API.zip",
            "StockTrader.API::StockTrader.API.Endpoints.GetStockPriceEndpoint_GetStockPrice_Generated::GetStockPrice",
            new Dictionary<string, string>(1)
            {
                { "TABLE_NAME", props.Table.TableName },
                { "IDEMPOTENCY_TABLE_NAME", props.Idempotency.TableName },
                { "ENV", props.StackProps.Postfix },
                { "POWERTOOLS_SERVICE_NAME", $"StockPriceApi{props.StackProps.Postfix}" },
                { "CONFIGURATION_PARAM_NAME", props.StackProps.Parameter.ParameterName }
            },
            isNativeAot: true).Function;

        props.Table.GrantReadWriteData(this.Function);
        props.Idempotency.GrantReadWriteData(this.Function);
        props.StackProps.Parameter.GrantRead(this.Function);

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
                                Resources = new[] { props.StackProps.Parameter.ParameterArn }
                            })
                    }
                }));
    }
}