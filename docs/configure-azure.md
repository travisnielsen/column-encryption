# Azure Environment Configuration

## Create Service Principal (local testing)

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

## Azure Function App

## Azure Key Vault

### Provision Column Master Key

The Column Master Key (CMK) is used to encrypt Column Encryption Keys (CEKs). In this example, a 2048-bit RSA key is generated and stored in the Azure Key Vault HSM with permissions to perform all key related operations. For details of various options for keys in Key Vault, please see the [documentation](https://docs.microsoft.com/en-us/cli/azure/keyvault/key?view=azure-cli-latest#az-keyvault-key-create) for `az keyvault key create`.

```bash
az keyvault key create --name trnieldemocmk --vault-name trniel-fledemo --kty RSA-HSM --protection hsm --size 2048
```

### Assign Permissions

When complted, use the following command to grant permissions to the Service Principal:

```bash

```

Next, assign permissions to the Managed Identity of the Function App:

```bash

```

## Metadata Configuration File

```yaml
ColumnEncryptionInfo:
- ColumnName: Email
  ColumnKeyName: CEKConfidential
  EncryptionType: Deterministic
  Algorithm: AEAD_AES_256_CBC_HMAC_SHA_256
ColumnKeyInfo:
- Name: CEKConfidential
  Algorithm: RSA_OAEP
  ColumnMasterKeyName: CMKConfidential
ColumnMasterKeyInfo:
- Name: CMKConfidential
  KeyProvider: AZURE_KEY_VAULT
  KeyPath: https://[vault-name].vault.azure.net/keys/[key-name]/[key-identifier]
```
