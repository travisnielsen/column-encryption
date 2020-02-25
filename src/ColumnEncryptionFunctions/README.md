# Column Encryption Azure Functions

This is a set of Azure Functions that utilize the SQL Always Encrypted libraries to encrypt and decrypt column (field-level) data for the following:

* `ProtectCsv`: Blob trigger function that takes a CSV file as input, performs encryption based on an input YAML config file, and writes an output file in a separate blob container.
* `CopyToSql`: Blob trigger function that takes in a CSV file that contains encrypted columns and inserts records into an Azure SQL Database without first decrypting the data. This showcases movement of encrypted data across platforms that may not natively support Always Encrypted.

## Prerequisites

Be sure your environment meets the following conditions:

1. You have completed the [deployment and configuration steps](../../docs/configure-azure.md) for Azure Key Vault and a Service Principal within your Azure Environment
2. [.Net Core 3.1 SDK](https://dotnet.microsoft.com/download/dotnet-core/3.1) installed
3. Visual Studio Code with the [Azure Functions extension](https://marketplace.visualstudio.com/items?itemName=ms-azuretools.vscode-azurefunctions) configured

## Setup (local development)

