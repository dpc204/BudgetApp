
# Request a token
$body = @{
    client_id     = $clientId
    client_secret = $clientSecret
    scope         = "https://graph.microsoft.com/.default"
    grant_type    = "client_credentials"
}

$response = Invoke-RestMethod -Uri "https://login.microsoftonline.com/$tenantId/oauth2/v2.0/token" `
    -Method Post `
    -ContentType "application/x-www-form-urlencoded" `
    -Body $body

# Display the access token
Write-Host "✅ Token acquired successfully!" -ForegroundColor Green
Write-Host "`nAccess Token:" -ForegroundColor Cyan
$response.access_token

Write-Host "`n📊 Token Info:" -ForegroundColor Yellow
Write-Host "Token Type: $($response.token_type)"
Write-Host "Expires In: $($response.expires_in) seconds"

# Copy token to clipboard
$response.access_token | Set-Clipboard
Write-Host "`n📋 Token copied to clipboard!  Paste at https://jwt.ms to inspect" -ForegroundColor Green