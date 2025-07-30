using Xunit;
using Amazon.Lambda.Core;
using Amazon.Lambda.TestUtilities;
using Amazon.Lambda.APIGatewayEvents;
using System.Text.Json;
using System.Net;
using System.Globalization;

namespace CsProductApi.Tests;

public class FunctionTest
{
    [Fact]
    public async Task TestGetRequestReturnsCorrectResponseFormat()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json", response.Headers["Content-Type"]);
        Assert.Equal("*", response.Headers["Access-Control-Allow-Origin"]);
        
        // Verify the response body structure
        Assert.NotNull(response.Body);
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;

        // Test top-level structure
        Assert.True(root.TryGetProperty("context", out var contextProp));
        Assert.Equal("product_data", contextProp.GetString());

        Assert.True(root.TryGetProperty("meta", out var metaProp));
        Assert.True(root.TryGetProperty("data", out var dataProp));
        Assert.Equal(JsonValueKind.Array, dataProp.ValueKind);

        // Test meta structure
        Assert.True(metaProp.TryGetProperty("query", out var queryProp));
        Assert.True(metaProp.TryGetProperty("total", out var totalProp));
        Assert.True(metaProp.TryGetProperty("source", out var sourceProp));

        // Test query structure (should all be null for no parameters)
        Assert.True(queryProp.TryGetProperty("color", out var colorProp));
        Assert.True(queryProp.TryGetProperty("category", out var categoryProp));
        Assert.True(queryProp.TryGetProperty("sort", out var sortProp));
        Assert.True(queryProp.TryGetProperty("maxPrice", out var maxPriceProp));
        
        Assert.Equal(JsonValueKind.Null, colorProp.ValueKind);
        Assert.Equal(JsonValueKind.Null, categoryProp.ValueKind);
        Assert.Equal(JsonValueKind.Null, sortProp.ValueKind);
        Assert.Equal(JsonValueKind.Null, maxPriceProp.ValueKind);

        // Test source structure
        Assert.True(sourceProp.TryGetProperty("system", out var systemProp));
        Assert.True(sourceProp.TryGetProperty("function", out var functionProp));
        Assert.True(sourceProp.TryGetProperty("timestamp", out var timestampProp));
        
        Assert.Equal("local-json-file", systemProp.GetString());
        Assert.Equal("CsProductApi", functionProp.GetString());
        Assert.True(DateTime.TryParse(timestampProp.GetString(), out _)); // Validate timestamp format

        // Test total matches data array length
        var dataArray = dataProp.EnumerateArray().ToList();
        Assert.Equal(dataArray.Count, totalProp.GetInt32());

