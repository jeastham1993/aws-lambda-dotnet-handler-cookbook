using System.Collections.Generic;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;

namespace NotificationService
{
    public static class WorkflowStep
    {
        public static DynamoPutItem StoreApiData(Construct scope, ITable apiTable)
        {
            return new DynamoPutItem(scope, "StoreApiInput", new DynamoPutItemProps()
            {
                Table = apiTable,
                ResultPath = "$.output",
                Item = new Dictionary<string, DynamoAttributeValue>(1)
                {
                    {"PK", DynamoAttributeValue.FromString(JsonPath.StringAt("$.customerId"))},
                    {"SK", DynamoAttributeValue.FromString(JsonPath.StringAt("$.stockSymbol"))},
                    {"GSI1PK", DynamoAttributeValue.FromString(JsonPath.StringAt("$.stockSymbol"))},
                    {"GSI1SK", DynamoAttributeValue.FromString(JsonPath.StringAt("$.customerId"))},
                },
            });
        }
    }
}