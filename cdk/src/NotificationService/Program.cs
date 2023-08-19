using Amazon.CDK;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;
using NotificationService;

var app = new App();

var postFix = System.Environment.GetEnvironmentVariable("STACK_POSTFIX");

var topicArn =
    StringParameter.ValueForStringParameter(app, $"/stocks/{postFix}/stock-price-updated-channel");

var stockPriceUpdatedTopic = Topic.FromTopicArn(app, "StockPriceUpdatedTopic", topicArn);

var configStack = new ConfigurationStack(
    app,
    $"ConfigurationStack{postFix}",
    $"{postFix}");

var notificationServiceStack = new NotificationServiceStack(
    app,
    $"NotificationServiceStack{postFix}",
    new NotificationServiceStackProps(
        postFix,
        stockPriceUpdatedTopic));

var testInfrastructure = new NotificationServiceTestInfrastructureStack(app, $"NotificationTestInfrastructure{postFix}",
    new NotificationServiceTestInfrastructureStackProps(postFix));

app.Synth();