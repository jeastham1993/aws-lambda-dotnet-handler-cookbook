using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.SSM;

namespace NotificationService;

public record SharedLambdaProps(
    NotificationServiceStackProps StackProps,
    ITable Table,
    ITable Idempotency,
    IStringParameter ConfigurationParameter);