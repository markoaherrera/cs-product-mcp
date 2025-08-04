# Deployment Guide

This guide walks you through deploying both CsProductApi and CsProductOrchestrator to AWS.

## Prerequisites

1. **AWS CLI configured** with appropriate credentials
2. **AWS CDK CLI** installed: `npm install -g aws-cdk`
3. **Amazon.Lambda.Tools** installed: `dotnet tool install -g Amazon.Lambda.Tools`
4. **OpenAI API Key** for the orchestrator service

## Quick Deployment (Recommended)

### Step 1: Deploy Everything
```powershell
.\deploy.ps1 -DeployAll
```

This will:
- Deploy CsProductApi Lambda function
- Deploy CsProductOrchestrator Lambda function  
- Deploy both API Gateways via CDK
- Show you the API Gateway URLs

### Step 2: Set Up Environment Variables
After deployment, you'll see output like:
```
ProductApiUrlForOrchestrator = https://abc123def.execute-api.us-east-1.amazonaws.com/prod/api/data
OrchestratorEndpoint = https://xyz789ghi.execute-api.us-east-1.amazonaws.com/prod/api/query
```

Set only the Product API URL (OpenAI API key is now provided in request body):
```bash
aws lambda update-function-configuration \
  --function-name CsProductOrchestrator \
  --environment "Variables={PRODUCT_API_URL=https://abc123def.execute-api.us-east-1.amazonaws.com/prod/api/data}"
```

Or use the AWS Console to set the `PRODUCT_API_URL` environment variable.

### Step 3: Test Your Deployment

**Test Product API:**
```bash
curl "https://abc123def.execute-api.us-east-1.amazonaws.com/prod/api/data?color=Black&maxPrice=100"
```

**Test Orchestrator:**
```bash
curl -X POST "https://xyz789ghi.execute-api.us-east-1.amazonaws.com/prod/api/query" \
  -H "Content-Type: application/json" \
  -d '{"query": "Show me black helmets under $50", "openAiApiKey": "your-openai-api-key"}'
```

## Step-by-Step Deployment

If you prefer to deploy components individually:

### 1. Deploy Lambda Functions
```powershell
# Deploy Product API
.\deploy.ps1 -DeployLambda

# Deploy Orchestrator
.\deploy.ps1 -DeployOrchestrator
```

### 2. Deploy Infrastructure
```powershell
.\deploy.ps1 -DeployInfrastructure
```

### 3. Configure Environment Variables
```powershell
.\setup-environment.ps1 -OpenAiApiKey "your-key" -ProductApiUrl "api-url-from-cdk-output"
```

## Manual Deployment

### Deploy Lambda Functions Manually
```bash
# Product API
cd src/CsProductApi
dotnet lambda deploy-function CsProductApi

# Orchestrator
cd ../CsProductOrchestrator
dotnet lambda deploy-function CsProductOrchestrator
```

### Deploy Infrastructure Manually
```bash
cd ../../infrastructure/CsProductApi.Infrastructure
cdk bootstrap  # Only needed once per account/region
cdk deploy
```

### Set Environment Variables Manually
```bash
aws lambda update-function-configuration \
  --function-name CsProductOrchestrator \
  --environment "Variables={OPEN_AI_API_KEY=your-key,PRODUCT_API_URL=your-product-api-url}"
```

## Troubleshooting

### Common Issues

**1. CDK Bootstrap Required**
```
Error: Need to perform AWS CDK bootstrap
```
Solution: Run `cdk bootstrap` in the infrastructure directory.

**2. Lambda Function Not Found**
```
Error: Function not found: CsProductApi
```
Solution: Deploy Lambda functions before infrastructure, or deploy them separately.

**3. Environment Variables Not Set**
```
Error: OpenAI API key is not set
```
Solution: Run the setup-environment.ps1 script or set variables manually.

**4. CORS Issues**
If you get CORS errors in the browser, make sure both API Gateways have CORS enabled (they should by default).

### Verification Commands

**Check Lambda Functions:**
```bash
aws lambda list-functions --query 'Functions[?contains(FunctionName, `CsProduct`)].FunctionName'
```

**Check Environment Variables:**
```bash
aws lambda get-function-configuration --function-name CsProductOrchestrator --query 'Environment.Variables'
```

**Check API Gateway:**
```bash
aws apigateway get-rest-apis --query 'items[?contains(name, `CsProduct`)].{Name:name,Id:id}'
```

## Local Testing

Before deploying, you can test locally:

**Product API:**
```bash
cd src/CsProductApi
dotnet run
# Test: http://localhost:5000/api/data
```

**Orchestrator:**
```bash
cd src/CsProductOrchestrator
# Make sure .env file has OPEN_AI_API_KEY and PRODUCT_API_URL=http://localhost:5000/api/data
dotnet run
# Test: http://localhost:8000/api/orchestrator
```

## Cost Considerations

- **Lambda**: Pay per request and execution time
- **API Gateway**: Pay per API call
- **OpenAI**: Pay per token used in GPT requests

For development, costs should be minimal. Consider setting up AWS billing alerts.

## Security Notes

- Store OpenAI API key securely (use AWS Secrets Manager for production)
- Consider implementing API authentication for production use
- Review CORS settings for production deployment
- Use least-privilege IAM roles for Lambda functions