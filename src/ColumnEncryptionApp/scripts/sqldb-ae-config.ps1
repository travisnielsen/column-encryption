Import-Module SqlServer

# Set up connection and database SMO objects
$password = ""
$sqlConnectionString = ""
$smoDatabase = Get-SqlDatabase -ConnectionString $sqlConnectionString

# If your encryption changes involve keys in Azure Key Vault, uncomment one of the lines below in order to authenticate:
#   * Prompt for a username and password:
Add-SqlAzureAuthenticationContext -Interactive

#   * Enter a Client ID, Secret, and Tenant ID:
#Add-SqlAzureAuthenticationContext -ClientID '<Client ID>' -Secret '<Secret>' -Tenant '<Tenant ID>'

# Change encryption schema

$encryptionChanges = @()

# Add reference to new column master key
$CmkSettings = New-SqlAzureKeyVaultColumnMasterKeySettings -KeyUrl "https://myvault.vault.contoso.net:443/keys/CMK/4c05f1a41b12488f9cba2ea964b6a700"
New-SqlColumnMasterKey "CMK1" -ColumnMasterKeySettings $CmkSettings

# Add an existing column encryption key
New-SqlColumnEncryptionKey -ColumnMasterKeyName "" -Name "" -EncryptedValue ""

# Generate a new column encryption key
New-SqlColumnEncryptionKey -ColumnMasterKeyName "" -Name ""

# Add changes for table [dbo].[Patients]
$encryptionChanges += New-SqlColumnEncryptionSettings -ColumnName dbo.Patients.SSN -EncryptionType Deterministic -EncryptionKey "CEKPatients"
$encryptionChanges += New-SqlColumnEncryptionSettings -ColumnName dbo.Patients.BirthDate -EncryptionType Deterministic -EncryptionKey "CEKPatients"

Set-SqlColumnEncryption -ColumnEncryptionSettings $encryptionChanges -InputObject $smoDatabase

