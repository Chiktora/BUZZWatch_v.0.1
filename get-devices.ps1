Write-Host "Getting list of devices from BuzzWatch API..."

try {
    # Skip type definition if already exists
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
    
    # This endpoint may need authentication, but we'll try the admin endpoint
    $response = Invoke-RestMethod -Uri "https://localhost:7116/api/v1/admin/devices" -Method Get
    
    Write-Host "Devices found:"
    $response | ForEach-Object {
        Write-Host "ID: $($_.id) - Name: $($_.name) - Location: $($_.location)"
    }
} catch {
    Write-Host "Error getting devices: $_" -ForegroundColor Red
    
    # Let's try a different approach - attempt to get devices through the public API
    try {
        $response = Invoke-RestMethod -Uri "https://localhost:7116/api/v1/devices" -Method Get
        
        Write-Host "Devices found (public API):"
        $response | ForEach-Object {
            Write-Host "ID: $($_.id) - Name: $($_.name) - Location: $($_.location)"
        }
    } catch {
        Write-Host "Error getting devices from public API: $_" -ForegroundColor Red
    }
} 