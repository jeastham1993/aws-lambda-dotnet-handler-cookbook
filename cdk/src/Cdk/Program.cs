using Amazon.CDK;

using Cdk;

var app = new App();

var configStack = new ConfigurationStack(app, "ConfigurationStack", "prod");

new StockPriceAPIStack(app, "StockPriceStack", new StockPriceStackProps(configStack.Parameter));

app.Synth();
