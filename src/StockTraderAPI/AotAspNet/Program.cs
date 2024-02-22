using Amazon.Lambda.Serialization.SystemTextJson;
using AotAspNet;

var builder = WebApplication.CreateSlimBuilder(args);

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.TypeInfoResolver = CustomSerializationContext.Default;
});

// Add AWS Lambda support. When application is run in Lambda Kestrel is swapped out as the web server with Amazon.Lambda.AspNetCoreServer. This
// package will act as the webserver translating request and responses between the Lambda event source and ASP.NET Core.
builder.Services.AddAWSLambdaHosting(LambdaEventSource.RestApi, options =>
{
    options.Serializer = new SourceGeneratorLambdaJsonSerializer<CustomSerializationContext>();
});

builder.Services.AddAuthorization();

builder.Logging.ClearProviders();
builder.Logging.AddJsonConsole(options =>
{
    options.IncludeScopes = true;
    options.UseUtcTimestamp = true;
    options.TimestampFormat = "hh:mm:ss ";
});

var app = builder.Build();

app.UseAuthorization();
app.MapControllers();

app.MapGet("/", () => "Welcome to running AOT compiled ASP.NET Core Minimal API on AWS Lambda");

app.MapGet("/_health", () => "We are healthy");

app.Run();
