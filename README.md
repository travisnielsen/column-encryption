# Introduction

The project describes an Azure-native topology for applying field (column) level encryption for a variety of scenarios across an organization's data estate. It includes sample code and documentation for evaluation purposes.

The core tenant of this project is the use of two encryption libraries that have been in production with Microsoft for several years:
1. The implementation of the `AEAD_AES_256_CBC_HMAC_SHA256` algorithm [within the open sourced versions of SQL Client](https://github.com/dotnet/SqlClient/tree/master/src/Microsoft.Data.SqlClient/netcore/src/Microsoft/Data/SqlClient).
2. The [Always Encrypted Azure Key Vault Provider](https://docs.microsoft.com/en-us/dotnet/api/microsoft.data.sqlclient.alwaysencrypted.azurekeyvaultprovider?view=akvprovider-dotnet-core-1.1) for managing encryption and decryption of column master keys via Azure Key Vault.

Because these libraries are portable, a standardized and compatible approach to encryption can extend beyond just supported data engines such as Azure SQL Database. For example, records with sensitive columns encrypted via SQL Always Encrypted can be streamed as events as part of a change data capture (CDC) feed and subsequently decrypted and procecessed elsewhere by an authorized microservice.

## Encryption Scenarios

The model can be used to support three principal scenarios: (1) migration from on-premises systems to a data lake, (2) data preparation and analytics via a distributed processing system, and (3) event driven architectures. The following diagram highlights (in yellow) where the aforementioned code libraries would be incorporated as part of an implementation in Azure as well as the services that support encryption configuration and key management.

<img src="docs/img/encryption-topology.png" />

## Components

### Encryption SDK

An encryption SDK will be used to perform cryptographic operations that are part of data flows outside of supported data platform clients. An early prototype of this SDK, implemented in .NET Core, is included as part of this repository. This SDK is intended to be pluggable in order to support a various enterprise management scenarios.

### Encryption Metadata

The configuration of encryption for sensitive columns is specified in a configuration file (or API), which may be stored in a highly-available location. This file represents the master record of encryption keys and algorithms used across all workloads. This file would likely be maintained by a compliance team and its configuration data, when updated, is applied (via automation) to supported data engines such as Azure SQL Database with Always Encrypted. Applications based on the aforementioned Encryption SDK would load updated settings using a modular settings provider.

The following is a sample configuraiton file where two columns are classified as sensitive are to be encrypted with the same encryption key (CEK) and algorithm. There is a one-to-many relationship between a given CEK and columns.

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

If the CEK is not present in the file, the prototype SDK will generate one and encrypt it using the Column Master Key (CMK) referenced in the `ColumnMasterKeyInfo` section. CEKs may be pre-provisined in the file if they had already been generated elsewhere.

> **NOTE:** Column Encryption Keys are always encrypted (wrapped) by a Column Master Key (CMK) hosted in Azure Key Vault. They are never exposed as plaintext. Access to decrypt CEKs is managed audited through Azure Key Vault access policies.

### Supported Data Engines

As previously mentioned, the use of the Encryption SDK will be fully compatible with native encryption support already available in Azure SQL Database. This provides seamless support for data processing is it flows in and out from various systems. [Sample Azure Functions](src/ColumnEncryptionFunctions/README.md) are provided in this repository that illustrate this capability. It should be noted that updates to the encryption metadata must be pushed to these systems as well via DevOps automation.

### Automation (DevOps)

Successful management of a distributed encryption infrastructure such as this relies on the use of modern automation practices based on tools such as Azure DevOps and/or GitHib Enterprise. A proposed management topology and workflow is discussed in more detail in the [Encryption Configuration Management](docs/management-and-ops.md) document.

## Next Steps: Deployment and Operations

Use following documentation for details on deployment and operations:

* [Encryption Configuration Management](docs/management-and-ops.md)
* [Configuration: Azure Environment](docs/configure-azure.md) (pre-requisites)
* [Configuration: Sample Console Application](src/ColumnEncryptionApp/README.md)
* [Configuration: Sample Azure Functions](src/ColumnEncryptionFunctions/README.md)
