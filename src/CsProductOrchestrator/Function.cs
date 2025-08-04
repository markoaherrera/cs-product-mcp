using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Net;
using ProductAgentModels;
using DotNetEnv;
using System.Net.Http.Json;
using System.Linq.Expressions;

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

        var filterSchema = @"{
  ""$schema"": ""https://json-schema.org/draft/2020-12/schema"",
  ""$id"": ""https://example.com/product-filter.schema.json"",
  ""title"": ""Product Filter"",
  ""description"": ""Schema for filtering products with price, color, sort, and category options"",
  ""type"": ""object"",
  ""properties"": {
    ""maxPrice"": {
      ""type"": ""number"",
      ""minimum"": 0,
      ""description"": ""Maximum price filter for products""
    },
    ""color"": {
      ""type"": ""string"",
      ""description"": ""Color filter for products""
    },
    ""sort"": {
      ""type"": ""string"",
      ""enum"": [
        ""price-asc"",
        ""price-desc""
      ],
      ""description"": ""Sort order for products by price""
    },
    ""category"": {
      ""type"": ""string"",
      ""enum"": [
        ""Road Frames"",
        ""Mountain Frames"",
        ""Road Bikes"",
        ""Mountain Bikes"",
        ""Helmets"",
        ""Socks"",
        ""Caps"",
        ""Jerseys"",
        ""Forks"",
        ""Head sets"",
        ""Handle bars"",
        ""Wheels"",
        ""Shorts"",
        ""Tights"",
        ""Bib-Shorts"",
        ""Gloves"",
        ""Vests"",
        ""Panniers"",
        ""Locks"",
        ""Pumps"",
        ""Lights"",
        ""Bottlesand Cages"",
        ""Tiresand Tubes"",
        ""Bike Racks"",
        ""Cleaners"",
        ""Fenders"",
        ""Bike Stands"",
        ""Hydration Packs"",
        ""Touring Frames"",
        ""Derailleurs"",
        ""Brakes"",
        ""Saddles"",
        ""Pedals"",
        ""Cranksets"",
        ""Chains"",
        ""Touring Bikes"",
        ""Bottom Brackets""
      ],
      ""description"": ""Product category filter""
    }
  },
  ""required"": [],
  ""additionalProperties"": false
}";

        try
        {
            Env.Load(); // TODO: consider using a more secure way to manage environment variables

            context.Logger.LogInformation($"Processing {request.HttpMethod} request for {request.Path}");

            var userQuery = JsonSerializer.Deserialize<UserQuery>(request.Body);
            var lambdaUrl = Environment.GetEnvironmentVariable("PRODUCT_API_URL");

            if (userQuery == null || string.IsNullOrEmpty(userQuery.Query) || string.IsNullOrEmpty(userQuery.OpenAiApiKey))
            {
                context.Logger.LogError("Invalid request body - missing query or OpenAI API key");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.BadRequest,
                    Body = JsonSerializer.Serialize(new { error = "Invalid request body - query and openAiApiKey are required" }),
                    Headers = responseHeaders
                };
            }

            if (string.IsNullOrEmpty(lambdaUrl))
            {
                context.Logger.LogError("Product API URL is not set");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.InternalServerError,
                    Body = JsonSerializer.Serialize(new { error = "Product API URL is not set" }),
                    Headers = responseHeaders
                };
            }

            http.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", userQuery.OpenAiApiKey);

            // Ask GPT: "What filters do I need for this query?"
            // Compose the GPT filter extraction request with clear structure and formatting
            var gptRequestFilter = new
            {
                model = "gpt-4o-2024-08-06",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are an API orchestrator. Extract relevant query parameters from the user's request to filter product data."
                    },
                    new
                    {
                        role = "user",
                        content = userQuery.Query
                    },
                    // new
                    // {
                    //     role = "system",
                    //     content =
                    //         "Only return a JSON object with fields: maxPrice, color, sort (possible values: 'price-desc' or 'price-asc'), " +
                    //         "category (possible values: Road Frames, Mountain Frames, Road Bikes, Mountain Bikes, Helmets, Socks, Caps, Jerseys, " +
                    //         "Forks, Head sets, Handle bars, Wheels, Shorts, Tights, Bib-Shorts, Gloves, Vests, Panniers, Locks, Pumps, Lights, " +
                    //         "Bottlesand Cages, Tiresand Tubes, Bike Racks, Cleaners, Fenders, Bike Stands, Hydration Packs, Touring Frames, Derailleurs, " +
                    //         "Brakes, Saddles, Pedals, Cranksets, Chains, Touring Bikes, Bottom Brackets)."
                    // }
                    new
                    {
                        role = "system",
                        content = $"Only and only return a JSON object without using markdown formatters just the text containing a JSON document, using the following JSON schema as a guide:\n{filterSchema}"
                    },
                }
            };

            var gptResponseFilter = await http.PostAsJsonAsync("https://api.openai.com/v1/chat/completions", gptRequestFilter);

            if (!gptResponseFilter.IsSuccessStatusCode)
            {
                context.Logger.LogError("Failed to extract filters from GPT response");
                context.Logger.Log($"Gpt response: {gptResponseFilter.StatusCode} - {await gptResponseFilter.Content.ReadAsStringAsync()}");
            }
            else
            {
                var gptResultFilter = await gptResponseFilter.Content.ReadFromJsonAsync<GptResponse>();
                var filterContent = gptResultFilter?.Choices?.FirstOrDefault()?.Message?.Content;

                if (string.IsNullOrWhiteSpace(filterContent))
                {
                    context.Logger.LogError("No response from GPT for filter extraction");
                }
                else
                {
                    context.Logger.LogInformation($"Extracted filter content: {filterContent}");
                    try
                    {
                        if (JsonSerializer.Deserialize<Dictionary<string, string>>(filterContent) is { } extractedFilter)
                        {
                            // Use TryGetValue for safety and build query string only for present keys
                            var queryParams = new List<string>();
                            if (extractedFilter.TryGetValue("color", out var color) && !string.IsNullOrEmpty(color))
                                queryParams.Add($"color={WebUtility.UrlEncode(color)}");
                            if (extractedFilter.TryGetValue("category", out var category) && !string.IsNullOrEmpty(category))
                                queryParams.Add($"category={WebUtility.UrlEncode(category)}");
                            if (extractedFilter.TryGetValue("sort", out var sort) && !string.IsNullOrEmpty(sort))
                                queryParams.Add($"sort={WebUtility.UrlEncode(sort)}");
                            if (extractedFilter.TryGetValue("maxPrice", out var maxPrice) && !string.IsNullOrEmpty(maxPrice))
                                queryParams.Add($"maxPrice={WebUtility.UrlEncode(maxPrice)}");

                            if (queryParams.Count > 0)
                                lambdaUrl += "?" + string.Join("&", queryParams);
                        }
                        else
                        {
                            context.Logger.LogError("Failed to deserialize filter content from GPT response");
                        }
                    }
                    catch (JsonException jsonEx)
                    {
                        context.Logger.LogError($"JSON deserialization error: {jsonEx.Message}");
                    }
                }
            }

            context.Logger.LogInformation($"Fetching product data from {lambdaUrl}");
            var productData = await http.GetFromJsonAsync<McpStructure>(lambdaUrl);
            context.Logger.LogInformation($"Product API response Metadata: {JsonSerializer.Serialize(productData?.Meta)}");

            if (productData == null)
            {
                context.Logger.LogError("Failed to fetch product data or data is null");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = JsonSerializer.Serialize(new { error = "Product data not found" }),
                    Headers = responseHeaders
                };
            }

            // Ask GPT about the product data
            var gptRequest = new
            {
                model = "gpt-4.1",
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "You are a helpful store assistant answering questions about products from the 'Adventure Works' store."
                    },
                    new
                    {
                        role = "user",
                        content = userQuery.Query
                    },
                    new
                    {
                        role = "system",
                        content = $"This is the store's product data: {JsonSerializer.Serialize(productData)}"
                    }
                },
            };


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
