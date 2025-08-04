namespace ProductAgentModels;

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

public class McpStructure
{
    public string Context { get; set; } = "product_data";
    public Meta Meta { get; set; } = new();
    public List<ProductResponse> Data { get; set; } = new();
}