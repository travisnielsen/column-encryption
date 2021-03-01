<#
.SYNOPSIS
    This script demonstrates generating Column Encryption Keys and storing them in Azure SQL Database

.DESCRIPTION
    Column Encryption Keys are generated via the SQL Server PowerShell Provider. Documentation on this process can be found at this link:
    https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/configure-column-encryption-using-powershell?view=azuresqldb-current

    NOTE: Currently, the SQL Server PowerShell Provider only works with non-.NET Core versions of Powershell (< 6.0). This script has been tested on a Windows 10 device with PowerShell 5.1 installed.
    Before testing, ensure you are using the correct version of PowerShell by executing this command: $PSVersionTable.PSVersion 
#>

[CmdletBinding()]
param (
    [Parameter(Mandatory)]
    [string]$ServerName,

    [Parameter(Mandatory)]
    [string]$KeyUri
)

# Use this to add the SqlServer module to the workstation
Import-Module SqlServer -Version 21.1.18218

# Set up connection and database SMO objects
$sqlConnectionString = "Server=tcp:${ServerName}.database.windows.net,1433;Initial Catalog=testdb;Persist Security Info=False;User ID=sqladmin;Password=${ServerName}!;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$smoDatabase = Get-SqlDatabase -ConnectionString $sqlConnectionString

# Authenticate to Azure Key Vault
Add-SqlAzureAuthenticationContext -Interactive

# Provision AKV-based column master key metadata to to SQL
$CmkSettings = New-SqlAzureKeyVaultColumnMasterKeySettings -KeyUrl $KeyUri
New-SqlColumnMasterKey -Name "cmk-userdata-sensitive" -ColumnMasterKeySettings $CmkSettings -InputObject $smoDatabase

# Generate a new column encryption key for PCI
$newCek = New-SqlColumnEncryptionKey -ColumnMasterKeyName "cmk-userdata-sensitive" -Name "cek-userdata-pci" -InputObject $smoDatabase
$newCek.ColumnEncryptionKeyValues
$newCek.ColumnEncryptionKeyValues.EncryptedValueAsSqlBinaryString

# NOTE: From here, the wrapped CEK and related metadata should now be submitted to the GitOps pipeline for deployment to other platforms and services

# Define and apply changes for table [dbo].[Patients]
$encryptionChanges = @()
$encryptionChanges += New-SqlColumnEncryptionSettings -ColumnName dbo.userdata.cc -EncryptionType Randomized -EncryptionKey "cek-userdata-pci"
Set-SqlColumnEncryption -ColumnEncryptionSettings $encryptionChanges -InputObject $smoDatabase
