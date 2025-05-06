targetScope = 'resourceGroup'

@description('The Azure region into which the resources should be deployed.')
param location string = 'northeurope'

@description('The prefix to use for all resources.')
param loc string = 'ne'

@allowed([
  'test'
  'prod'
])
param env string

@description('Tag of container image to deploy')
param version string

resource kv 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${loc}-heroes-${env}'
  location: location
  properties: {
    sku: {
      family: 'A'
      name: 'standard'
    }
    tenantId: subscription().tenantId
    enabledForTemplateDeployment: true
    enableSoftDelete: true
    enableRbacAuthorization: true
  }
}

resource cosmosAccount 'Microsoft.DocumentDB/databaseAccounts@2024-02-15-preview' = {
  name: 'cosno-${loc}-${env}-001'
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    publicNetworkAccess: 'Enabled'
    enableFreeTier: false
    databaseAccountOfferType: 'Standard'
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
      maxIntervalInSeconds: 5
      maxStalenessPrefix: 100
    }
    locations: [
      {
        locationName: location
        failoverPriority: 0
        isZoneRedundant: false
      }
    ]
    capabilities: [
      {
        name: 'EnableServerless'
      }
    ]
  }
}

resource cosmosDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2021-06-15' = {
  parent: cosmosAccount
  name: 'cosmos-heroes'
  properties: {
    resource: {
      id: 'cosmos-heroes'
    }
  }
}

resource containerHeroes 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2021-06-15' = {
  parent: cosmosDatabase
  name: 'heroes'
  properties: {
    resource: {
      id: 'heroes'
      partitionKey: {
        paths: [
          '/__partitionKey'
        ]
      }
    }
  }
}

resource kvConnectionStringSecret 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: kv
  name: 'heroes-db-connectionstring'
  properties: {
    value: cosmosAccount.listConnectionStrings().connectionStrings[0].connectionString
  }
}

resource rgShared 'Microsoft.Resources/resourceGroups@2025-03-01' existing {
  name: 'rg-heroes-shared'
  scope: subscription()
}

var acrResourceToken = uniqueString(rgShared.id)
resource acr 'Microsoft.ContainerRegistry/registries@2020-11-01-preview' existing = {
  name: 'acr${acrResourceToken}'
  scope: rgShared
}

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${loc}-heroes-${env}'
  location: location
}

resource secretReaderRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(kv.id, managedIdentity.id)
  scope: kv
  properties: {
    principalId: managedIdentity.properties.principalId
    roleDefinitionId: resourceId('Microsoft.Authorization/roleDefinitions', '4633458b-17de-408a-b874-0445c86b69e6') // Key Vault Secrets User
  }
}

module acrPullRoleAssignment 'modules/acrPullRoleAssignment.bicep' = {
  name: 'acrPullRoleAssignment'
  scope: resourceGroup('rg-heroes-shared')
  params: {
    principalId: managedIdentity.properties.principalId
    acrName: acr.name
  }
}

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: 'log-${loc}-heroes-${env}'
  location: location
  properties: {
    retentionInDays: 30
    sku: {
      name: 'PerGB2018'
    }
  }
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'ai-${loc}-heroes-${env}'
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource containerAppEnvironment 'Microsoft.App/managedEnvironments@2023-11-02-preview' = {
  name: 'cae-${loc}-heroes-${env}'
  location: location
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspace.id, '2020-03-01-preview').customerId
        sharedKey: listKeys(logAnalyticsWorkspace.id, '2020-03-01-preview').primarySharedKey
      }
    }
  }
}

resource containerappApi 'Microsoft.App/containerApps@2023-11-02-preview' = {
  name: 'ca-${loc}-heroes-api-${env}'
  location: location
  dependsOn: [
    acrPullRoleAssignment
  ]
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentity.id}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppEnvironment.id
    configuration: {
      activeRevisionsMode: 'Single'
      ingress: {
        external: true
        targetPort: 8080
      }
      registries: [
        {
          identity: managedIdentity.id
          server: acr.properties.loginServer
        }
      ]
      secrets: [
        {
          name: 'heroes-db-connection-string'
          keyVaultUrl: kvConnectionStringSecret.properties.secretUri
          identity: managedIdentity.id
        }
      ]
    }
    template: {
      revisionSuffix: replace('${version}', '.', '-')
      containers: [
        {
          image: '${acr.name}.azurecr.io/heroes/api:${version}'
          name: 'heroes-api'
          resources: {
            cpu: json('0.25')
            memory: '0.5Gi'
          }
          env: [
            {
              name: 'ConnectionStrings__HeroesDbContext'
              secretRef: 'heroes-db-connection-string'
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 1
      }
    }
  }
}
