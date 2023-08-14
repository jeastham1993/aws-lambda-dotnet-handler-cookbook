using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SSM;

namespace Cdk.StockPriceApi;

public record SharedLambdaProps(
    StockPriceStackProps StackProps,
    ITable Table,
    ITable Idempotency,
    IStringParameter ConfigurationParameter);