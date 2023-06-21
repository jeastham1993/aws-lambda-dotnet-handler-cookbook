namespace Cdk;

using Amazon.CDK;

using Constructs;

public class ConfigurationStack : Stack
{
    internal ConfigurationStack(
        Construct scope,
        string id,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
    }
}