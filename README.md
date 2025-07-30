# CsProductApi

A .NET 8 AWS Lambda function with API Gateway that provides a REST API for product data with filtering and sorting capabilities.

## Features

- **GET /api/data** - Returns product data with optional filtering and sorting
- **Query Parameters:**
  - `sort`: `price-asc` or `price-desc` - Sort by ListPrice
  - `maxPrice`: Numeric value - Filter products with ListPrice ≤ maxPrice
  - `color`: String - Filter products by Color (case-insensitive)
  - `category`: String - Filter products by Category (case-insensitive)

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

## Local Development

### Run locally for testing:

```cmd
cd src\CsProductApi
dotnet run
```

Test endpoints:
- `http://localhost:5000/api/data`
- `http://localhost:5000/api/data?color=Black&maxPrice=100&sort=price-asc`
- `http://localhost:5000/health`

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

**Deploy Lambda function only:**
```powershell
.\deploy.ps1 -DeployLambda
```

**Deploy Infrastructure (API Gateway) only:**
```powershell
.\deploy.ps1 -DeployInfrastructure
```

### Manual Deployment Steps

**1. Deploy Lambda Function:**
```cmd
cd src\CsProductApi
dotnet lambda deploy-function CsProductApi
```

**2. Deploy Infrastructure:**
```cmd
cd infrastructure\CsProductApi.Infrastructure
cdk bootstrap  # Only needed once per account/region
cdk deploy
```

## Architecture

- **AWS Lambda**: .NET 8 function handling API requests
- **API Gateway**: REST API with CORS enabled
- **Data Source**: Local JSON file with product data
- **Infrastructure**: AWS CDK for Infrastructure as Code

## Project Structure

```
├── src/
│   └── CsProductApi/           # Lambda function code
├── test/
│   └── CsProductApi.Tests/     # Unit tests
├── infrastructure/
│   └── CsProductApi.Infrastructure/  # CDK infrastructure code
├── deploy.ps1                 # Deployment script
└── README.md
```

## API Examples

**Get all products:**
```bash
curl "https://your-api-id.execute-api.region.amazonaws.com/prod/api/data"
```

**Filter by color and sort by price:**
```bash
curl "https://your-api-id.execute-api.region.amazonaws.com/prod/api/data?color=Black&sort=price-asc"
```

**Filter by category and max price:**
```bash
curl "https://your-api-id.execute-api.region.amazonaws.com/prod/api/data?category=Road%20Bikes&maxPrice=1000"
```

## Development Notes

- The Lambda function reads product data from `data.json` included in the deployment package
- CORS is enabled for all origins in development
- All query parameters are optional and can be combined
- Invalid sort parameters are ignored (no sorting applied)
- Case-insensitive filtering for color and category