using System.Collections.Generic;

namespace ProductAgentModels;
public class JsonSchemaResponseFormat
{
    public string Type { get; set; } = "json_schema";
    public JsonSchemaDefinition Json_schema { get; set; }
}

public class JsonSchemaDefinition
{
    public string Name { get; set; }
    public SchemaObject Schema { get; set; }
    public bool Strict { get; set; } = true;
}

public class SchemaObject
{
    public string Type { get; set; }
    public Dictionary<string, SchemaProperty> Properties { get; set; }
    public List<string> Required { get; set; }
    public bool AdditionalProperties { get; set; }
}

public class SchemaProperty
{
    public string Type { get; set; }
    public List<string> Enum { get; set; }
    public double? Minimum { get; set; }
    public string Description { get; set; }
}

// Example usage for your bike filter schema
public class BikeFilterSchemaExample
{
    public static JsonSchemaResponseFormat GetBikeFilterResponseFormat()
    {
        return new JsonSchemaResponseFormat
        {
            Type = "json_schema",
            Json_schema = new JsonSchemaDefinition
            {
                Name = "bike_product_filter",
                Schema = new SchemaObject
                {
                    Type = "object",
                    Properties = new Dictionary<string, SchemaProperty>
                    {
                        ["maxPrice"] = new SchemaProperty
                        {
                            Type = "number",
                            Minimum = 0,
                            Description = "Maximum price filter for products"
                        },
                        ["color"] = new SchemaProperty
                        {
                            Type = "string",
                            Description = "Color filter for products"
                        },
                        ["sort"] = new SchemaProperty
                        {
                            Type = "string",
                            Enum = new List<string> { "price-asc", "price-desc" },
                            Description = "Sort order for products by price"
                        },
                        ["category"] = new SchemaProperty
                        {
                            Type = "string",
                            Enum = new List<string>
                            {
                                "Road Frames", "Mountain Frames", "Road Bikes", "Mountain Bikes",
                                "Helmets", "Socks", "Caps", "Jerseys", "Forks", "Head sets",
                                "Handle bars", "Wheels", "Shorts", "Tights", "Bib-Shorts",
                                "Gloves", "Vests", "Panniers", "Locks", "Pumps", "Lights",
                                "Bottlesand Cages", "Tiresand Tubes", "Bike Racks", "Cleaners",
                                "Fenders", "Bike Stands", "Hydration Packs", "Touring Frames",
                                "Derailleurs", "Brakes", "Saddles", "Pedals", "Cranksets",
                                "Chains", "Touring Bikes", "Bottom Brackets"
                            },
                            Description = "Product category filter"
                        }
                    },
                    Required = new List<string>(), // Empty list - all fields optional
                    AdditionalProperties = false
                },
                Strict = true
            }
        };
    }

    // Alternative: Using object initializer syntax inline
    public static readonly JsonSchemaResponseFormat BikeFilterSchema = new JsonSchemaResponseFormat
    {
        Type = "json_schema",
        Json_schema = new JsonSchemaDefinition
        {
            Name = "bike_product_filter",
            Strict = true,
            Schema = new SchemaObject
            {
                Type = "object",
                AdditionalProperties = false,
                Required = new List<string>(),
                Properties = new Dictionary<string, SchemaProperty>
                {
                    ["maxPrice"] = new SchemaProperty { Type = "number", Minimum = 0 },
                    ["color"] = new SchemaProperty { Type = "string" },
                    ["sort"] = new SchemaProperty
                    {
                        Type = "string",
                        Enum = new List<string> { "price-asc", "price-desc" }
                    },
                    ["category"] = new SchemaProperty
                    {
                        Type = "string",
                        Enum = new List<string>
                        {
                            "Road Frames", "Mountain Frames", "Road Bikes", "Mountain Bikes",
                            "Helmets", "Socks", "Caps", "Jerseys", "Forks", "Head sets",
                            "Handle bars", "Wheels", "Shorts", "Tights", "Bib-Shorts",
                            "Gloves", "Vests", "Panniers", "Locks", "Pumps", "Lights",
                            "Bottlesand Cages", "Tiresand Tubes", "Bike Racks", "Cleaners",
                            "Fenders", "Bike Stands", "Hydration Packs", "Touring Frames",
                            "Derailleurs", "Brakes", "Saddles", "Pedals", "Cranksets",
                            "Chains", "Touring Bikes", "Bottom Brackets"
                        }
                    }
                }
            }
        }
    };

    // Usage example
    public static void ExampleUsage()
    {
        var responseFormat = GetBikeFilterResponseFormat();

        // Or use the static readonly version
        var staticResponseFormat = BikeFilterSchema;

        // You can now serialize this to JSON if needed
        // var json = JsonSerializer.Serialize(responseFormat);
        // or with Newtonsoft.Json:
        // var json = JsonConvert.SerializeObject(responseFormat);
    }
}