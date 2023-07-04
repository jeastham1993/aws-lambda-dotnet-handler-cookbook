using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;
using XaasKit.CDK.AWS.Lambda.DotNet;

namespace Cdk.SharedConstructs;

public class LambdaFunction : Construct
{
    public Function Function { get; set; }
    public LambdaFunction(Construct scope, string id, string codePath, string handler, Dictionary<string, string> environmentVariables) : base(scope, id)
    {
        this.Function = new DotNetFunction(this, id, new DotNetFunctionProps()
        {
            Runtime = Runtime.PROVIDED_AL2,
            MemorySize = 1024,
            LogRetention = RetentionDays.ONE_DAY,
            Handler = handler,
            Environment = environmentVariables,
            Tracing = Tracing.ACTIVE,
            ProjectDir = codePath,
            Architecture = Architecture.ARM_64
        });
    }
}