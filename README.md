# Introduction

The project describes an Azure-native topology for applying field (column) level encryption for a variety of scenarios across an organization's data estate. It includes sample code and documentation for evaluation purposes.

The core tenant of this project is the use of two encryption libraries that have been in production with Microsoft for several years:
1. The implementation of the `AEAD_AES_256_CBC_HMAC_SHA256` algorithm [within the open sourced versions of SQL Client](https://github.com/dotnet/SqlClient/tree/master/src/Microsoft.Data.SqlClient/netcore/src/Microsoft/Data/SqlClient).
2. The [Always Encrypted Azure Key Vault Provider](https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.alwaysencrypted.azurekeyvaultprovider?view=akvprovider-dotnet-core-1.1) for managing encryption and decryption of column master keys via Azure Key Vault.

Because these libraries are portable, they can be used to provide standardized approach to data protection across a variety of workloads and scenarios. For example, sensitive fields encrypted via SQL Always Encrypted can be exported to a non-SQL system or data flow and decrypted by, for example, a microservice.

## Topology

The model can be used in a variety of topologies. The canonical example used here is bulk encryption of source data based on data classification performed in a configuration file. The following diagram explains the workflow.

<img src="docs/img/encryption-topology.png" />

Sensitive columns are defined in the configuraiton file where a column encryption key (CEK) is referenced. There is a one-to-many relationship between a given CEK and columns. The following YAML is an example configuration for encrypting two columns with the same key and same algorithm.

```yaml
ColumnEncryptionInfo:
- ColumnName: SSN
  ColumnKeyName: CEKConfidential
  EncryptionType: Deterministic
  Algorithm: AEAD_AES_256_CBC_HMAC_SHA256
- ColumnName: Email
  ColumnKeyName: CEKConfidential
  EncryptionType: Deterministic
  Algorithm: AEAD_AES_256_CBC_HMAC_SHA256
ColumnKeyInfo:
- Name: CEKConfidential
  EncryptedColumnKey: 0x01b40000016800740074007
  Algorithm: RSA_OAEP
  ColumnMasterKeyName: CMKConfidential
ColumnMasterKeyInfo:
- Name: CMKConfidential
  KeyProvider: AZURE_KEY_VAULT
  KeyPath: https://akvdemo.vault.azure.net/keys/democmk/abc123456
```

If the CEK is not present in the file, the sample application will generate one and encrypt it using the Column Master Key (CMK) referenced in the `ColumnMasterKeyInfo` section. CEKs may be pre-provisined in the file if they had already been generated elsewhere.

> **NOTE:** Column Encryption Keys are always encrypted (wrapped) by a Column Master Key (CMK) hosted in Azure Key Vault. They are never exposed as plaintext. Access to decrypt CEKs is managed audited through Azure Key Vault access policies.

## Deployment and Operations

Use following documentation for details on deployment and operations: 
* [Management and Operations](docs/management-and-ops.md)
* [Azure Environment Configuration](docs/configure-azure.md) (pre-requisites)
* [Sample Azure Functions](src/ColumnEncryptionFunctions/README.md)
