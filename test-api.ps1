# Login to get token
$loginBody = @{
    email = "admin@buzzwatch.com"
    password = "Admin123!"
} | ConvertTo-Json

Write-Host "Logging in as admin..."
$loginResult = Invoke-RestMethod -Uri "http://localhost:5189/api/v1/auth/login" -Method Post -Body $loginBody -ContentType "application/json"
$token = $loginResult.token

Write-Host "Token received: $token"

# Create a unique email with timestamp
$timestamp = Get-Date -Format "yyyyMMddHHmmss"
$email = "user$timestamp@example.com"

# Create a new user
$newUserBody = @{
    email = $email
    password = "Newuser123!"
    name = "New Test User"
    role = "User"
} | ConvertTo-Json

Write-Host "Creating new user with email: $email"
$headers = @{
    Authorization = "Bearer $token"
}

try {
    $createResult = Invoke-RestMethod -Uri "http://localhost:5189/api/v1/admin/users" -Method Post -Body $newUserBody -ContentType "application/json" -Headers $headers
    Write-Host "User created successfully:"
    $createResult | ConvertTo-Json
} catch {
    Write-Host "Error creating user: $_"
    Write-Host "Status code: $($_.Exception.Response.StatusCode)"
    
    try {
        $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
        $reader.BaseStream.Position = 0
        $reader.DiscardBufferedData()
        $responseBody = $reader.ReadToEnd()
        Write-Host "Response body: $responseBody"
    } catch {
        Write-Host "Could not read response body: $_"
    }
}

$deviceId = "55E66C5F-A840-40FE-9962-D13F05F6FD8F" # Device ID from Swagger
$apiKey = "92483e93bbc7436b80a930a88e0c6bd4"

$headers = @{
    "X-Api-Key" = $apiKey
    "Content-Type" = "application/json"
}

$body = @{
    recordedAt = (Get-Date).ToUniversalTime().ToString("o")
    tempInsideC = 29.5
    humInsidePct = 54.3
    tempOutsideC = 21.2
    humOutsidePct = 48.6
    weightKg = 62.8
} | ConvertTo-Json

Write-Host "Sending test measurement to device: $deviceId"
try {
    # For older PowerShell, we need to ignore certificate validation
    if (-not ([System.Management.Automation.PSTypeName]'TrustAllCertsPolicy').Type) {
        add-type @"
        using System.Net;
        using System.Security.Cryptography.X509Certificates;
        public class TrustAllCertsPolicy : ICertificatePolicy {
            public bool CheckValidationResult(
                ServicePoint srvPoint, X509Certificate certificate,
                WebRequest request, int certificateProblem) {
                return true;
            }
        }
"@
    }
    [System.Net.ServicePointManager]::CertificatePolicy = New-Object TrustAllCertsPolicy
    [System.Net.ServicePointManager]::SecurityProtocol = [System.Net.SecurityProtocolType]::Tls12
    
    $response = Invoke-RestMethod -Uri "https://localhost:7116/api/v1/devices/$deviceId/measurements" `
        -Method Post -Headers $headers -Body $body
    
    Write-Host "✅ Measurement sent successfully!" -ForegroundColor Green
    Write-Host $response
} catch {
    Write-Host "❌ Error sending measurement: $_" -ForegroundColor Red
} 