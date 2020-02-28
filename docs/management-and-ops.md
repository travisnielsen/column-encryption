# Key Management and Operations

This document describes the concepts for securely managing encryption key infrastructure.

## Distribution


## Access Control

Column Encryption Keys (CEKs) do not have explicit access control. Instead, access is *implicitly* granted by assigning users and processes access to decrypt them via the CEK's assigned Column Master Key (CMK). Azure Key Vault (AKV) provides the facility to do this via [Access Policies](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-secure-your-key-vault).

## Column Master Keys

Column Master Keys are used to encrypt (wrap) Column Encryption Keys. They are typically RSA key pairs that are stored in a hardware security module (HSM) such as Azure Key Vault. CMKs can be generated in a variety of ways, [including from within a compatible on-premises HSM infrastructure](https://docs.microsoft.com/en-us/azure/key-vault/key-vault-hsm-protected-keys). Once a CMK is uploaded to an HSM, it cannot be exported or exfiltrated and instead can only be used for authorized cryptographic operations.

## Rotation

## Backup and Recovery

The loss of any key used for encryption purposes means the loss of the data. Therefore, safeguards must be put into place to ensure both CEKs and CMKs are available and cannot be accidentally or intentionally deleted.

## Infrastructure Security

