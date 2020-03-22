vaultName="your keyvault name"
vaultRgName="resource group name"
location="key vault location. Ex: centralus"
keyName="name of your column master key"
applicationId="service principal appId"
functionPrincipalId="principalId for the Functions managed identity"

# create Key Vault and provision the column master key
az keyvault create --location $location --name $vaultName --resource-group $vaultRgName --enable-purge-protection true --enable-soft-delete true
az keyvault key create --name $keyName --vault-name $vaultName --kty RSA-HSM --protection hsm --size 2048

# assign permissions for key management (wrap CEKs)
az keyvault set-policy --name $vaultName --key-permissions get, list, sign, unwrapKey, verify, wrapKey --resource-group $vaultRgName --spn $applicationId

# assign permissions for using keys (unwrap CEKs)
az keyvault set-policy --name $vaultName --key-permissions get, list, sign, unwrapKey, verify, wrapKey --resource-group $vaultRgName --spn $applicationId

