using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;


namespace CsProductOrchestrator;

public class LocalTestServer
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Add CORS
        builder.Services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.AllowAnyOrigin()
                      .AllowAnyMethod()
                      .AllowAnyHeader();
            });
        });

        var app = builder.Build();
        app.UseCors();

        // Create Lambda function instance
        var function = new Function();
        var context = new TestLambdaContext();

        // Map http methodds
        app.MapMethods("/api/orchestrator", new[] { "GET",  "POST", "PUT", "DELETE" }, async (HttpContext httpContext) =>
        {
            // Convert HttpContext to APIGatewayProxyRequest
            var request = new APIGatewayProxyRequest
            {
                HttpMethod = httpContext.Request.Method,
                Path = httpContext.Request.Path,
                QueryStringParameters = httpContext.Request.Query.ToDictionary(q => q.Key, q => q.Value.ToString()),
                Headers = httpContext.Request.Headers.ToDictionary(h => h.Key, h => h.Value.ToString())
            };

            // Read body if present
            if (httpContext.Request.ContentLength > 0)
            {
                using var reader = new StreamReader(httpContext.Request.Body);
                request.Body = await reader.ReadToEndAsync();
            }

            // Call Lambda function
            var response = await function.FunctionHandler(request, context);
            // Convert APIGatewayProxyResponse back to HttpResponse
            httpContext.Response.StatusCode = response.StatusCode;

            if (response.Headers != null)
            {
                foreach (var header in response.Headers)
                {
                    httpContext.Response.Headers[header.Key] = header.Value;
                }
            }

            await httpContext.Response.WriteAsync(response.Body ?? string.Empty);
        });

        app.MapGet("/health", () => Results.Ok("Healthy"));

        Console.WriteLine("Starting local test Lambda function...");
        Console.WriteLine("Test server is running at http://localhost:8000/api/orchestrator");
        Console.WriteLine("Health check endpoint: http://localhost:8000/health");
        Console.WriteLine("Press Ctrl+C to stop the server.");

        app.Run("http://localhost:8000");
    }
}

