﻿namespace Cdk;

using System.IO;
using System.Net;

using Amazon.CDK;
using Amazon.CDK.AWS.AppConfig;
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
        var fileContents = File.ReadAllText($"./src/Cdk/configuration/{environment}_configuration.json");

        return fileContents;
    }
}