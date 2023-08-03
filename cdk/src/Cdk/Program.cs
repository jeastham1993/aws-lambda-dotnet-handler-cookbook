using Amazon.CDK;

using Cdk;

var app = new App();

var postFix = System.Environment.GetEnvironmentVariable("STACK_POSTFIX");

var configStack = new ConfigurationStack(app, $"ConfigurationStack{postFix}", $"prod{postFix}");

new StockPriceAPIStack(app, $"StockPriceStack{postFix}", new StockPriceStackProps(postFix, configStack.Parameter));

app.Synth();
