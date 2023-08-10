namespace Cdk.SharedConstructs;

using System;
using System.Collections.Generic;

using Amazon.CDK.AWS.Events;
using Amazon.CDK.AWS.Events.Targets;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.Lambda.EventSources;
using Amazon.CDK.AWS.SNS;

using Constructs;

public class PublishSubscribeChannel : Construct
{
    private string _id;
    private Construct _scope;
    public ITopic Topic { get; private set; }
    
    public IEventBus EventBus { get; private set; }

    private List<IFunction> lambdaSubscribers;
    
    public PublishSubscribeChannel(Construct scope,
        string id) : base(
        scope,
        id)
    {
        this._scope = scope;
        this._id = id;

        this.lambdaSubscribers = new List<IFunction>();
    }

    public PublishSubscribeChannel WithTopicName(string topicName)
    {
        if (this.EventBus != null)
        {
            throw new ArgumentException("Cannot set Topic based publishing when an EventBus is configured");
        }
        
        this.Topic = new Topic(
            this,
            topicName);

        return this;
    }

    public PublishSubscribeChannel WithEventBus(IEventBus eventBus)
    {
        if (this.Topic != null)
        {
            throw new ArgumentException("Cannot set EventBus based publishing when a topic is configured");
        }
        
        this.EventBus = eventBus;

        return this;
    }

    public PublishSubscribeChannel WithSubscriber(IFunction lambdaFunction)
    {
        this.lambdaSubscribers.Add(lambdaFunction);

        return this;
    }

    public PublishSubscribeChannel WithFilteredSubscriber(IFunction lambdaFunction, Dictionary<string, FilterOrPolicy> filters)
    {
        if (this.Topic != null)
        {
            lambdaFunction.AddEventSource(new SnsEventSource(this.Topic, new SnsEventSourceProps()
            {
                FilterPolicyWithMessageBody = filters
            }));   
        }
        else if (this.EventBus != null)
        {
            var eventTarget = new Amazon.CDK.AWS.Events.Targets.LambdaFunction(lambdaFunction);

            var eventRule = new Rule(
                this,
                $"{this._id}Rule",
                new RuleProps()
                {
                    Enabled = true,
                    Targets = new[] { eventTarget },
                });
        }

        return this;
    }

    public PublishSubscribeChannel WithFilteredSubscriber(IFunction lambdaFunction, EventPattern pattern)
    {
        var eventTarget = new Amazon.CDK.AWS.Events.Targets.LambdaFunction(lambdaFunction);

        var eventRule = new Rule(
            this,
            $"{this._id}Rule",
            new RuleProps()
            {
                Enabled = true,
                Targets = new[] { eventTarget },
                EventPattern = pattern
            });

        return this;
    }

    public PublishSubscribeChannel Build()
    {
        if (this.Topic != null)
        {
            foreach (var function in this.lambdaSubscribers)
            {
                function.AddEventSource(new SnsEventSource(this.Topic));
            }
        }
        
        return this;
    }
}