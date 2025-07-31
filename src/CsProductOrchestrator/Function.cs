using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Net;
using ProductAgentModels;
using DotNetEnv;
using System.Net.Http.Json;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CsProductOrchestrator;

public class Function
{
    private static HttpClient http = new();

    /// <summary>
    /// A simple function that takes a string and does a ToUpper
    /// </summary>
    /// <param name="request">The API Gateway request</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>API Gateway response with JSON file contents</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        var responseHeaders = new Dictionary<string, string>
        {
            { "Content-Type", "application/json" },
            { "Access-Control-Allow-Origin", "*" }
        };

        try
        {
            Env.Load(); // TODO: consider using a more secure way to manage environment variables

            context.Logger.LogInformation($"Processing {request.HttpMethod} request for {request.Path}");

            var userQuery = JsonSerializer.Deserialize<UserQuery>(request.Body);

            if (userQuery == null)
            {
                context.Logger.LogError("Invalid request body");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = JsonSerializer.Serialize(new { error = "Invalid request body" }),
                    Headers = responseHeaders
                };
            }

            var lambdaUrl = Environment.GetEnvironmentVariable("PRODUCT_API_URL");
            var productData = await http.GetFromJsonAsync<McpStructure>(lambdaUrl);

            var gptRequest = new
            {
                model = "gpt-4o",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a helpful assistant answering questions about product data."
                    },
                    new
                    {
                        role = "user",
                        content = userQuery.Query
                    },
                    new
                    {
                        role = "system",
                        content = $"Here is the product data: {JsonSerializer.Serialize(productData)}"
                    }
                },
            };

            var openAiApiKey = Environment.GetEnvironmentVariable("OPEN_AI_API_KEY");
            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", openAiApiKey);

            var gptResponse = await http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", gptRequest);
            if (!gptResponse.IsSuccessStatusCode)
            {
                context.Logger.LogError($"OpenAI API request failed with status code {gptResponse.StatusCode}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = JsonSerializer.Serialize(new { error = "Failed to process request" }),
                    Headers = responseHeaders
                };
            }

            var gptResult = await gptResponse.Content.ReadFromJsonAsync<GptResponse>();

            var reply = gptResult?.Choices?.FirstOrDefault()?.Message?.Content ?? "No response from GPT";

            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = JsonSerializer.Serialize(new { answer = reply}),
                Headers = responseHeaders,
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Errro processing request {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = JsonSerializer.Serialize(new { errror = "Internal server error" }),
                Headers = responseHeaders
            };
        }
    }
}
