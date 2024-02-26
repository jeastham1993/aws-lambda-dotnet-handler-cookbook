using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.SSM;
using Cdk.StockPriceApi;

namespace StockPriceService;

public record SharedLambdaProps(
    StockPriceStackProps StackProps,
    ITable Table,
    ITable Idempotency,
    IStringParameter ConfigurationParameter,
    IEventBus EventBus);