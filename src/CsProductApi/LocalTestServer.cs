using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.TestUtilities;

namespace CsProductApi;

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

        // Map all HTTP methods to Lambda function
        app.MapMethods("/api/data", new[] { "GET", "POST", "PUT", "DELETE" }, async (HttpContext httpContext) =>
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

            await httpContext.Response.WriteAsync(response.Body ?? "");
        });

        // Health check endpoint
        app.MapGet("/health", () => "Lambda function is running locally!");

        Console.WriteLine("Lambda function running locally!");
        Console.WriteLine("Test endpoint: http://localhost:5000/api/data");
        Console.WriteLine("Health check: http://localhost:5000/health");
        Console.WriteLine("Press Ctrl+C to stop");

        app.Run();
    }
}