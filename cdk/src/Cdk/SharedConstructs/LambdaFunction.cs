using System.Collections.Generic;
using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Logs;
using Constructs;

namespace Cdk.SharedConstructs;

public class LambdaFunction : Construct
{
    public Function Function { get; set; }
    public LambdaFunction(Construct scope, string id, string codePath, string handler, Dictionary<string, string> environmentVariables) : base(scope, id)
    {
        var buildOption = new BundlingOptions()
        {
            Image = Runtime.DOTNET_6.BundlingImage,
            User = "root",
            OutputType = BundlingOutput.ARCHIVED,
            Command = new string[]{
                "/bin/sh",
                "-c",
                " dotnet tool install -g Amazon.Lambda.Tools"+
                " && dotnet build"+
                " && dotnet lambda package --output-package /asset-output/function.zip"
            }
        };
        
        this.Function = new Function(this, id, new FunctionProps
        {
            Runtime = Runtime.DOTNET_6,
            MemorySize = 1024,
            LogRetention = RetentionDays.ONE_DAY,
            Handler = handler,
            Environment = environmentVariables,
            Tracing = Tracing.ACTIVE,
            Code = Code.FromAsset(codePath, new Amazon.CDK.AWS.S3.Assets.AssetOptions
            {
                Bundling = buildOption
            }),
        });
    }
}