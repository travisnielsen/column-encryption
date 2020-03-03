# Azure Environment Configuration

This page documents the steps required to deploy and configure the Azure components necessary for running the various test scenarios described in this repository.

## Azure Function App

Create a Function App for hosting custom encryption services. Azure Cloud Shell is recommended for running these commands:

```bash
location="centralus"
funcRgName=""
storageAcctName=""
appName=""

az storage account create --name $storageAcctName --location $location --resource-group $funcRgName --sku Standard_LRS

az functionapp create --resource-group $funcRgName --consumption-plan-location $location --runtime dotnet --os-type Linux --functions_version 3 --name $appName --storage-account $storageAcctName
```

A System Assigned Managed Identity will be used to access keys hosted in Azure Key Vault. Enable Managed Identity on the Function App by running the following command:

```bash
az functionapp identity assign --name $appName --resource-group $funcRgName
```

Be sure to document the `principalId` listed in the JSON result. It will be required when assigning Key Vault access later in this document.

## Service Principal (local testing)

Enter the following in Azure CLI to create the test Service Principal.

```bash
az ad sp create-for-rbac --name alwaysprotected-dev --skip-assignment
```

Document the output as it will be required in the next steps.

```json
{
  "appId": "[your_app_id]",
  "displayName": "alwaysprotected-dev",
  "name": "http://alwaysprotected-dev",
  "password": "[your_password]",
  "tenant": "[your_tenant_id]"
}
```

## Azure Key Vault

Azure CLI commands will be used to deploy and configure Azure Key Vault. To help streamline the setup, set the following environment variables to match your environment.

```bash
vaultName="your keyvault name"
vaultRgName="resource group name"
location="key vault location. Ex: centralus"
keyName="name of your column master key"
applicationId="service principal appId"
functionPrincipalId="principalId for the Functions managed identity"
```

Create a new Azure Key Vault for hosting Column Master Keys:

```bash
az keyvault create --location $location --name $vaultName --resource-group $vaultRgName --enable-purge-protection true --enable-soft-delete true
```

Next, a Column Master Key (CMK) needs to be provisioned. The CMK is used to encrypt (wrap) Column Encryption Keys (CEKs). In this example, a 2048-bit RSA key is generated and stored in the Azure Key Vault HSM. For details of various options for keys in Key Vault, please see the [documentation](https://docs.microsoft.com/en-us/cli/azure/keyvault/key?view=azure-cli-latest#az-keyvault-key-create) for `az keyvault key create`.

```bash
az keyvault key create --name $keyName --vault-name $vaultName --kty RSA-HSM --protection hsm --size 2048
```

Next, permissions to use the CMK for crytographic operations are granted to the Service Principal created earlier (test environment only).

```bash
az keyvault set-policy --name $vaultName --key-permissions get, list, sign, unwrapKey, verify, wrapKey --resource-group $vaultRgName --spn $applicationId
```

Finally, assign permissions to the Managed Identity of the Function App.

>**NOTE**: Typically a production Key Vault would only have access polices for Managed Identites and not local developer accounts. Instructions to add permissions to both account types is purely for demonstration convenience.

```bash
az keyvault set-policy --name $vaultName --key-permissions get, list, sign, unwrapKey, verify, wrapKey --resource-group $vaultRgName --object-id $functionPrincipalId
```

## Azure SQL Database

To configure the test Clinics database, we will use a modified version of the setup instructions documented in this example: [Always Encrypted: Protect sensitive data and store encryption keys in Azure Key Vault](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-always-encrypted-azure-key-vault?tabs=azure-cli#create-a-blank-sql-database)

First, use SQL Server Management Studio to create a blank datbase. Next, define its schema via this SQL statement

```SQL
CREATE TABLE [dbo].[Patients](
         [PatientId] [int] IDENTITY(1,1),
         [SSN] [char](11) NOT NULL,
         [FirstName] [nvarchar](50) NULL,
         [LastName] [nvarchar](50) NULL,
         [MiddleName] [nvarchar](50) NULL,
         [StreetAddress] [nvarchar](50) NULL,
         [City] [nvarchar](50) NULL,
         [ZipCode] [char](5) NULL,
         [State] [char](2) NULL,
         [BirthDate] [date] NOT NULL
         PRIMARY KEY CLUSTERED ([PatientId] ASC) ON [PRIMARY] );
GO
```

Next, use the following PowerShell script to define the columns to encrypt as well as the Column Master Key (CMK).

```powershell
TBD
```

Full details on available PowerShell commands for controlling Always Encrypted settings for Azure SQL Database can be found here: [Configure Always Encrypted using PowerShell](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/configure-always-encrypted-using-powershell?view=sql-server-ver15)

Once the encryption configuration is completed, identify and document the encrypted (wrapped) value of the Column Encryption Key (CEK) for the `SSN` column.

```SQL
SELECT * from sys.column_encryption_key_values
```

## Metadata Configuration File

Finally, create a YAML file that represents the column data classification and add the value of the generated CEK in the `EncryptedColumnKey` field for `CEKConfidential`.

```yaml
ColumnEncryptionInfo:
- ColumnName: SSN
  ColumnKeyName: CEKConfidential
  EncryptionType: Deterministic
  Algorithm: AEAD_AES_256_CBC_HMAC_SHA256
ColumnKeyInfo:
- Name: CEKConfidential
  EncryptedColumnKey: 0x01B200000...
  Algorithm: RSA_OAEP
  ColumnMasterKeyName: CMKConfidential
ColumnMasterKeyInfo:
- Name: CMKConfidential
  KeyProvider: AZURE_KEY_VAULT
  KeyPath: https://[vault-name].vault.azure.net/keys/[key-name]/[key-identifier]
```

Be sure the `KeyPath` value for `CMKConfidential` matches the actual key path in your Key Vault.
