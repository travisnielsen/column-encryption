# Introduction 
TODO: Give a short introduction of your project. Let this section explain the objectives or the motivation behind this project. 

# Getting Started (Azure Key Vault)

These steps outline the steps for configuring an environment where keys are managed by Azure Key Vault.

## Deploy Sample Application

ARM template with Managed Identity enabled

## Configure Azure Key Vault

### Create the Key Vault instance

TODO: ARM template with security

### Generate and Provision Column Master Key

The Column Master Key (CMK) is used to encrypt Column Encryption Keys (CEKs). In this example, a 2048-bit RSA key is generated and stored in the Azure Key Vault HSM with permissions to perform all key related operations. For details of various options for keys in Key Vault, please see the [documentation](https://docs.microsoft.com/en-us/cli/azure/keyvault/key?view=azure-cli-latest#az-keyvault-key-create) for `az keyvault key create`.

```bash
az keyvault key create --name trnieldemocmk --vault-name trniel-fledemo --kty RSA-HSM --protection hsm --size 2048
```



## Local Testing

### Create Service Principal

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

### Create Test Key Vault and Assign Permissions

Use the steps documented earlier to create a test Key Vault instance. When complted, use the following command to grant permissions to the Service Principal:

```bash

```


### Configure Authentication for Local Environment

For local testing of applicaitons that use Managed Identitiy for authorization to services, you must set these environment variables:

```cmd
set AzureServicesAuthConnectionString=RunAs=App;AppId={appId};TenantId={tenant};AppKey={password}
```

Replace the value of `{appId}`, `{tenant}`, and `{password}` with the output from the Service Principal creation command.

## Deployment

