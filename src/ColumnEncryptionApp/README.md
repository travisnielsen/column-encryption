# Column Encryption Sample Application

This is a sample console application written in .Net Core that uses the SQL Always Encrypted libraries to encrypt and decrypt columns within data files.

## Setup

It is assumed you have configured your Azure Environment as detailed here. and  Azure Key Vault instance and provisioned it with a key, which will be used as the Column Master Key (CMK) for encrypting and decrypting Column Encryption Keys (CEK). It is also assumed you have defined a service principal to be used for testing this application in a development environment, such as a laptop running MacOS or Windows 10.

### Service Principal Authentication

For local testing of applicaitons that use Managed Identitiy for authorization to services, you must set these environment variables:

```cmd
set AzureServicesAuthConnectionString=RunAs=App;AppId={appId};TenantId={tenant};AppKey={password}
```

Replace the value of `{appId}`, `{tenant}`, and `{password}` with the output from the Service Principal creation command.
