param region string = 'centralus'
param appPrefix string = 'mdedemo'
param storageContainerName string = 'userinfo'
param cekName string = 'mde-sensitive'
param userObjectId string
param tags object = {
  project: 'AzSecurePaaS'
  component: 'core'
}

// Storage Account
resource storageAccount 'Microsoft.Storage/storageAccounts@2019-06-01' = {
  name: uniqueString(resourceGroup().id)
  location: resourceGroup().location
  sku: {
    name: 'Standard_LRS'
    tier: 'Standard'
  }
  kind: 'StorageV2'
  properties: {
    supportsHttpsTrafficOnly: true
    accessTier: 'Hot'
  }
  tags: tags
}

resource container 'Microsoft.Storage/storageAccounts/blobServices/containers@2019-06-01' = {
  name: '${storageAccount.name}/default/${storageContainerName}'
  dependsOn: [
    storageAccount
  ]
}

// Function App


/*
Key Vault
*/
resource keyvault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: 'akv${uniqueString(resourceGroup().id)}' // AKV name must start with a letter
  location: resourceGroup().location
  properties: {
    tenantId: subscription().tenantId
    sku: {
      family: 'A'
      name: 'standard'
    }
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: userObjectId
        permissions: {
          keys: [
            'get'
            'list'
            'sign'
            'unwrapKey'
            'verify'
            'wrapKey'
          ]
          secrets: [
          ]
          certificates: [
          ]
        }
      }
    ]
    enabledForDeployment: true
    enabledForDiskEncryption: false
    enabledForTemplateDeployment: true
    softDeleteRetentionInDays: 7
    enableRbacAuthorization: false
    networkAcls: {
      ipRules: []
      virtualNetworkRules: []
    }
  }
}

// create key
resource key 'Microsoft.KeyVault/vaults/keys@2019-09-01' = {
  name: '${keyvault.name}/${cekName}'
  properties: {
    kty: 'RSA'
    keyOps: [
      'encrypt'
      'decrypt'
      'sign'
      'verify'
      'wrapKey'
      'unwrapKey'
    ]
    keySize: 4096
  }
}

/*
SQL SERVER AND DATABASE
*/
resource sqlServer 'Microsoft.Sql/servers@2019-06-01-preview' = {
  name: uniqueString(resourceGroup().id)
  location: resourceGroup().location
  properties: {
    administratorLogin: 'sqladmin'
    administratorLoginPassword: '${uniqueString(resourceGroup().id)}A!' 
    minimalTlsVersion: '1.2'
    version: '12.0'
  }
}

resource sqladmin 'Microsoft.Sql/servers/administrators@2019-06-01-preview' = {
  name: '${sqlServer.name}/ActiveDirectory'
  dependsOn: [
    sqlServer
  ]
  properties: {
    administratorType: 'ActiveDirectory'
    login: 'sqladmin'
    sid: userObjectId
    tenantId: subscription().tenantId
  }
}

resource db 'Microsoft.Sql/servers/databases@2020-02-02-preview' = {
  name: '${sqlServer.name}/testdb'
  location: resourceGroup().location
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    collation: 'SQL_Latin1_General_CP1_CI_AS'
  }
}