using Amazon.CDK;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;
using Cdk.SharedConstructs;
using Constructs;

namespace Cdk.StockPriceApi;

public record StockPriceTestInfrastructureStackProps(string Postfix);

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
        var topicArn =
            StringParameter.ValueForStringParameter(this, $"/stocks/{stackProps.Postfix}/stock-price-updated-channel");
        
        var topic = Topic.FromTopicArn(this, "StockPriceUpdatedTopic", topicArn);
        
        new AsyncTestInfrastructure(this, $"StockPriceTest{stackProps.Postfix}", topic);
    }
}