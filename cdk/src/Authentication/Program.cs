using Amazon.CDK;

using Cdk.Authentication;

var app = new App();

var postFix = System.Environment.GetEnvironmentVariable("STACK_POSTFIX");

var authenticationStack = new AuthenticationStack(
    app,
    $"AuthenticationStack{postFix}",
    new AuthenticationProps($"{postFix}"));

app.Synth();


