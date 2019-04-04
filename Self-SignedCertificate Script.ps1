# Create and export a self-signed certificate
# Script by Tim Buntrock
 
$Certname = Read-Host “Enter Certificate Name”
# Define expiration
$YearsToExpire = Read-Host “How many years should this certificate be valid”
Write-Host "Creating Certifcate $Certname" -ForegroundColor Green
# Create certificate
$Cert = New-SelfSignedCertificate -certstorelocation cert:\localmachine\my -dnsname $Certname -NotAfter (Get-Date).AddYears($YearsToExpire)
Write-Host "Exporting Certificate $Certname to $env:USERPROFILE\Desktop\$Certname.pfx" -ForegroundColor Green
# Set password to export certificate
$pw = ConvertTo-SecureString -String "Password" -Force -AsPlainText
# Get thumbprint
$thumbprint = $Cert.Thumbprint
# Export certificate
Export-PfxCertificate -cert cert:\localMachine\my\$thumbprint -FilePath $env:USERPROFILE\Desktop\$Certname.pfx -Password $pw