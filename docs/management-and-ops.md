# Encryption Configuration Management

Management of the encryption infrastucture involes three main components:

**Column Encryption Keys (CEKs)** - These are symetric (AES256) keys that are used to encrypt sensitive columns (fields). For performance and usability reasons, CEKs are typically embedded as metadata within a file or document or included as part of a database configuration. A CEK is always stored in an encrypted format so the metadata must always include a reference to a Column Master Key.

**Column Master Keys (CMKs)** - Used to encrypt (wrap) and decrypt (unwrap) Column Encryption Keys. They are typically RSA key pairs that are stored in a hardware security module (HSM) such as Azure Key Vault.

**Encryption Metadata** - Provides the necessary details that support encryption flows for data engines and applications. This includes the assignment of protected columns to CEKs, the embedded (wraped) CEKs, specification of encryption algorithms, and the linkage of CEKs to CMKs. For data engines such as Azure SQL Database, the hosting and management of encryption metadata is built into the platform and managed via platform-specific clients and tools. For custom applications, encryption metadata is externalized and used by SDKs (clients). To ensure consistency across various deployments, metadata is managed via an enterprise *metadata document*. An example of such a document can be seen here: [Metadata Configuration File](https://github.com/travisnielsen/column-encryption/blob/master/docs/configure-azure.md#metadata-configuration-file)
 
Management of this infrastructure involves tools and processes to ensure keys and metadata are properly created, secured, distributed, and made highly available. The following sections of this document describes tools and processes for achieving this.

> Further information about key management can be reviewed in the Azure SQL Database document: [Overview of key management for Always Encrypted](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/overview-of-key-management-for-always-encrypted?view=azuresqldb-current). The tools and concepts discussed in this repo are designed to be consistent and compatible with Always Encrypted, which is a fully supported, Generally Available feature.

Key management responsibilites are organized into the following roles:

* **Data Protection Engineer** - The Data Protection Engineer works with application owners to apply classifications to fields and ensure data is protected with the right keys across all flows. Any necessary changes to the key topology is documented in a *metadata document* with requests for new keys forwarded to the Key Administrator. Data Protection engineers also document security principals that require access to sensitive data and thus access to use CMKs hosted in Key Vault.
* **Key Administrator** - Using a secure workstation, Key Creators generate CMKs and save them to the appropriate Azure Key Vault based on the classification. Key Creators also generate new CEKs and submit them for deployment to data engines and services via an updated *metadata document*.
* **DevOps Engineer** - This role is responsible for creating and maintaining automation scripts that support the deployment of updated encryption metadata to custom applications as well as data engines.

## Key Creation and Distribution

All key generation activites are accomplished using existing, generally available tools which are deployed to a secure workstation. Distribution and updates to the business applications and core encryption services is achieved though a managed Continuous Deployment pipeline that includes release gates for coordination across systems. The following diagram illustrates the workflow:

<img src="img/key-creation-distribution.png"/>

### Column Master Keys

CMKs can be generated in a variety of ways, [including from within a compatible on-premises HSM infrastructure](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-hsm-protected-keys). Once a CMK is uploaded to an HSM, it cannot be exported or exfiltrated and instead can only be used for authorized cryptographic operations. The [az keyvault key create](https://docs.microsoft.com/en-us/cli/azure/keyvault/key?view=azure-cli-latest#az-keyvault-key-create) command is an example of a way by which a CMK can be created.

### Column Encryption Keys and Metadata

The [SqlServer PowerShell module](https://docs.microsoft.com/en-us/sql/relational-databases/security/encryption/configure-column-encryption-using-powershell?view=azuresqldb-current) allows for generating and encrypting (wrapping) CEK's via easy-to-use commands that can be deployed for use on secure workstations. This repository includes a sample script that implements these commands. See: [cek-create.ps1](../scripts/cek-create.ps1)

Once CEKs are generated, Key Management admins must then update the metadata configuration (YAML) file and issue a Pull Request to the Git repository for approval and deployment to Azure infrastructure.

## Key Rotation

## Access Control

This section documents the authorization controls used to enforce least-privelage access control for maintaing the encryption infrastructure and using encryption keys.

### Administrative Access

The following table describes the permissions required for each role.

| Role                     | Resource            | Permission                       |
|--------------------------|---------------------|----------------------------------|
| Data Protection Engineer | TBD                 | TBD                              |
| Key Administrator        | Azure Key Vault     | Keys: wrap, unwrap, list, verify, create, import |
| Key Administrator        | Secure Workstation  | TBD |


### Data Access

Column Encryption Keys (CEKs) do not have explicit access control and are typically embedded in encrypted (wrapped) format as metadta within a file or database schema. Controlling the ability to decrypt and hence use CEKs for data access is *implicitly* accomplished by assigning users and processes access to invoke the unwrap command in Azure Key Vault. This command uses the assigned Column Master Key (CMK) and the user or process must be granted access to run the command. Azure Key Vault (AKV) provides the facility to do this via [Access Policies](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-secure-your-key-vault).

> Because Azure Key Vault Access Policies are scoped to the vault and not individual keys, a separate vault must be created for each key that requires different access control.


## Availability and Recovery

The loss of any key used for encryption purposes means the loss of the data. Therefore, safeguards must be put into place to ensure both CEKs and CMKs are available and cannot be accidentally or intentionally deleted.
