using Amazon.CDK;
using Amazon.CDK.AWS.SNS;
using Amazon.CDK.AWS.SSM;
using NotificationService;

var app = new App();

var postFix = System.Environment.GetEnvironmentVariable("STACK_POSTFIX");

var configStack = new ConfigurationStack(
    app,
    $"ConfigurationStack{postFix}",
    $"{postFix}");

var notificationServiceStack = new NotificationServiceStack(
    app,
    $"NotificationServiceStack{postFix}",
    new NotificationServiceStackProps(
        postFix));

var testInfrastructure = new NotificationServiceTestInfrastructureStack(app, $"NotificationTestInfrastructure{postFix}",
    new NotificationServiceTestInfrastructureStackProps(postFix));

app.Synth();