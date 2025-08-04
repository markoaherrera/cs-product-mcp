# PowerShell script to help set up environment variables for CsProductOrchestrator
param(
    [Parameter(Mandatory=$true)]
    [string]$ProductApiUrl,
    
    [Parameter(Mandatory=$false)]
    [string]$FunctionName = "CsProductOrchestrator"
)

Write-Host "=== Environment Setup for CsProductOrchestrator ===" -ForegroundColor Green
Write-Host "Note: OpenAI API key is now provided in the request body, not as an environment variable" -ForegroundColor Cyan

# Update Lambda function environment variables
Write-Host "Setting environment variables for Lambda function: $FunctionName" -ForegroundColor Yellow

try {
    # Set only PRODUCT_API_URL
    Write-Host "Setting PRODUCT_API_URL..." -ForegroundColor Cyan
    aws lambda update-function-configuration --function-name $FunctionName --environment "Variables={PRODUCT_API_URL=$ProductApiUrl}"
    
    if ($LASTEXITCODE -eq 0) {
        Write-Host "Environment variable set successfully!" -ForegroundColor Green
        Write-Host "PRODUCT_API_URL: $ProductApiUrl" -ForegroundColor White
        Write-Host ""
        Write-Host "Remember to include your OpenAI API key in the request body when calling the API:" -ForegroundColor Yellow
        Write-Host '{"query": "your question", "openAiApiKey": "your-openai-api-key"}' -ForegroundColor White
    } else {
        Write-Host "Failed to set environment variable!" -ForegroundColor Red
        exit 1
    }
}
catch {
    Write-Host "Error setting environment variable: $($_.Exception.Message)" -ForegroundColor Red
    exit 1
}

Write-Host "=== Environment Setup Complete ===" -ForegroundColor Green
Write-Host "Your CsProductOrchestrator is now ready to use!" -ForegroundColor Green