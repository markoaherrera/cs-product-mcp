# CsProductApi & CsProductOrchestrator

A .NET 8 AWS Lambda solution with API Gateway that provides:
1. **CsProductApi** - REST API for product data with filtering and sorting
2. **CsProductOrchestrator** - AI-powered product query orchestrator using OpenAI GPT

## Services

### CsProductApi
- **GET /api/data** - Returns product data with optional filtering and sorting
- **Query Parameters:**
  - `sort`: `price-asc` or `price-desc` - Sort by ListPrice
  - `maxPrice`: Numeric value - Filter products with ListPrice ≤ maxPrice
  - `color`: String - Filter products by Color (case-insensitive)
  - `category`: String - Filter products by Category (case-insensitive)

### CsProductOrchestrator
- **POST /api/query** - Natural language product queries powered by OpenAI GPT
- **Request Body:** `{ "query": "Show me black helmets under $50", "openAiApiKey": "your-openai-api-key" }`
- **Features:**
  - Natural language processing for product queries
  - Automatic filter extraction using GPT
  - Integration with CsProductApi for data retrieval
  - Intelligent responses about product data
  - Client-provided OpenAI API key for flexibility

## Response Format

```json
{
  "context": "product_data",
  "meta": {
    "query": {
      "color": "Black",
      "category": null,
      "sort": "price-asc",
      "maxPrice": "100"
    },
    "total": 15,
    "source": {
      "system": "local-json-file",
      "function": "GetProducts",
      "timestamp": "2025-01-28T10:32:00Z"
    }
  },
  "data": [
    {
      "id": 712,
      "name": "AWC Logo Cap",
      "color": "Black",
      "price": "8.99",
      "category": "Caps"
    }
  ]
}
```

## Prerequisites

- .NET 8 SDK
- AWS CLI configured with appropriate credentials
- AWS CDK CLI (`npm install -g aws-cdk`)
- Amazon.Lambda.Tools (`dotnet tool install -g Amazon.Lambda.Tools`)
- OpenAI API Key (provided in request body for CsProductOrchestrator)

## Local Development

### Environment Setup

1. **CsProductOrchestrator now accepts OpenAI API key in request body** - no environment setup needed for the API key
2. **Only PRODUCT_API_URL environment variable is required** - this will be set automatically after infrastructure deployment

### Run locally for testing:

**CsProductApi:**
```cmd
cd src\CsProductApi
dotnet run
```
Test endpoints:
- `http://localhost:5000/api/data`
- `http://localhost:5000/api/data?color=Black&maxPrice=100&sort=price-asc`
- `http://localhost:5000/health`

**CsProductOrchestrator:**
```cmd
cd src\CsProductOrchestrator
# Set PRODUCT_API_URL=http://localhost:5000/api/data in .env file
dotnet run
```
Test endpoints:
- `http://localhost:8000/api/orchestrator` (POST with JSON body including openAiApiKey)
- `http://localhost:8000/health`

### Run tests:

```cmd
dotnet test
```

## Deployment

### Option 1: Deploy Everything (Recommended)

```powershell
.\deploy.ps1 -DeployAll
```

### Option 2: Deploy Components Separately

**Deploy Product API Lambda only:**
```powershell
.\deploy.ps1 -DeployLambda
```

**Deploy Orchestrator Lambda only:**
```powershell
.\deploy.ps1 -DeployOrchestrator
```

**Deploy both Lambda functions:**
```powershell
.\deploy.ps1 -DeployLambda -DeployOrchestrator
```

**Deploy Infrastructure (API Gateways) only:**
```powershell
.\deploy.ps1 -DeployInfrastructure
```

### Manual Deployment Steps

**1. Deploy Lambda Functions:**
```cmd
# Deploy Product API
cd src\CsProductApi
dotnet lambda deploy-function CsProductApi

# Deploy Orchestrator (make sure to set environment variables first)
cd ..\CsProductOrchestrator
dotnet lambda deploy-function CsProductOrchestrator
```

**2. Deploy Infrastructure:**
```cmd
cd ..\..\infrastructure\CsProductApi.Infrastructure
cdk bootstrap  # Only needed once per account/region
cdk deploy
```

**3. Configure Environment Variables:**
After infrastructure deployment, update the CsProductOrchestrator Lambda function environment variable:
- `PRODUCT_API_URL`: Use the ProductApiUrlForOrchestrator output from CDK
- Note: OpenAI API key is now provided in the request body, not as an environment variable

## Architecture

- **AWS Lambda Functions**: 
  - CsProductApi: .NET 8 function for product data API
  - CsProductOrchestrator: .NET 8 function for AI-powered queries
- **API Gateways**: Two REST APIs with CORS enabled
- **Data Source**: Local JSON file with product data
- **AI Integration**: OpenAI GPT for natural language processing
- **Infrastructure**: AWS CDK for Infrastructure as Code

## Project Structure

```
├── src/
│   ├── CsProductApi/           # Product API Lambda function
│   ├── CsProductOrchestrator/  # AI Orchestrator Lambda function
│   └── ProductAgentModels/     # Shared models
├── test/
│   ├── CsProductApi.Tests/     # Product API unit tests
│   └── CsProductOrchestrator.Tests/  # Orchestrator unit tests
├── infrastructure/
│   └── CsProductApi.Infrastructure/  # CDK infrastructure code
├── deploy.ps1                 # Deployment script
└── README.md
```

## API Examples

### CsProductApi Examples

**Get all products:**
```bash
curl "https://your-product-api-id.execute-api.region.amazonaws.com/prod/api/data"
```

**Filter by color and sort by price:**
```bash
curl "https://your-product-api-id.execute-api.region.amazonaws.com/prod/api/data?color=Black&sort=price-asc"
```

**Filter by category and max price:**
```bash
curl "https://your-product-api-id.execute-api.region.amazonaws.com/prod/api/data?category=Road%20Bikes&maxPrice=1000"
```

### CsProductOrchestrator Examples

**Natural language product query:**
```bash
curl -X POST "https://your-orchestrator-api-id.execute-api.region.amazonaws.com/prod/api/query" \
  -H "Content-Type: application/json" \
  -d '{"query": "Show me black helmets under $50", "openAiApiKey": "your-openai-api-key"}'
```

**Complex product inquiry:**
```bash
curl -X POST "https://your-orchestrator-api-id.execute-api.region.amazonaws.com/prod/api/query" \
  -H "Content-Type: application/json" \
  -d '{"query": "What are the most expensive road bikes available?", "openAiApiKey": "your-openai-api-key"}'
```

**Product comparison:**
```bash
curl -X POST "https://your-orchestrator-api-id.execute-api.region.amazonaws.com/prod/api/query" \
  -H "Content-Type: application/json" \
  -d '{"query": "Compare mountain bikes and road bikes in terms of price", "openAiApiKey": "your-openai-api-key"}'
```

## Development Notes

- The Lambda function reads product data from `data.json` included in the deployment package
- CORS is enabled for all origins in development
- All query parameters are optional and can be combined
- Invalid sort parameters are ignored (no sorting applied)
- Case-insensitive filtering for color and category