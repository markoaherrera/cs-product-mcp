using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Net;

// Assembly attribute to enable the Lambda function's JSON input to be converted into a .NET class.
[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace CsProductApi;

public class Product
{
    public int ProductID { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public decimal ListPrice { get; set; }
    public string? Size { get; set; }
    public decimal? Weight { get; set; }
    public string Category { get; set; } = string.Empty;
}

public class ProductResponse
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Color { get; set; }
    public string Price { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
}

public class QueryMeta
{
    public string? Color { get; set; }
    public string? Category { get; set; }
    public string? Sort { get; set; }
    public string? MaxPrice { get; set; }
}

public class SourceMeta
{
    public string System { get; set; } = "local-json-file";
    public string Function { get; set; } = "CsProductApi";
    public string Timestamp { get; set; } = string.Empty;
}

public class Meta
{
    public QueryMeta Query { get; set; } = new();
    public int Total { get; set; }
    public SourceMeta Source { get; set; } = new();
}

public class ApiResponse
{
    public string Context { get; set; } = "product_data";
    public Meta Meta { get; set; } = new();
    public List<ProductResponse> Data { get; set; } = new();
}

public class Function
{
    private const string JSON_FILE_PATH = "data.json";
    
    /// <summary>
    /// Lambda function handler for API Gateway requests
    /// </summary>
    /// <param name="request">The API Gateway request</param>
    /// <param name="context">The ILambdaContext that provides methods for logging and describing the Lambda environment.</param>
    /// <returns>API Gateway response with JSON file contents</returns>
    public async Task<APIGatewayProxyResponse> FunctionHandler(APIGatewayProxyRequest request, ILambdaContext context)
    {
        try
        {
            context.Logger.LogInformation($"Processing {request.HttpMethod} request for {request.Path}");
            
            // Only handle GET requests
            if (request.HttpMethod != "GET")
            {
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.MethodNotAllowed,
                    Body = JsonSerializer.Serialize(new { error = "Only GET method is allowed" }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }

            // Check if JSON file exists
            if (!File.Exists(JSON_FILE_PATH))
            {
                context.Logger.LogError($"JSON file not found at path: {JSON_FILE_PATH}");
                return new APIGatewayProxyResponse
                {
                    StatusCode = (int)HttpStatusCode.NotFound,
                    Body = JsonSerializer.Serialize(new { error = "Data file not found" }),
                    Headers = new Dictionary<string, string>
                    {
                        { "Content-Type", "application/json" },
                        { "Access-Control-Allow-Origin", "*" }
                    }
                };
            }

            // Read and parse JSON file contents
            string jsonContent = await File.ReadAllTextAsync(JSON_FILE_PATH);
            var products = JsonSerializer.Deserialize<Product[]>(jsonContent);
            
            if (products == null)
            {
                throw new JsonException("Failed to deserialize products data");
            }

            // Apply filters and sorting based on query parameters
            var filteredProducts = ApplyFiltersAndSorting(products, request.QueryStringParameters, context).ToList();
            
            // Build the API response in the specified format
            var apiResponse = BuildApiResponse(filteredProducts, request.QueryStringParameters);
            
            // Serialize the response
            var responseBody = JsonSerializer.Serialize(apiResponse, new JsonSerializerOptions 
            { 
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase 
            });
            
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.OK,
                Body = responseBody,
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }
        catch (JsonException ex)
        {
            context.Logger.LogError($"Invalid JSON format: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = JsonSerializer.Serialize(new { error = "Invalid JSON format in data file" }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }
        catch (Exception ex)
        {
            context.Logger.LogError($"Error processing request: {ex.Message}");
            return new APIGatewayProxyResponse
            {
                StatusCode = (int)HttpStatusCode.InternalServerError,
                Body = JsonSerializer.Serialize(new { error = "Internal server error" }),
                Headers = new Dictionary<string, string>
                {
                    { "Content-Type", "application/json" },
                    { "Access-Control-Allow-Origin", "*" }
                }
            };
        }
    }

    /// <summary>
    /// Apply filters and sorting to the products based on query parameters
    /// </summary>
    /// <param name="products">Array of products to filter</param>
    /// <param name="queryParams">Query string parameters from the request</param>
    /// <param name="context">Lambda context for logging</param>
    /// <returns>Filtered and sorted products</returns>
    private IEnumerable<Product> ApplyFiltersAndSorting(Product[] products, IDictionary<string, string>? queryParams, ILambdaContext context)
    {
        var result = products.AsEnumerable();

        if (queryParams == null)
        {
            return result;
        }

        // Apply maxPrice filter
        if (queryParams.TryGetValue("maxPrice", out var maxPriceStr) && 
            decimal.TryParse(maxPriceStr, out var maxPrice))
        {
            context.Logger.LogInformation($"Applying maxPrice filter: {maxPrice}");
            result = result.Where(p => p.ListPrice <= maxPrice);
        }

        // Apply color filter
        if (queryParams.TryGetValue("color", out var color) && !string.IsNullOrWhiteSpace(color))
        {
            context.Logger.LogInformation($"Applying color filter: {color}");
            result = result.Where(p => string.Equals(p.Color, color, StringComparison.OrdinalIgnoreCase));
        }

        // Apply category filter
        if (queryParams.TryGetValue("category", out var category) && !string.IsNullOrWhiteSpace(category))
        {
            context.Logger.LogInformation($"Applying category filter: {category}");
            result = result.Where(p => string.Equals(p.Category, category, StringComparison.OrdinalIgnoreCase));
        }

        // Apply sorting
        if (queryParams.TryGetValue("sort", out var sortParam) && !string.IsNullOrWhiteSpace(sortParam))
        {
            context.Logger.LogInformation($"Applying sort: {sortParam}");
            result = sortParam.ToLowerInvariant() switch
            {
                "price-asc" => result.OrderBy(p => p.ListPrice),
                "price-desc" => result.OrderByDescending(p => p.ListPrice),
                _ => result // Invalid sort parameter, return unsorted
            };
        }

        return result;
    }

    /// <summary>
    /// Build the API response in the specified format
    /// </summary>
    /// <param name="products">Filtered products</param>
    /// <param name="queryParams">Query string parameters from the request</param>
    /// <returns>Formatted API response</returns>
    private ApiResponse BuildApiResponse(List<Product> products, IDictionary<string, string>? queryParams)
    {
        // Extract query parameters with null defaults
        string? color = null;
        string? category = null;
        string? sort = null;
        string? maxPrice = null;

        if (queryParams != null)
        {
            queryParams.TryGetValue("color", out color);
            queryParams.TryGetValue("category", out category);
            queryParams.TryGetValue("sort", out sort);
            queryParams.TryGetValue("maxPrice", out maxPrice);
        }

        // Convert products to response format
        var productResponses = products.Select(p => new ProductResponse
        {
            Id = p.ProductID,
            Name = p.Name,
            Color = p.Color,
            Price = p.ListPrice.ToString("F2"),
            Category = p.Category
        }).ToList();

        // Build the complete response
        return new ApiResponse
        {
            Context = "product_data",
            Meta = new Meta
            {
                Query = new QueryMeta
                {
                    Color = string.IsNullOrWhiteSpace(color) ? null : color,
                    Category = string.IsNullOrWhiteSpace(category) ? null : category,
                    Sort = string.IsNullOrWhiteSpace(sort) ? null : sort,
                    MaxPrice = string.IsNullOrWhiteSpace(maxPrice) ? null : maxPrice
                },
                Total = productResponses.Count,
                Source = new SourceMeta
                {
                    System = "local-json-file",
                    Function = "CsProductApi",
                    Timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ")
                }
            },
            Data = productResponses
        };
    }
}
