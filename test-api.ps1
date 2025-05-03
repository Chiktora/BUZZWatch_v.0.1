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