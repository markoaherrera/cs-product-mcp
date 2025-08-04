using Amazon.CDK;
using Amazon.CDK.AWS.Lambda;
using Amazon.CDK.AWS.APIGateway;
using Constructs;

namespace CsProductApi.Infrastructure;

public class CsProductApiStack : Stack
{
    internal CsProductApiStack(Construct scope, string id, IStackProps? props = null) : base(scope, id, props)
    {
        // Reference the existing Lambda functions by name
        var productApiLambda = Function.FromFunctionName(this, "ProductApiLambda", "CsProductApi");
        var orchestratorLambda = Function.FromFunctionName(this, "OrchestratorLambda", "CsProductOrchestrator");

        // Note: Environment variables for existing Lambda functions should be set manually
        // or through the setup-environment.ps1 script after deployment

        // Create API Gateway for Product API
        var productApi = new RestApi(this, "CsProductApiGateway", new RestApiProps
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

        // Create API Gateway for Orchestrator
        var orchestratorApi = new RestApi(this, "CsProductOrchestratorGateway", new RestApiProps
        {
            RestApiName = "CsProductOrchestrator",
            Description = "API Gateway for CsProductOrchestrator Lambda function",
            DefaultCorsPreflightOptions = new CorsOptions
            {
                AllowOrigins = Cors.ALL_ORIGINS,
                AllowMethods = Cors.ALL_METHODS,
                AllowHeaders = new[] { "Content-Type", "X-Amz-Date", "Authorization", "X-Api-Key", "X-Amz-Security-Token" }
            }
        });

        // Create Lambda integrations
        var productLambdaIntegration = new LambdaIntegration(productApiLambda, new LambdaIntegrationOptions
        {
            RequestTemplates = new Dictionary<string, string>
            {
                ["application/json"] = "{ \"statusCode\": \"200\" }"
            }
        });

        var orchestratorLambdaIntegration = new LambdaIntegration(orchestratorLambda, new LambdaIntegrationOptions
        {
            RequestTemplates = new Dictionary<string, string>
            {
                ["application/json"] = "{ \"statusCode\": \"200\" }"
            }
        });

        // Add Product API resources and methods
        var productApiResource = productApi.Root.AddResource("api");
        var dataResource = productApiResource.AddResource("data");
        dataResource.AddMethod("GET", productLambdaIntegration);

        // Add Orchestrator API resources and methods
        var orchestratorApiResource = orchestratorApi.Root.AddResource("api");
        var queryResource = orchestratorApiResource.AddResource("query");
        queryResource.AddMethod("POST", orchestratorLambdaIntegration);

        // Output the API Gateway URLs
        new CfnOutput(this, "ProductApiGatewayUrl", new CfnOutputProps
        {
            Value = productApi.Url,
            Description = "Product API Gateway endpoint URL"
        });

        new CfnOutput(this, "OrchestratorApiGatewayUrl", new CfnOutputProps
        {
            Value = orchestratorApi.Url,
            Description = "Orchestrator API Gateway endpoint URL"
        });

        // Output the full endpoint URLs
        new CfnOutput(this, "ProductsEndpoint", new CfnOutputProps
        {
            Value = $"{productApi.Url}api/data",
            Description = "Products API endpoint"
        });

        new CfnOutput(this, "OrchestratorEndpoint", new CfnOutputProps
        {
            Value = $"{orchestratorApi.Url}api/query",
            Description = "Orchestrator API endpoint"
        });

        // Output environment variable for orchestrator to use
        new CfnOutput(this, "ProductApiUrlForOrchestrator", new CfnOutputProps
        {
            Value = $"{productApi.Url}api/data",
            Description = "Product API URL to be used as PRODUCT_API_URL environment variable in orchestrator"
        });
    }
}