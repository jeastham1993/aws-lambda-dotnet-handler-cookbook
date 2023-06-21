// See https://aka.ms/new-console-template for more information

using System.Data.Common;
using System.Text.Json;
using System.Threading.Channels;

using AWS.Lambda.Powertools.Parameters;
using AWS.Lambda.Powertools.Parameters.SimpleSystemsManagement;

using Microsoft.Extensions.Configuration;

using Temp;

var config = new ConfigurationBuilder()
    .AddSystemsManager($"/test")
    .AddEnvironmentVariables()
    .Build();

ISsmProvider provider = ParametersManager.SsmProvider;

var dataString = provider.Get("/test");

Console.WriteLine(dataString);

var data = JsonSerializer.Deserialize<Dictionary<string, object>>(dataString);

Console.WriteLine(data.Count);

var featureFlags = new FeatureFlags(data);

var enabledFeatures = featureFlags.GetEnabledFeatures(new Dictionary<string, object>(1)
{
    { "customer_name", "James Eastham" }
});

Console.WriteLine(enabledFeatures.Count);