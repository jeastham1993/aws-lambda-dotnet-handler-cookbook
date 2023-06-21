
# AWS Lambda Handler Cookbook (Python)

[![license](https://img.shields.io/github/license/jeastham1993/aws-lambda-dotnet-handler-cookbook)](https://github.com/jeastham1993/aws-lambda-handler-dotnet-cookbook/blob/master/LICENSE)
![github-star-badge](https://img.shields.io/github/stars/jeastham1993/aws-lambda-dotnet-handler-cookbook.svg?style=social)
![issues](https://img.shields.io/github/issues/jeastham1993/aws-lambda-dotnet-handler-cookbook)

This project provides a working, open source based, AWS Lambda handler skeleton .NET code including DEPLOYMENT code with CDK and a pipeline.

This project can serve as a template for new Serverless services - CDK deployment code, pipeline and handler are covered.

## Cookiecutter Option

## **The Problem**

Starting a Serverless service can be overwhelming. You need to figure out many questions and challenges that have nothing to do with your business domain:

- How to deploy to the cloud? What IAC framework do you choose?
- How to write a SaaS-oriented CI/CD pipeline? What does it need to contain?
- How do you handle observability, logging, tracing, metrics?
- How do you handle testing?
- What makes an AWS Lambda handler resilient, traceable, and easy to maintain? How do you write such a code?


## **The Solution**

This project aims to reduce cognitive load and answer these questions for you by providing a skeleton .NET Serverless service template that implements best practices for AWS Lambda, Serverless CI/CD, and AWS CDK in one template project.

### Serverless Service - The Order service

- This project provides a working orders service where customers can create orders of items.

- The project deploys an API GW with an AWS Lambda integration under the path POST /api/orders/ and stores data in a DynamoDB table.

![design](https://github.com/jeastham1993/aws-lambda-dotnet-handler-cookbook/blob/main/docs/media/design.png?raw=true)
<br></br>

### **Features**

- .NET Serverless service with a recommended file structure.
- CDK infrastructure with infrastructure tests and security tests.
- CI/CD pipelines based on Github actions that deploys to AWS with python linters, complexity checks and style formatters.
- Makefile for simple developer experience.
- The AWS Lambda handler embodies Serverless best practices and has all the bells and whistles for a proper production ready handler.
- AWS Lambda handler uses [AWS Lambda Powertools](https://docs.powertools.aws.dev/lambda-dotnet/){:target="_blank" rel="noopener"}.
- AWS Lambda handler 3 layer architecture: handler layer, logic layer and data access layer
- Features flags and configuration based on AWS AppConfig
- Idempotent API
- Unit, infrastructure, security, integration and end to end tests.


## CDK Deployment
The CDK code create an API GW with a path of /api/orders which triggers the lambda on 'POST' requests.

The AWS Lambda handler uses a Lambda layer optimization which takes all the packages under the [packages] section in the Pipfile and downloads them in via a Docker instance.

This allows you to package any custom dependencies you might have, just add them to the Pipfile under the [packages] section.

## Serverless Best Practices
The AWS Lambda handler will implement multiple best practice utilities.

Each utility is implemented when a new blog post is published about that utility.

The utilities cover multiple aspect of a production-ready service, including:

- [Logging](https://www.ranthebuilder.cloud/post/aws-lambda-cookbook-elevate-your-handler-s-code-part-1-logging)
- [Observability: Monitoring and Tracing](https://www.ranthebuilder.cloud/post/aws-lambda-cookbook-elevate-your-handler-s-code-part-2-observability)
- [Observability: Business KPIs Metrics](https://www.ranthebuilder.cloud/post/aws-lambda-cookbook-elevate-your-handler-s-code-part-3-business-domain-observability)
- [Environment Variables](https://www.ranthebuilder.cloud/post/aws-lambda-cookbook-environment-variables)
- [Input Validation](https://www.ranthebuilder.cloud/post/aws-lambda-cookbook-elevate-your-handler-s-code-part-5-input-validation)
- [Dynamic Configuration & feature flags](https://www.ranthebuilder.cloud/post/aws-lambda-cookbook-part-6-feature-flags-configuration-best-practices)
- [Start Your AWS Serverless Service With Two Clicks](https://www.ranthebuilder.cloud/post/aws-lambda-cookbook-part-7-how-to-use-the-aws-lambda-cookbook-github-template-project)
- [CDK Best practices](https://github.com/jeastham1993/aws-lambda-dotnet-handler-cookbook)

## Getting started
Head over to the complete project documentation pages at GitHub pages at [https://jeastham1993.github.io/aws-lambda-dotnet-handler-cookbook](https://jeastham1993.github.io/aws-lambda-dotnet-handler-cookbook/)

## Code Contributions
Code contributions are welcomed. Read this [guide.](https://github.com/jeastham1993/aws-lambda-dotnet-handler-cookbook/blob/main/CONTRIBUTING.md)

## Code of Conduct
Read our code of conduct [here.](https://github.com/jeastham1993/aws-lambda-dotnet-handler-cookbook/blob/main/CODE_OF_CONDUCT.md)

## Connect
* Blog Website [James Eastham](https://jameseastham.com)
* LinkedIn: [james-eastham](https://www.linkedin.com/in/james-eastham/)
* Twitter: [@plantpowerjames](https://twitter.com/plantpowerjames)

## License
This library is licensed under the MIT License. See the [LICENSE](https://github.com/jeastham1993/aws-lambda-dotnet-handler-cookbook/blob/main/LICENSE) file.

