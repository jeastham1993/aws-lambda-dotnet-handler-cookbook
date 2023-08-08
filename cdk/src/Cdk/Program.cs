using Amazon.CDK;

using Cdk;

var app = new App();

var postFix = System.Environment.GetEnvironmentVariable("STACK_POSTFIX");

var configStack = new ConfigurationStack(
    app,
    $"ConfigurationStack{postFix}",
    $"prod{postFix}");

var authenticationStack = new AuthenticationStack(
    app,
    $"AuthenticationStack{postFix}",
    new AuthenticationProps($"prod{postFix}"));

var stockPriceStack = new StockPriceAPIStack(
    app,
    $"StockPriceStack{postFix}",
    new StockPriceStackProps(
        postFix,
        configStack.Parameter,
        authenticationStack.UserPool));

app.Synth();
