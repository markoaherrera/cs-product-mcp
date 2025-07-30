# PowerShell deployment script for CsProductApi
param(
    [string]$Environment = "dev",
    [switch]$DeployLambda = $false,
    [switch]$DeployInfrastructure = $false,
    [switch]$DeployAll = $false
)

Write-Host "=== CsProductApi Deployment Script ===" -ForegroundColor Green

if ($DeployAll) {
    $DeployLambda = $true
    $DeployInfrastructure = $true
}

# Deploy Lambda function
if ($DeployLambda) {
    Write-Host "Deploying Lambda function..." -ForegroundColor Yellow
    Push-Location "src\CsProductApi"
    try {
        dotnet lambda deploy-function CsProductApi
        if ($LASTEXITCODE -eq 0) {
            Write-Host "Lambda function deployed successfully!" -ForegroundColor Green
        } else {
            Write-Host "Lambda deployment failed!" -ForegroundColor Red
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
            Write-Host "Check the output above for your API Gateway URL" -ForegroundColor Cyan
        } else {
            Write-Host "Infrastructure deployment failed!" -ForegroundColor Red
            exit 1
        }
    }
    finally {
        Pop-Location
    }
}

if (-not $DeployLambda -and -not $DeployInfrastructure) {
    Write-Host "Usage:" -ForegroundColor Yellow
    Write-Host "  .\deploy.ps1 -DeployAll                    # Deploy both Lambda and Infrastructure" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployLambda                 # Deploy only Lambda function" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployInfrastructure         # Deploy only Infrastructure (API Gateway)" -ForegroundColor White
    Write-Host ""
    Write-Host "Examples:" -ForegroundColor Yellow
    Write-Host "  .\deploy.ps1 -DeployAll                    # Full deployment" -ForegroundColor White
    Write-Host "  .\deploy.ps1 -DeployLambda                 # Update Lambda only" -ForegroundColor White
}

Write-Host "=== Deployment Complete ===" -ForegroundColor Green