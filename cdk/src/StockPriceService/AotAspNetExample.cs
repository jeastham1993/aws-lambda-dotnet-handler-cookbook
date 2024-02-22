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
                IsNativeAot = true
            }).Function;
    }
}