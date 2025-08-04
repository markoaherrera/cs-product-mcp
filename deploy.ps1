# PowerShell deployment script for CsProductApi and CsProductOrchestrator
param(
    [string]$Environment = "dev",
    [switch]$DeployLambda = $false,
    [switch]$DeployOrchestrator = $false,
    [switch]$DeployInfrastructure = $false,
    [switch]$DeployAll = $false
)

Write-Host "=== CsProductApi & CsProductOrchestrator Deployment Script ===" -ForegroundColor Green

if ($DeployAll) {
    $DeployLambda = $true
    $DeployOrchestrator = $true
    $DeployInfrastructure = $true
}

# Deploy Product API Lambda function
if ($DeployLambda) {
    Write-Host "Deploying CsProductApi Lambda function..." -ForegroundColor Yellow
    Push-Location "src\CsProductApi"
    try {
        dotnet lambda deploy-function CsProductApi
        if ($LASTEXITCODE -eq 0) {
            Write-Host "CsProductApi Lambda function deployed successfully!" -ForegroundColor Green
        } else {
            Write-Host "CsProductApi Lambda deployment failed!" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

# Deploy Orchestrator Lambda function
if ($DeployOrchestrator) {
    Write-Host "Deploying CsProductOrchestrator Lambda function..." -ForegroundColor Yellow
    Push-Location "src\CsProductOrchestrator"
    try {
        dotnet lambda deploy-function CsProductOrchestrator
        if ($LASTEXITCODE -eq 0) {
            Write-Host "CsProductOrchestrator Lambda function deployed successfully!" -ForegroundColor Green
        } else {
            Write-Host "CsProductOrchestrator Lambda deployment failed!" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

# Deploy Infrastructure (API Gateway)
if ($DeployInfrastructure) {
    Write-Host "Deploying infrastructure with CDK..." -ForegroundColor Yellow
    Push-Location "infrastructure\CsProductApi.Infrastructure"
    try {
        # Bootstrap CDK if needed (only needs to be done once per account/region)
        Write-Host "Checking CDK bootstrap status..." -ForegroundColor Cyan
        cdk bootstrap
        
        # Deploy the stack
        Write-Host "Deploying CDK stack..." -ForegroundColor Cyan
        cdk deploy --require-approval never
        
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Infrastructure deployed successfully!" -ForegroundColor Green
            Write-Host "Check the output above for your API Gateway URLs" -ForegroundColor Cyan
            Write-Host ""
            Write-Host "IMPORTANT: Don't forget to set up the PRODUCT_API_URL environment variable for CsProductOrchestrator!" -ForegroundColor Yellow
            Write-Host "Use the setup-environment.ps1 script:" -ForegroundColor Yellow
            Write-Host "  .\setup-environment.ps1 -ProductApiUrl 'product-api-url-from-output'" -ForegroundColor White
            Write-Host "Note: OpenAI API key is now provided in the request body, not as an environment variable" -ForegroundColor Cyan
        } else {
            Write-Host "Infrastructure deployment failed!" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $DeployLambda -and -not $DeployOrchestrator -and -not $DeployInfrastructure) {
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\deploy.ps1 -DeployAll                    # Deploy both Lambda functions and Infrastructure" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployLambda                 # Deploy only CsProductApi Lambda function" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployOrchestrator           # Deploy only CsProductOrchestrator Lambda function" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployInfrastructure         # Deploy only Infrastructure (API Gateways)" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\deploy.ps1 -DeployAll                    # Full deployment" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployLambda                 # Update Product API Lambda only" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployOrchestrator           # Update Orchestrator Lambda only" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployLambda -DeployOrchestrator  # Update both Lambda functions" -ForegroundColor White
}

Write-Host "=== Deployment Complete ===" -ForegroundColor Green