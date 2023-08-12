using Amazon.CDK;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;
using Cdk.SharedConstructs;
using Constructs;

namespace Cdk.StockPriceApi;

public record StockPriceTestInfrastructureStackProps(ITopic Topic, string Postfix);

public class StockPriceTestInfrastructureStack : Stack
{
    internal StockPriceTestInfrastructureStack(
        Construct scope,
        string id,
        StockPriceTestInfrastructureStackProps stackProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        new AsyncTestInfrastructure(this, $"StockPriceTest{stackProps.Postfix}", stackProps.Topic);
    }
}