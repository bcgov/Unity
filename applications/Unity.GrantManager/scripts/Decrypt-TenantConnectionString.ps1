<#
.SYNOPSIS
    Decrypts an ABP-encrypted tenant connection string.

.DESCRIPTION
    Mirrors ABP's StringEncryptionService (AES-256-CBC, PBKDF2/SHA-1, 1000 iterations).
    Use this to recover a plain-text connection string from the value stored in the
    AbpTenantConnectionStrings table.

.PARAMETER EncryptedText
    The base64-encoded encrypted value from the database.

.PARAMETER PassPhrase
    The pass phrase from appsettings.json -> StringEncryption -> DefaultPassPhrase.

.PARAMETER Salt
    The salt used by ABP's StringEncryptionService (default matches ABP's built-in default).

.PARAMETER InitVector
    The AES initialisation vector used by ABP's StringEncryptionService (default matches ABP's built-in default).

.PARAMETER KeySize
    AES key size in bits. Must match the value configured in AbpStringEncryptionOptions (default: 256).

.EXAMPLE
    .\Decrypt-TenantConnectionString.ps1 `
        -EncryptedText "abc123==" `
        -PassPhrase "g2IuZx7PwXDvCmlW"
#>
param(
    [Parameter(Mandatory)][string] $EncryptedText,
    [Parameter(Mandatory)][string] $PassPhrase,
    [string] $Salt       = "hgt!16kl",
    [string] $InitVector = "jkE49230Tf093b42",
    [int]    $KeySize    = 256
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$cipherBytes = [Convert]::FromBase64String($EncryptedText)
$saltBytes   = [Text.Encoding]::ASCII.GetBytes($Salt)
$ivBytes     = [Text.Encoding]::ASCII.GetBytes($InitVector)

$deriveBytes = New-Object System.Security.Cryptography.Rfc2898DeriveBytes($PassPhrase, $saltBytes, 1000)
$keyBytes    = $deriveBytes.GetBytes($KeySize / 8)

$aes      = [System.Security.Cryptography.Aes]::Create()
$aes.Mode = [System.Security.Cryptography.CipherMode]::CBC
$aes.Key  = $keyBytes
$aes.IV   = $ivBytes

$ms = New-Object System.IO.MemoryStream(, $cipherBytes)
$cs = New-Object System.Security.Cryptography.CryptoStream($ms, $aes.CreateDecryptor(), [System.Security.Cryptography.CryptoStreamMode]::Read)
$sr = New-Object System.IO.StreamReader($cs)

try {
    Write-Output $sr.ReadToEnd()
} finally {
    $sr.Dispose()
    $cs.Dispose()
    $ms.Dispose()
    $aes.Dispose()
    $deriveBytes.Dispose()
}
