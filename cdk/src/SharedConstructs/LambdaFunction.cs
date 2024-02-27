using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.Destinations;
using Amazon.CDK.AWS.Logs;
using Amazon.CDK.AWS.SQS;
using Constructs;
using XaasKit.CDK.AWS.Lambda.DotNet;

namespace SharedConstructs;

public class LambdaFunctionProps : FunctionProps
{
    public LambdaFunctionProps(string codePath)
    {
        CodePath = codePath;
        Postfix = "Dev";
    }
    
    public bool IsNativeAot { get; set; }
    
    public string CodePath { get; set; }
    
    public string Postfix { get; set; }
}

public class LambdaFunction : Construct
{
    public Function Function { get; }
    
    public LambdaFunction(Construct scope, string id, LambdaFunctionProps props) : base(scope, id)
    {
        this.Function = new DotNetFunction(this, id, new DotNetFunctionProps()
        {
            FunctionName = id,
            Runtime = Runtime.DOTNET_8,
            MemorySize = props.MemorySize ?? 1024,
            LogRetention = RetentionDays.ONE_DAY,
            Handler = props.Handler,
            Environment = props.Environment,
            Tracing = Tracing.ACTIVE,
            ProjectDir = props.CodePath,
            Architecture = System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture == System.Runtime.InteropServices.Architecture.Arm64 ? Architecture.ARM_64 : Architecture.X86_64,
            OnFailure = new SqsDestination(new Queue(this, $"{id}FunctionDLQ")),
        });
    }
}