        // Test data item structure (if any data exists)
        if (dataArray.Count > 0)
        {
            var firstItem = dataArray[0];
            Assert.True(firstItem.TryGetProperty("id", out _));
            Assert.True(firstItem.TryGetProperty("name", out _));
            Assert.True(firstItem.TryGetProperty("color", out _));
            Assert.True(firstItem.TryGetProperty("price", out _));
            Assert.True(firstItem.TryGetProperty("category", out _));
        }
    }

    [Fact]
    public async Task TestPostRequestReturnsMethodNotAllowed()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "POST",
            Path = "/api/data"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("application/json", response.Headers["Content-Type"]);
        
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(response.Body);
        Assert.Equal("Only GET method is allowed", errorResponse.GetProperty("error").GetString());
    }

    [Fact]
    public async Task TestPutRequestReturnsMethodNotAllowed()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "PUT",
            Path = "/api/data"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.MethodNotAllowed, response.StatusCode);
        Assert.Equal("application/json", response.Headers["Content-Type"]);
        
        var errorResponse = JsonSerializer.Deserialize<JsonElement>(response.Body);
        Assert.Equal("Only GET method is allowed", errorResponse.GetProperty("error").GetString());
    }

    [Fact]
    public async Task TestResponseHasCorsHeaders()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data"
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
    public async Task TestColorFilterReturnsCorrectMetadata()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "color", "Black" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        // Verify query metadata reflects the filter
        var queryProp = root.GetProperty("meta").GetProperty("query");
        Assert.Equal("Black", queryProp.GetProperty("color").GetString());
        Assert.Equal(JsonValueKind.Null, queryProp.GetProperty("category").ValueKind);
        Assert.Equal(JsonValueKind.Null, queryProp.GetProperty("sort").ValueKind);
        Assert.Equal(JsonValueKind.Null, queryProp.GetProperty("maxPrice").ValueKind);

        // Verify all returned products have the specified color
        var dataArray = root.GetProperty("data").EnumerateArray();
        foreach (var item in dataArray)
        {
            var color = item.GetProperty("color").GetString();
            Assert.Equal("Black", color);
        }
    }

    [Fact]
    public async Task TestMaxPriceFilterReturnsCorrectMetadata()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "maxPrice", "50.00" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        // Verify query metadata reflects the filter
        var queryProp = root.GetProperty("meta").GetProperty("query");
        Assert.Equal("50.00", queryProp.GetProperty("maxPrice").GetString());
        Assert.Equal(JsonValueKind.Null, queryProp.GetProperty("color").ValueKind);

        // Verify all returned products are within the price limit
        var dataArray = root.GetProperty("data").EnumerateArray();
        foreach (var item in dataArray)
        {
            var priceStr = item.GetProperty("price").GetString();
            var price = decimal.Parse(priceStr);
            Assert.True(price <= 50.00m);
        }
    }

    [Fact]
    public async Task TestCategoryFilterReturnsCorrectMetadata()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "category", "Helmets" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        // Verify query metadata reflects the filter
        var queryProp = root.GetProperty("meta").GetProperty("query");
        Assert.Equal("Helmets", queryProp.GetProperty("category").GetString());

        // Verify all returned products have the specified category
        var dataArray = root.GetProperty("data").EnumerateArray();
        foreach (var item in dataArray)
        {
            var category = item.GetProperty("category").GetString();
            Assert.Equal("Helmets", category);
        }
    }

    [Fact]
    public async Task TestSortPriceAscReturnsCorrectMetadata()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "sort", "price-asc" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        // Verify query metadata reflects the sort
        var queryProp = root.GetProperty("meta").GetProperty("query");
        Assert.Equal("price-asc", queryProp.GetProperty("sort").GetString());

        // Verify products are sorted by price ascending
        var dataArray = root.GetProperty("data").EnumerateArray().ToList();
        if (dataArray.Count > 1)
        {
            for (int i = 0; i < dataArray.Count - 1; i++)
            {
                var currentPrice = decimal.Parse(dataArray[i].GetProperty("price").GetString());
                var nextPrice = decimal.Parse(dataArray[i + 1].GetProperty("price").GetString());
                Assert.True(currentPrice <= nextPrice);
            }
        }
    }

    [Fact]
    public async Task TestSortPriceDescReturnsCorrectMetadata()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "sort", "price-desc" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        // Verify query metadata reflects the sort
        var queryProp = root.GetProperty("meta").GetProperty("query");
        Assert.Equal("price-desc", queryProp.GetProperty("sort").GetString());

        // Verify products are sorted by price descending
        var dataArray = root.GetProperty("data").EnumerateArray().ToList();
        if (dataArray.Count > 1)
        {
            for (int i = 0; i < dataArray.Count - 1; i++)
            {
                var currentPrice = decimal.Parse(dataArray[i].GetProperty("price").GetString());
                var nextPrice = decimal.Parse(dataArray[i + 1].GetProperty("price").GetString());
                Assert.True(currentPrice >= nextPrice);
            }
        }
    }

    [Fact]
    public async Task TestMultipleFiltersReturnsCorrectMetadata()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "color", "Black" },
                { "maxPrice", "100" },
                { "sort", "price-asc" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        // Verify all query metadata is correctly set
        var queryProp = root.GetProperty("meta").GetProperty("query");
        Assert.Equal("Black", queryProp.GetProperty("color").GetString());
        Assert.Equal("100", queryProp.GetProperty("maxPrice").GetString());
        Assert.Equal("price-asc", queryProp.GetProperty("sort").GetString());
        Assert.Equal(JsonValueKind.Null, queryProp.GetProperty("category").ValueKind);

        // Verify all returned products match all filters
        var dataArray = root.GetProperty("data").EnumerateArray().ToList();
        foreach (var item in dataArray)
        {
            var color = item.GetProperty("color").GetString();
            var priceStr = item.GetProperty("price").GetString();
            var price = decimal.Parse(priceStr);
            
            Assert.Equal("Black", color);
            Assert.True(price <= 100m);
        }

        // Verify sorting (if multiple items)
        if (dataArray.Count > 1)
        {
            for (int i = 0; i < dataArray.Count - 1; i++)
            {
                var currentPrice = decimal.Parse(dataArray[i].GetProperty("price").GetString());
                var nextPrice = decimal.Parse(dataArray[i + 1].GetProperty("price").GetString());
                Assert.True(currentPrice <= nextPrice);
            }
        }
    }

    [Fact]
    public async Task TestTotalCountMatchesDataArrayLength()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data",
            QueryStringParameters = new Dictionary<string, string>
            {
                { "maxPrice", "25" }
            }
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        var total = root.GetProperty("meta").GetProperty("total").GetInt32();
        var dataArray = root.GetProperty("data").EnumerateArray().ToList();
        
        Assert.Equal(dataArray.Count, total);
        Assert.True(total >= 0); // Should never be negative
    }

    [Fact]
    public async Task TestTimestampIsValidIsoFormat()
    {
        // Arrange
        var function = new Function();
        var context = new TestLambdaContext();
        var request = new APIGatewayProxyRequest
        {
            HttpMethod = "GET",
            Path = "/api/data"
        };

        // Act
        var response = await function.FunctionHandler(request, context);

        // Assert
        Assert.Equal((int)HttpStatusCode.OK, response.StatusCode);
        
        var jsonDocument = JsonDocument.Parse(response.Body);
        var root = jsonDocument.RootElement;
        
        var timestamp = root.GetProperty("meta").GetProperty("source").GetProperty("timestamp").GetString();
        
        // Verify timestamp is in ISO format
        // Use RoundtripKind for identifying the "Z" in the the ISO date string
        Assert.True(DateTime.TryParse(timestamp, null, DateTimeStyles.RoundtripKind, out var parsedTime));
        var now = DateTime.UtcNow;
        var timeDiff = now - parsedTime;
        Assert.True(timeDiff.TotalMinutes < 1); // Should be very recent
        
        // Verify it ends with 'Z' (UTC indicator)
        Assert.True(timestamp.EndsWith("Z"));
    }
}
