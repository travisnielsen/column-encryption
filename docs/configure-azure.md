# Azure Environment Configuration

This page documents the steps required to deploy and configure the Azure components necessary for running the various test scenarios described in this repository. The following must be installed in your local development envioronment:

- [Azure CLI](https://docs.microsoft.com/en-us/cli/azure/install-azure-cli)
- [Azure Bicep](https://github.com/Azure/bicep/blob/ae8d7b320d82a0c6de152388ab7d7deef93dbf89/docs/installing.md)

## Deploy Azure Infrastructure

The Azure Bicep file included in the `scripts` directory creates the following resources:

- **Key Vault**: hosts Column Master Keys (CMKs) and manages access
- **Storage Account**: location for data files
- **SQL Database**: contains sample data with SQL Always Encrypted to protect sensitive columns

To deploy this infrastrucrure, navigate to the `scripts` directory using a terminal window directory and run the following:

```bash
az login
$userId=$(az ad user list --display-name '' --query "[].[objectId]" -o tsv)
bicep build deployment.bicep
az group create --name mdedemo --location centralus
az deployment group create --resource-group mdedemo --template-file deployment.json  --parameters userObjectId=$userId
```

Be sure to set your Azure Active Directory display name for the `--display-name` argument above. You can use the Azure Portal to confirm the resources after deployment completes.

## Configure Azure SQL Database and SQL Always Encrypted

To configure the test `testdb` database, we will use a modified version of the setup instructions documented in this example: [Always Encrypted: Protect sensitive data and store encryption keys in Azure Key Vault](https://docs.microsoft.com/en-us/azure/sql-database/sql-database-always-encrypted-azure-key-vault?tabs=azure-cli#create-a-blank-sql-database)

First, connect to the `testdb` database via SQL Server Management Studio or the Query Editor in the Azure Portal and create the `userdata` table.

```SQL
CREATE TABLE [dbo].[userdata](
         [id] [int] IDENTITY(1,1),
         [first_name] [nvarchar](50) NULL,
         [last_name] [nvarchar](50) NULL,
         [email] [nvarchar](50) NULL,
         [gender] [nvarchar](10) NULL,
         [ip_address] [nvarchar](50) NULL,
         [cc] [nvarchar](50) NULL,
         [country] [nvarchar](50) NULL,
         [birthdate] [date] NULL,
         [salary] [smallmoney] NULL,
         [title] [nvarchar](50) NULL,
         PRIMARY KEY CLUSTERED ([id] ASC) ON [PRIMARY] );
GO
```

Next, use the following PowerShell script to configure SQL Always Encrypted:

```powershell
 .\configure-sqlae.ps1 -ServerName {your_server_name} -KeyUri {key_uri}
```

Replace `{server_name}` with the friendly name of the SQL Server instance deployed earlier and `{key_uri}` with the value from Key Vaule (i.e. https://{vault_name}.vault.azure.net/keys/{key_name}/{version})

By default, this script configures the `cc` field to for Randomized encryption.

> Full details on available PowerShell commands for controlling Always Encrypted settings for Azure SQL Database can be found here: [Configure Always Encrypted using PowerShell](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/configure-always-encrypted-using-powershell?view=sql-server-ver15)

Once the encryption configuration is completed, identify and document the encrypted (wrapped) value of the Column Encryption Key (CEK) for the `SSN` column.

```SQL
SELECT * from sys.column_encryption_key_values
```

## Create Metadata Configuration File

Finally, create a YAML file that represents the column data classification and add the value of the generated CEK in the `EncryptedColumnKey` field for `cmk-userdata-pci`.

```yaml
ColumnEncryptionInfo:
- ColumnName: cc
  ColumnKeyName: cmk-userdata-pci
  EncryptionType: Randomized
ColumnKeyInfo:
- Name: cmk-userdata-pci
  EncryptedColumnKey: 0x01B200000...
  Algorithm: RSA_OAEP
  ColumnMasterKeyName: cmk-userdata-sensitive
ColumnMasterKeyInfo:
- Name: cmk-userdata-sensitive
  KeyProvider: AZURE_KEY_VAULT
  KeyPath: https://[vault-name].vault.azure.net/keys/[key-name]/[key-identifier]
```

Be sure the `KeyPath` value for `cmk-userdata-sensitive` matches the actual key path in your Key Vault.
