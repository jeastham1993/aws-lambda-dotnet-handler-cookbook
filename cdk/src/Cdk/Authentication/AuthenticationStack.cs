namespace Cdk.Authentication;

using Amazon.CDK;
using Amazon.CDK.AWS.Cognito;
using Amazon.CDK.AWS.SSM;

using Constructs;

public record AuthenticationProps(string Postfix);

public class AuthenticationStack : Stack
{
    internal AuthenticationStack(
        Construct scope,
        string id,
        AuthenticationProps authProps,
        IStackProps props = null) : base(
        scope,
        id,
        props)
    {
        var userPool = new UserPool(
            this,
            $"StockPriceUserPool{authProps.Postfix}",
            new UserPoolProps
            {
                UserPoolName = $"stock-service-users{authProps.Postfix}",
                SelfSignUpEnabled = true,
                SignInAliases = new SignInAliases
                {
                    Email = true
                },
                AutoVerify = new AutoVerifiedAttrs
                {
                    Email = true
                },
                StandardAttributes = new StandardAttributes
                {
                    GivenName = new StandardAttribute
                    {
                        Required = true
                    },
                    FamilyName = new StandardAttribute
                    {
                        Required = true
                    }
                },
                PasswordPolicy = new PasswordPolicy
                {
                    MinLength = 6,
                    RequireDigits = true,
                    RequireLowercase = true,
                    RequireSymbols = false,
                    RequireUppercase = false
                },
                AccountRecovery = AccountRecovery.EMAIL_ONLY,
                RemovalPolicy = RemovalPolicy.DESTROY
            });

        var userPoolClient = new UserPoolClient(
            this,
            $"StockPriceClient{authProps.Postfix}",
            new UserPoolClientProps()
            {
                UserPool = userPool,
                UserPoolClientName = "api-login",
                AuthFlows = new AuthFlow()
                {
                    AdminUserPassword = true,
                    Custom = true,
                    UserSrp = true
                },
                SupportedIdentityProviders = new[]
                {
                    UserPoolClientIdentityProvider.COGNITO,
                },
                ReadAttributes = new ClientAttributes().WithStandardAttributes(
                    new StandardAttributesMask()
                    {
                        GivenName = true,
                        FamilyName = true,
                        Email = true,
                        EmailVerified = true
                    }),
                WriteAttributes = new ClientAttributes().WithStandardAttributes(
                    new StandardAttributesMask()
                    {
                        GivenName = true,
                        FamilyName = true,
                        Email = true
                    })
            });


        var userPoolParameter = new StringParameter(this, $"UserPoolParameter{authProps.Postfix}",
            new StringParameterProps()
            {
                ParameterName = $"/authentication/{authProps.Postfix}/user-pool-id",
                StringValue = userPool.UserPoolArn
            });
        
        var userPoolClientParameter = new StringParameter(this, $"UserPoolClientParameter{authProps.Postfix}",
            new StringParameterProps()
            {
                ParameterName = $"/authentication/{authProps.Postfix}/user-pool-client-id",
                StringValue = userPoolClient.UserPoolClientId
            });

        var userPoolOutput = new CfnOutput(this, $"UserPoolId{authProps.Postfix}", new CfnOutputProps()
        {
            Value = userPool.UserPoolId,
            ExportName = $"UserPoolId{authProps.Postfix}"
        });

        var clientIdOutput = new CfnOutput(this, $"ClientId{authProps.Postfix}", new CfnOutputProps()
        {
            Value = userPoolClient.UserPoolClientId,
            ExportName = $"ClientId{authProps.Postfix}"
        });
    }
}