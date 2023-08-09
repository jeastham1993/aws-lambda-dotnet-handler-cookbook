namespace Cdk.SharedConstructs;

using Amazon.CDK.AWS.APIGateway;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.Lambda;

using Constructs;

public class CognitoAuthorizedApi : RestApi
{
   private CognitoUserPoolsAuthorizer _authorizer;
   
   public CognitoAuthorizedApi(
      Construct scope,
      string id,
      RestApiProps props,
      UserPool userPool) : base(
      scope,
      id,
      props)
   {
      this._authorizer = new CognitoUserPoolsAuthorizer(
         this,
         "CognitoAuthorizer",
         new CognitoUserPoolsAuthorizerProps
         {
            CognitoUserPools = new IUserPool[]
            {
               userPool
            },
            AuthorizerName = "cognitoauthorizer",
            IdentitySource = "method.request.header.Authorization"
         });
   }

   public IResource AddLambdaEndpoint(
      Function lambdaFunction,
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
            lastResource = this.Root.GetResource(sanitisedPathSegment) ?? this.Root.AddResource(sanitisedPathSegment);
            continue;
         }

         lastResource = lastResource.GetResource(sanitisedPathSegment) ??
                        lastResource.AddResource(sanitisedPathSegment);
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
            Authorizer = this._authorizer
         });

      return lastResource;
   }
}