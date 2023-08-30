using Amazon.DynamoDBv2;
using Amazon.DynamoDBv2.Model;

namespace Stocks.Tests.Shared;

public class AsyncTestManager
{
    private readonly AmazonDynamoDBClient _dynamoDbClient;
    private readonly string _outputTableName;

    public AsyncTestManager(AmazonDynamoDBClient dynamoDbClient, string outputTableName)
    {
        _dynamoDbClient = dynamoDbClient;
        _outputTableName = outputTableName;
    }

    public async Task<bool> PollForOutput(string testIdentifier, int maxPoll = 5)
    {
        var pollCounter = 0;

        var success = false;

        while (pollCounter < maxPoll)
        {
            var output = await _dynamoDbClient.GetItemAsync(_outputTableName, new Dictionary<string, AttributeValue>(1)
            {
                { "PK", new AttributeValue(testIdentifier) }
            });

            if (output.IsItemSet)
            {
                await this._dynamoDbClient.DeleteItemAsync(
                    this._outputTableName,
                    new Dictionary<string, AttributeValue>(1)
                    {
                        { "PK", new AttributeValue(testIdentifier) }
                    });
                success = true;
                break;
            }

            await Task.Delay(TimeSpan.FromSeconds(5));
        }

        return success;
    }
}