namespace Cdk;

using System.IO;
using Amazon.CDK;
using Amazon.CDK.AWS.SSM;

using Constructs;

public class ConfigurationStack : Stack
{
    public StringParameter Parameter { get; private set; }
    
    internal ConfigurationStack(
        Construct scope,
        string id,
        string environment,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var configurationStr = parseAndValidateConfiguration(environment);

        this.Parameter = new StringParameter(
            this,
            "ConfigurationParam",
            new StringParameterProps()
            {
                ParameterName = $"/{environment}/configuration",
                StringValue = configurationStr,
                DataType = ParameterDataType.TEXT
            });
    }

    private string parseAndValidateConfiguration(string environment)
    {
        var filePath = $"./cdk/src/Cdk/configuration/{environment}_configuration.json";

        if (!File.Exists(filePath))
        {
            filePath = $"./cdk/src/Cdk/configuration/Dev_configuration.json";
        }
        
        var fileContents = File.ReadAllText(filePath);

        return fileContents;
    }
}