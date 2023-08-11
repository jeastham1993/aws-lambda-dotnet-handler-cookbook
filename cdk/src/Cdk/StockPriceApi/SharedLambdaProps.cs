namespace Cdk;

using Amazon.CDK.AWS.DynamoDB;

public record SharedLambdaProps(
    StockPriceStackProps StackProps,
    ITable Table,
    ITable Idempotency);