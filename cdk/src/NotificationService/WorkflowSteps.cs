using System.Collections.Generic;
using Amazon.CDK.AWS.DynamoDB;
using Amazon.CDK.AWS.StepFunctions;
using Amazon.CDK.AWS.StepFunctions.Tasks;
using Constructs;

namespace NotificationService
{
    public static class WorkflowStep
    {
        public static Pass ParseSQSInput(Construct scope)
        {
            return new Pass(scope, "ParseInput", new PassProps
            {
                Parameters = new Dictionary<string, object>(1)
                {
                    { "parsed.$", "States.StringToJson($.body)" }
                },
                OutputPath = JsonPath.StringAt("$.parsed")
            });
        }
        
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

        public static CallAwsService QueryForStockNotificationRequests(Construct scope, ITable stockNotificationTable)
        {
            return new CallAwsService(scope,
                "QueryStockItems", new CallAwsServiceProps
                {
                    Action = "query",
                    IamResources = new string[]
                    {
                        stockNotificationTable.TableArn
                    },
                    Service = "dynamodb",
                    ResultPath = "$.queryResults",
                    Parameters = new Dictionary<string, object>()
                    {
                        { "TableName", stockNotificationTable.TableName },
                        { "IndexName", "GSI1" },
                        { "KeyConditionExpression", "GSI1PK = :pk" },
                        {
                            "ExpressionAttributeValues", new Dictionary<string, object>()
                            {
                                {
                                    ":pk", new Dictionary<string, object>()
                                    {
                                        { "S", JsonPath.StringAt("$.Body.Data.StockSymbol") }
                                    }
                                }
                            }
                        }
                    }
                });
        }

        public static Chain SendNotification(Construct scope, ITable table)
        {
            return Chain.Start(new DynamoPutItem(scope, "AuditEmailSend", new DynamoPutItemProps
            {
                OutputPath = JsonPath.DISCARD,
                Item = new Dictionary<string, DynamoAttributeValue>(2)
                {
                    {"PK", DynamoAttributeValue.FromString(JsonPath.StringAt("States.Format('AUDIT#{}', $.PK.S)"))},
                    {"SK", DynamoAttributeValue.FromString(JsonPath.StringAt("$.SK.S"))},
                },
                Table = table,
            }));
        }
    }
}