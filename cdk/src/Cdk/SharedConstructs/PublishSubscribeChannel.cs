namespace Cdk.SharedConstructs;

using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SNS;

using Constructs;

public class PublishSubscribeChannel : Construct
{
    private string _id;
    private Construct _scope;
    public ITopic Topic { get; private set; }
    
    public PublishSubscribeChannel(Construct scope,
        string id) : base(
        scope,
        id)
    {
        this._scope = scope;
        this._id = id;
    }

    public PublishSubscribeChannel WithTopicName(string topicName)
    {
        this.Topic = new Topic(
            this,
            topicName);

        return this;
    }

    public PublishSubscribeChannel WithSubscriber(IFunction lambdaFunction)
    {
        lambdaFunction.AddEventSource(new SnsEventSource(this.Topic));

        return this;
    }

    public PublishSubscribeChannel Build()
    {
        return this;
    }
}