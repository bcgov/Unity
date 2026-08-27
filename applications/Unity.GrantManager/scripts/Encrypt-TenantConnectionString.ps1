<#
.SYNOPSIS
    Encrypts a plain-text tenant connection string using ABP's StringEncryptionService algorithm.

.DESCRIPTION
    Mirrors ABP's StringEncryptionService (AES-256-CBC, PBKDF2/SHA-1, 1000 iterations).
    Use this to produce the encrypted value that should be stored in the
    AbpTenantConnectionStrings table, or to verify an existing encrypted value.

.PARAMETER PlainText
    The plain-text connection string to encrypt.

.PARAMETER PassPhrase
    The pass phrase from appsettings.json -> StringEncryption -> DefaultPassPhrase.

.PARAMETER Salt
    The salt used by ABP's StringEncryptionService (default matches ABP's built-in default).

.PARAMETER InitVector
    The AES initialisation vector used by ABP's StringEncryptionService (default matches ABP's built-in default).

.PARAMETER KeySize
    AES key size in bits. Must match the value configured in AbpStringEncryptionOptions (default: 256).

.EXAMPLE
    .\Encrypt-TenantConnectionString.ps1 `
        -PlainText "Host=localhost;Database=T_ABC123;Username=T_ABC123;Password=XYZ789" `
        -PassPhrase "g2IuZx7PwXDvCmlW"
#>
param(
    [Parameter(Mandatory)][string] $PlainText,
    [Parameter(Mandatory)][string] $PassPhrase,
    [string] $Salt       = "hgt!16kl",
    [string] $InitVector = "jkE49230Tf093b42",
    [int]    $KeySize    = 256
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$plainBytes = [Text.Encoding]::UTF8.GetBytes($PlainText)
$saltBytes  = [Text.Encoding]::ASCII.GetBytes($Salt)
$ivBytes    = [Text.Encoding]::ASCII.GetBytes($InitVector)

$deriveBytes = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($PassPhrase, $saltBytes, 1000)
$keyBytes    = $deriveBytes.GetBytes($KeySize / 8)

$aes      = [System.Security.Cryptography.Aes]::Create()
$aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
$aes.Key  = $keyBytes
$aes.IV   = $ivBytes

$ms = New-Object System.IO.MemoryStream
$cs = New-Object System.Security.Cryptography.CryptoStream($ms, $aes.CreateEncryptor(), [System.Security.Cryptography.CryptoStreamMode]::Write)

try {
    $cs.Write($plainBytes, 0, $plainBytes.Length)
    $cs.FlushFinalBlock()
    Write-Output ([Convert]::ToBase64String($ms.ToArray()))
} finally {
    $cs.Dispose()
    $ms.Dispose()
    $aes.Dispose()
    $deriveBytes.Dispose()
}
