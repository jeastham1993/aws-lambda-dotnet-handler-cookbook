using System.Collections.Generic;
using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.Lambda;
using Constructs;
using SharedConstructs;
using StockPriceService;

namespace Cdk.StockPriceApi;

public class SetStockPriceEndpoint : Construct
{
    public Function Function { get; }

    public SetStockPriceEndpoint(
        Construct scope,
        string id,
        SharedLambdaProps props) : base(
        scope,
        id)
    {
        var serviceName = $"StockPriceApi{props.StackProps.Postfix}";
        
        this.Function = new LambdaFunction(
            this,
            $"SetStockPriceEndpoint{props.StackProps.Postfix}",
            new LambdaFunctionProps("./src/StockTraderAPI/StockTrader.SetStockPriceFunction")
            {
                Handler    = "StockTrader.SetStockPriceFunction::StockTrader.SetStockPriceHandler.Function_SetStockPrice_Generated::SetStockPrice",
                Environment = new Dictionary<string, string>(1)
                {
                    { "TABLE_NAME", props.Table.TableName },
                    { "IDEMPOTENCY_TABLE_NAME", props.Idempotency.TableName },
                    { "ENV", props.StackProps.Postfix },
                    { "POWERTOOLS_SERVICE_NAME", serviceName },
                    { "CONFIGURATION_PARAM_NAME", props.ConfigurationParameter.ParameterName },
                    { "SERVICE_NAME", serviceName },
                    { "EVENT_BUS_NAME", props.EventBus.EventBusName}
                }
            }).Function;

        props.Table.GrantReadWriteData(this.Function);
        props.Idempotency.GrantReadWriteData(this.Function);
        props.ConfigurationParameter.GrantRead(this.Function);
        props.EventBus.GrantPutEventsTo(this.Function);

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