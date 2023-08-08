namespace Cdk.Extensions;

using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Lambda;

public static class RestApiExtensions
{
    public static IResource AddLambdaEndpoint(
        this RestApi api,
        Function lambdaFunction,
        CognitoUserPoolsAuthorizer userPoolAuthorizer,
        string path,
        string httpMethod)
    {
        IResource? lastResource = null;  

        foreach (var pathSegment in path.Split('/'))
        {
            var sanitisedPathSegment = pathSegment.Replace(
                "/",
                "");

            if (string.IsNullOrEmpty(sanitisedPathSegment))
            {
                continue;
            }

            if (lastResource == null)
            {
                lastResource = api.Root.GetResource(sanitisedPathSegment) ?? api.Root.AddResource(sanitisedPathSegment);
                continue;
            }

            lastResource = lastResource.GetResource(sanitisedPathSegment) ?? lastResource.AddResource(sanitisedPathSegment);
        }
        
        lastResource?.AddMethod(
            httpMethod,
            new LambdaIntegration(lambdaFunction),
            new MethodOptions
            {
                MethodResponses = new IMethodResponse[]
                {
                    new MethodResponse { StatusCode = "200" },
                    new MethodResponse { StatusCode = "400" },
                    new MethodResponse { StatusCode = "500" }
                },
                AuthorizationType = AuthorizationType.COGNITO,
                Authorizer = userPoolAuthorizer
            });

        return lastResource;
    }
}