using System.Text.Json;

using Amazon.CognitoIdentityProvider;
using Amazon.CognitoIdentityProvider.Model;

var cognitoClient = new AmazonCognitoIdentityProviderClient();

Console.WriteLine("What is the UserPool ID?");
var userPoolId = Console.ReadLine();
    
Console.WriteLine("What is the UserPool Client ID?");
var userPoolClientId = Console.ReadLine();

Console.WriteLine("If you have already configured the client please enter the password? If you haven't, just press enter.");

var preConfiguredPassword = Console.ReadLine();

if (string.IsNullOrEmpty(preConfiguredPassword))
{
    var createdUser = await cognitoClient.AdminCreateUserAsync(new AdminCreateUserRequest()
    {
        UserPoolId = userPoolId,
        Username = "john@example.com",
        UserAttributes = new List<AttributeType>(2)
        {
            new()
            {
                Name = "given_name",
                Value = "John"
            },
            new()
            {
                Name = "family_name",
                Value = "Doe"
            }
        }
    });
    
    Console.WriteLine("Created user");
    
    Console.WriteLine("What password would you like to use?");
    var password = Console.ReadLine();
    
    var setUserPassword = await cognitoClient.AdminSetUserPasswordAsync(
        new AdminSetUserPasswordRequest()
        {
            UserPoolId = userPoolId,
            Username = "john@example.com",
            Permanent = true,
            Password = password
        });
    
    var authOutput = await cognitoClient.AdminInitiateAuthAsync(
        new AdminInitiateAuthRequest()
        {
            UserPoolId = userPoolId,
            ClientId = userPoolClientId,
            AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
            AuthParameters = new Dictionary<string, string>(2)
            {
                {"USERNAME", "john@example.com"},
                {"PASSWORD", password},
            }
        });
    
    Console.WriteLine("TOKEN:");
    Console.WriteLine(authOutput.AuthenticationResult.IdToken);

    return;
}
    
var preConfiguredAuthOutput = await cognitoClient.AdminInitiateAuthAsync(
    new AdminInitiateAuthRequest()
    {
        UserPoolId = userPoolId,
        ClientId = userPoolClientId,
        AuthFlow = AuthFlowType.ADMIN_NO_SRP_AUTH,
        AuthParameters = new Dictionary<string, string>(2)
        {
            {"USERNAME", "john@example.com"},
            {"PASSWORD", preConfiguredPassword},
        }
    });

Console.WriteLine("TOKEN:");
Console.WriteLine(preConfiguredAuthOutput.AuthenticationResult.IdToken);