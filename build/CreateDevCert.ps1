function ImportCert {
	param (
		$cert,
		[string]$StoreScope,
		[string]$StoreName
	)
	if($StoreName -eq "Root") {
		$export = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Cert, "");
	} else {
		$export = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12, "");
	}
    #$export = $cert.Export([System.Security.Cryptography.X509Certificates.X509ContentType]::Pkcs12, "");
	$flags = [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::PersistKeySet -bor [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::Exportable
    $certificate = New-Object System.Security.Cryptography.X509Certificates.X509Certificate2($export, "", $flags);
	$certificate.FriendlyName = "ASP.NET Core HTTPS development certificate"

	Write-Host "Adding Certificate to $StoreScope/$StoreName"
	$Store = New-Object System.Security.Cryptography.X509Certificates.X509Store($StoreName, $StoreScope)
	$Store.Open([System.Security.Cryptography.X509Certificates.OpenFlags]::ReadWrite)
	try {
		$Store.Add($certificate)
	}
	catch {
		Write-Output "An error occurred: $_"
	} finally {
		$Store.Close()
	}
}

$oid = "1.3.6.1.4.1.311.84.1.1"
Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.Extensions | Where-Object { $_.Oid.Value -eq $oid } } | Remove-Item -Force
Get-ChildItem -Path Cert:\CurrentUser\Root | Where-Object { $_.Extensions | Where-Object { $_.Oid.Value -eq $oid } } | Remove-Item -Force

Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object { $_.Extensions | Where-Object { $_.Oid.Value -eq $oid } } | Remove-Item -Force
Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object { $_.Extensions | Where-Object { $_.Oid.Value -eq $oid } } | Remove-Item -Force

Get-ChildItem -Path Cert:\CurrentUser\My | Where-Object { $_.FriendlyName -eq "IIS Express Development Certificate" } | Remove-Item -Force
Get-ChildItem -Path Cert:\CurrentUser\Root | Where-Object { $_.FriendlyName -eq "IIS Express Development Certificate" } | Remove-Item -Force

Get-ChildItem -Path Cert:\LocalMachine\My | Where-Object { $_.FriendlyName -eq "IIS Express Development Certificate" } | Remove-Item -Force
Get-ChildItem -Path Cert:\LocalMachine\Root | Where-Object { $_.FriendlyName -eq "IIS Express Development Certificate" } | Remove-Item -Force

$oid_obj = New-Object System.Security.Cryptography.Oid($oid, "ASP.NET Core HTTPS development certificate")
$ext = New-Object System.Security.Cryptography.X509Certificates.X509Extension($oid_obj,	@(2), $false)

$cert = New-SelfSignedCertificate -DnsName "localhost" `
	-CertStoreLocation "cert:\LocalMachine\My" `
	-NotAfter (Get-Date).AddYears(5) `
	-FriendlyName "IIS Express Development Certificate" `
	-Extension @( $ext ) `
	-TextExtension @('2.5.29.37={text}1.3.6.1.5.5.7.3.1', '2.5.29.19={text}CA=false') #{critical}
$thumb = $cert.GetCertHashString()
Write-Host "Created certificate $thumb in LocalMachine\My"

For ($i=44300; $i -le 44399; $i++) {
    netsh http delete sslcert ipport=0.0.0.0:$i | Out-Null
    netsh http add sslcert ipport=0.0.0.0:$i certhash=$thumb appid="{214124cd-d05b-4309-9af9-9caa44b2b74a}" | Out-Null
}


ImportCert $cert "CurrentUser" "My"
#ImportCert $cert "CurrentUser" "Root" # To avoid the UI Security prompt, we put it in LM Root instead
ImportCert $cert "LocalMachine" "Root"