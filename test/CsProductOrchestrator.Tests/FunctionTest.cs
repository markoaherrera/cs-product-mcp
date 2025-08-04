using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Net;

namespace CsProductOrchestrator.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestPostRequestWithMissingOpenAiApiKey()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/query",
            Body = JsonSerializer.Serialize(new { query = "Show me black helmets" }) // Missing openAiApiKey
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        // Should return bad request due to missing OpenAI API key
        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(response.Body);
        
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(response.Body);
        Assert.Equal("Invalid request body - query and openAiApiKey are required", errorResponse.GetProperty("error").GetString());
    }

    [Fact]
    public async Task TestPostRequestWithInvalidBody()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/query",
            Body = "invalid json"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        // JSON deserialization error is caught and returns 500 Internal Server Error
        Assert.Equal((int)HttpStatusCode.InternalServerError, response.StatusCode);
        Assert.NotNull(response.Body);
        
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(response.Body);
        Assert.True(errorResponse.TryGetProperty("errror", out _)); // Note: there's a typo in the original code "errror"
    }

    [Fact]
    public async Task TestPostRequestWithEmptyQuery()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/query",
            Body = JsonSerializer.Serialize(new { query = "", openAiApiKey = "test-key" })
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(response.Body);
        
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(response.Body);
        Assert.Equal("Invalid request body - query and openAiApiKey are required", errorResponse.GetProperty("error").GetString());
    }

    [Fact]
    public async Task TestResponseHasCorsHeaders()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/query",
            Body = JsonSerializer.Serialize(new { query = "test query", openAiApiKey = "test-key" })
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.True(response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        Assert.Equal("*", response.Headers["Access-Control-Allow-Origin"]);
        Assert.True(response.Headers.ContainsKey("Content-Type"));
        Assert.Equal("application/json", response.Headers["Content-Type"]);
    }

    [Fact]
    public async Task TestPostRequestWithMissingQuery()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/query",
            Body = JsonSerializer.Serialize(new { openAiApiKey = "test-key" }) // Missing query
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        // Should return bad request due to missing query
        Assert.Equal((int)HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotNull(response.Body);
        
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(response.Body);
        Assert.Equal("Invalid request body - query and openAiApiKey are required", errorResponse.GetProperty("error").GetString());
    }
}
