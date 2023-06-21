using Amazon.CDK;

using Cdk;

var app = new App();

new ConfigurationStack(app, "ConfigurationStack", new StackProps());
new StockPriceStack(app, "StockPriceStack", new StackProps { });

app.Synth();
