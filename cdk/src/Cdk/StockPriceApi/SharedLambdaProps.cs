using Amazon.CDK.AWS.DynamoDB;

namespace Cdk.StockPriceApi;

public record SharedLambdaProps(
    StockPriceStackProps StackProps,
    ITable Table,
    ITable Idempotency);