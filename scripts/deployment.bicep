param storageContainerName string = 'userdata'
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

/*
Event Hub
*/

resource eventHubNamespace 'Microsoft.EventHub/namespaces@2021-06-01-preview' = {
  name: 'mdedemo-${uniqueString(resourceGroup().id)}' // event hub namespaces must begin with a letter
  location: resourceGroup().location
  sku: {
    name: 'Standard'
    tier: 'Standard'
    capacity: 1
  }
  properties: {
    kafkaEnabled: true
    isAutoInflateEnabled: false
    maximumThroughputUnits: 0
  }
}


resource eventHub 'Microsoft.EventHub/namespaces/eventhubs@2021-06-01-preview' = {
  name: '${eventHubNamespace.name}/userdata'
  properties: {
    messageRetentionInDays: 1
    partitionCount: 1
  }
}

/*
Function App
*/

resource appServicePlan 'Microsoft.Web/serverFarms@2020-06-01' = {
  name: '${uniqueString(resourceGroup().id)}-plan'
  location: resourceGroup().location
  kind: 'elastic'
  sku: {
    name: 'EP1'
    tier: 'ElasticPremium'
  }
  properties: {
    maximumElasticWorkerCount: 20
  }
  tags: tags
}

resource functionApp 'Microsoft.Web/sites@2020-06-01' = {
  name: '${uniqueString(resourceGroup().id)}-functionapp'
  location: resourceGroup().location
  kind: 'functionapp'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    enabled: true
    hostNameSslStates: [
      {
        name: '${uniqueString(resourceGroup().id)}.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
      {
        name: '${uniqueString(resourceGroup().id)}.scm.azurewebsites.net'
        sslState: 'Disabled'
        hostType: 'Standard'
      }
    ]
    serverFarmId: appServicePlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
        }
        {
          name: 'WEBSITE_CONTENTAZUREFILECONNECTIONSTRING'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${listKeys(storageAccount.id, storageAccount.apiVersion).keys[0].value}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~3'
        }
        {
          name: 'WEBSITE_NODE_DEFAULT_VERSION'
          value: '~12'
        }
      ]
    }
    httpsOnly: true
  }
  tags: tags
}

/*
Key Vault
*/

resource keyvault 'Microsoft.KeyVault/vaults@2019-09-01' = {
  name: 'akv${uniqueString(resourceGroup().id)}' // AKV name must start with a letter
  location: resourceGroup().location
  dependsOn: [
    functionApp
  ]
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
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
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
