using Amazon.CDK;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;
using Constructs;

namespace NotificationService;

public record NotificationServiceTestInfrastructureStackProps(string Postfix);

public class NotificationServiceTestInfrastructureStack : Stack
{
    internal NotificationServiceTestInfrastructureStack(
        Construct scope,
        string id,
        NotificationServiceTestInfrastructureStackProps stackProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
    }
}