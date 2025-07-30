using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.APIGateway;
using Constructs;

namespace CsProductApi.Infrastructure;

public class CsProductApiStack : Stack
{
    internal CsProductApiStack(Construct scope, string id, IStackProps props = null) : base(scope, id, props)
    {
        // Reference the existing Lambda function by name
        var existingLambda = Function.FromFunctionName(this, "ExistingLambda", "CsProductApi");

        // Create API Gateway
        var api = new RestApi(this, "CsProductApiGateway", new RestApiProps
        {
            RestApiName = "CsProductApi",
            Description = "API Gateway for CsProductApi Lambda function",
            DefaultCorsPreflightOptions = new CorsOptions
            {
                AllowOrigins = Cors.ALL_ORIGINS,
                AllowMethods = Cors.ALL_METHODS,
                AllowHeaders = new[] { "Content-Type", "X-Amz-Date", "Authorization", "X-Api-Key", "X-Amz-Security-Token" }
            }
        });

        // Create Lambda integration
        var lambdaIntegration = new LambdaIntegration(existingLambda, new LambdaIntegrationOptions
        {
            RequestTemplates = new Dictionary<string, string>
            {
                ["application/json"] = "{ \"statusCode\": \"200\" }"
            }
        });

        // Add /api/data resource and GET method
        var apiResource = api.Root.AddResource("api");
        var dataResource = apiResource.AddResource("data");
        dataResource.AddMethod("GET", lambdaIntegration);

        // Output the API Gateway URL
        new CfnOutput(this, "ApiGatewayUrl", new CfnOutputProps
        {
            Value = api.Url,
            Description = "API Gateway endpoint URL"
        });

        // Output the full endpoint URL
        new CfnOutput(this, "ProductsEndpoint", new CfnOutputProps
        {
            Value = $"{api.Url}api/data",
            Description = "Products API endpoint"
        });
    }
}