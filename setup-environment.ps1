# PowerShell script to help set up environment variables for CsProductOrchestrator
param(
    [Parameter(Mandatory=$true)]
    [string]$OpenAiApiKey,
    
    [Parameter(Mandatory=$false)]
    [string]$ProductApiUrl = "",
    
    [Parameter(Mandatory=$false)]
    [string]$FunctionName = "CsProductOrchestrator"
)

Write-Host "=== Environment Setup for CsProductOrchestrator ===" -ForegroundColor Green

# If ProductApiUrl is not provided, try to get it from CDK outputs
if ([string]::IsNullOrEmpty($ProductApiUrl)) {
    Write-Host "Attempting to get Product API URL from CDK outputs..." -ForegroundColor Yellow
    
    Push-Location "infrastructure\CsProductApi.Infrastructure"
    try {
        $cdkOutput = cdk list --json 2>$null | ConvertFrom-Json
        if ($cdkOutput) {
            Write-Host "Please run 'cdk deploy' first to get the Product API URL, then run this script with -ProductApiUrl parameter" -ForegroundColor Red
            exit 1
        }
    }
    catch {
        Write-Host "Could not retrieve CDK outputs. Please provide -ProductApiUrl parameter manually." -ForegroundColor Red
        Write-Host "Example: .\setup-environment.ps1 -OpenAiApiKey 'your-key' -ProductApiUrl 'https://abc123.execute-api.us-east-1.amazonaws.com/prod/api/data'" -ForegroundColor Yellow
        exit 1
    }
    finally {
        Pop-Location
    }
}

# Update Lambda function environment variables
Write-Host "Setting environment variables for Lambda function: $FunctionName" -ForegroundColor Yellow

try {
    # Set OPEN_AI_API_KEY
    Write-Host "Setting OPEN_AI_API_KEY..." -ForegroundColor Cyan
    aws lambda update-function-configuration --function-name $FunctionName --environment "Variables={OPEN_AI_API_KEY=$OpenAiApiKey,PRODUCT_API_URL=$ProductApiUrl}"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Environment variables set successfully!" -ForegroundColor Green
        Write-Host "OPEN_AI_API_KEY: [HIDDEN]" -ForegroundColor White
        Write-Host "PRODUCT_API_URL: $ProductApiUrl" -ForegroundColor White
    } else {
        Write-Host "Failed to set environment variables!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error setting environment variables: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "=== Environment Setup Complete ===" -ForegroundColor Green
Write-Host "Your CsProductOrchestrator is now ready to use!" -ForegroundColor Green