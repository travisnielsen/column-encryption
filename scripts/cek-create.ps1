<#
.SYNOPSIS
    This script demonstrates generating Column Encryption Keys and storing them in Azure SQL Database

.DESCRIPTION
    Column Encryption Keys are generated via the SQL Server PowerShell Provider. Documentation on this process can be found at this link:
    https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/configure-column-encryption-using-powershell?view=azuresqldb-current

    NOTE: Currently, the SQL Server PowerShell Provider only works with non-.Net Core versions of Powershell (< 6.0). This script has been tested on a Windows 10 device with PowerShell 5.1 installed.
    Before testing, ensure you are using the correct version of PowerShell by executing this command: $PSVersionTable.PSVersion 

.NOTES
    Author: Travis Nielsen
    Last Edit: 2020-03-21
    Version 1.0 - initial release
#>

# Use this to add the SqlServer module to the workstation
# Install-Module -Name SqlServer
Import-Module SqlServer

# Set up connection and database SMO objects
$sqlConnectionString = "Server=tcp:{db_name}.database.windows.net,1433;Initial Catalog=clinics;Persist Security Info=False;User ID={admin_id};Password={admin_pwd};MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"
$smoDatabase = Get-SqlDatabase -ConnectionString $sqlConnectionString

# Authenticate to Azure Key Vault
Add-SqlAzureAuthenticationContext -Interactive

# Provision AKV-based column master key metadata to to SQL
$CmkSettings = New-SqlAzureKeyVaultColumnMasterKeySettings -KeyUrl "https://{vault_name}.vault.azure.net/keys/{key_name}/{version}"
New-SqlColumnMasterKey -Name "cmk-phi" -ColumnMasterKeySettings $CmkSettings -InputObject $smoDatabase

# Generate a new column encryption key
$newCek = New-SqlColumnEncryptionKey -ColumnMasterKeyName "cmk-phi" -Name "cek-test-phi" -InputObject $smoDatabase
$newCek.ColumnEncryptionKeyValues
$newCek.ColumnEncryptionKeyValues.EncryptedValueAsSqlBinaryString

# NOTE: From here, the wrapped CEK and related metadata should now be submitted to the GitOps pipeline for deployment to other platforms and services

# Define and apply changes for table [dbo].[Patients]
$encryptionChanges = @()
$encryptionChanges += New-SqlColumnEncryptionSettings -ColumnName dbo.Patients.SSN -EncryptionType Deterministic -EncryptionKey "cek-patients-phi"
$encryptionChanges += New-SqlColumnEncryptionSettings -ColumnName dbo.Patients.BirthDate -EncryptionType Randomized -EncryptionKey "cek-patients-phi"
Set-SqlColumnEncryption -ColumnEncryptionSettings $encryptionChanges -InputObject $smoDatabase
