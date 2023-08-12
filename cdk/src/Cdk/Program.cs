using Amazon.CDK;

using Cdk;
using Cdk.StockPriceApi;

var app = new App();

var postFix = System.Environment.GetEnvironmentVariable("STACK_POSTFIX");

var configStack = new ConfigurationStack(
    app,
    $"ConfigurationStack{postFix}",
    $"{postFix}");

var authenticationStack = new AuthenticationStack(
    app,
    $"AuthenticationStack{postFix}",
    new AuthenticationProps($"{postFix}"));

var stockPriceStack = new StockPriceApiStack(
    app,
    $"StockPriceStack{postFix}",
    new StockPriceStackProps(
        postFix));

var testInfrastructure = new StockPriceTestInfrastructureStack(app, $"StockTestInfrastructure{postFix}",
    new StockPriceTestInfrastructureStackProps(postFix));

app.Synth();


