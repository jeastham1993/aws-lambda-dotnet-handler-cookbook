using Amazon.CDK.AWS.IAM;
using Amazon.CDK.AWS.SNS.Subscriptions;
using Amazon.CDK.AWS.SQS;

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
    private readonly string _id;
    public ITopic Topic { get; private set; }
    
    public IEventBus EventBus { get; private set; }

    private readonly List<IFunction> lambdaSubscribers;

    private readonly List<IQueue> queueSubscribers;
    
    public PublishSubscribeChannel(Construct scope,
        string id) : base(
        scope,
        id)
    {
        this._id = id;

        this.lambdaSubscribers = new List<IFunction>();
        this.queueSubscribers = new List<IQueue>();
    }

    public PublishSubscribeChannel SubscribeTo(string topicName)
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

    public PublishSubscribeChannel SubscribeTo(ITopic topic)
    {
        if (this.EventBus != null)
        {
            throw new ArgumentException("Cannot set Topic based publishing when an EventBus is configured");
        }

        this.Topic = topic;

        return this;
    }

    public PublishSubscribeChannel SubscribeTo(IEventBus eventBus)
    {
        if (this.Topic != null)
        {
            throw new ArgumentException("Cannot set EventBus based publishing when a topic is configured");
        }
        
        this.EventBus = eventBus;

        return this;
    }

    public PublishSubscribeChannel Targeting(IFunction lambdaFunction)
    {
        this.lambdaSubscribers.Add(lambdaFunction);

        return this;
    }

    public PublishSubscribeChannel Targeting(IQueue queue)
    {
        this.queueSubscribers.Add(queue);

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

            foreach (var queue in this.queueSubscribers)
            {
                queue.AddToResourcePolicy(new PolicyStatement(new PolicyStatementProps
                {
                    Effect = Effect.ALLOW,
                    Conditions = new Dictionary<string, object>(1)
                    {
                        {
                            "ArnEquals", new Dictionary<string, string>
                            {
                                { "aws:SourceArn", this.Topic.TopicArn }
                            }
                        }
                    },
                    Resources = new[] { queue.QueueArn },
                    Principals = new[] { new ServicePrincipal("sns.amazonaws.com") },
                    Actions = new[] {"sqs:SendMessage"}
                }));

                this.Topic.AddSubscription(new SqsSubscription(queue));
            }
        }
        
        return this;
    }
}