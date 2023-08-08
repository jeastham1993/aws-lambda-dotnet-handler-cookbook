using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;
using XaasKit.CDK.AWS.Lambda.DotNet;

namespace Cdk.SharedConstructs;

using BundlingOptions = Amazon.CDK.BundlingOptions;

public class LambdaFunction : Construct
{
    public Function Function { get; set; }
    
    public LambdaFunction(Construct scope, string id, string codePath, string handler, Dictionary<string, string> environmentVariables) : base(scope, id)
    {
        var commands = new[]
        {
            "cd /asset-input",
            "export XDG_DATA_HOME=\"/tmp/DOTNET_CLI_HOME\"",
            "export DOTNET_CLI_HOME=\"/tmp/DOTNET_CLI_HOME\"",
            "export PATH=\"$PATH:/tmp/DOTNET_CLI_HOME/.dotnet/tools\"",
            "dotnet tool install -g Amazon.Lambda.Tools",
            $"dotnet lambda package -pl {codePath} -o output.zip",
            "unzip -o -d /asset-output output.zip"
        };
        
        this.Function = new DotNetFunction(this, id, new DotNetFunctionProps()
        {
            FunctionName = id,
            Runtime = Runtime.DOTNET_6,
            MemorySize = 1024,
            LogRetention = RetentionDays.ONE_DAY,
            Handler = handler,
            Environment = environmentVariables,
            Tracing = Tracing.ACTIVE,
            ProjectDir = codePath,
            Architecture = Architecture.X86_64
        });
    }
}