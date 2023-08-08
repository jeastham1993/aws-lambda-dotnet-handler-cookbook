using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.SystemTextJson;
using StockTrader.Infrastructure;


[assembly: LambdaSerializer(typeof(SourceGeneratorLambdaJsonSerializer<CustomSerializationContext>))]