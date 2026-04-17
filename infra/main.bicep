targetScope = 'resourceGroup'

@description('Primary location for all resources. Hosted Agents supported in East US and East US 2.')
@allowed([
  'eastus'
  'eastus2'
])
param location string = 'eastus2'

@description('Project name used for resource naming')
@minLength(3)
@maxLength(20)
param projectName string = 'salestosignature'

@description('Model deployment name')
param modelName string = 'gpt-4o'

@description('Environment name (dev, staging, prod)')
@allowed([
  'dev'
  'staging'
  'prod'
])
param environmentName string = 'dev'

var resourceToken = toLower(uniqueString(resourceGroup().id, projectName))
var abbrev = take(projectName, 6) // short prefix for length-constrained resources
var tags = {
  project: projectName
  environment: environmentName
  'managed-by': 'azd'
}

// Managed Identity with RBAC
module identity 'modules/managed-identity.bicep' = {
  name: 'managed-identity'
  params: {
    location: location
    identityName: '${projectName}-identity-${resourceToken}'
    tags: tags
  }
}

// Key Vault (required by AI Hub for secrets storage)
module keyVault 'modules/key-vault.bicep' = {
  name: 'key-vault'
  params: {
    location: location
    keyVaultName: '${abbrev}-kv-${resourceToken}'
    tags: tags
    identityPrincipalId: identity.outputs.principalId
  }
}

// Storage Account (required by AI Hub for workspace data)
module storage 'modules/storage.bicep' = {
  name: 'storage'
  params: {
    location: location
    storageAccountName: '${abbrev}st${resourceToken}'
    tags: tags
    identityPrincipalId: identity.outputs.principalId
  }
}

// Application Insights + Log Analytics
module appInsights 'modules/app-insights.bicep' = {
  name: 'app-insights'
  params: {
    location: location
    appInsightsName: '${projectName}-insights-${resourceToken}'
    logAnalyticsName: '${projectName}-logs-${resourceToken}'
    tags: tags
  }
}

// Azure AI Foundry (Hub + Project + AI Services + Connection)
module foundry 'modules/foundry.bicep' = {
  name: 'foundry'
  params: {
    location: location
    projectName: projectName
    abbrev: abbrev
    resourceToken: resourceToken
    tags: tags
    identityPrincipalId: identity.outputs.principalId
    keyVaultId: keyVault.outputs.keyVaultId
    storageAccountId: storage.outputs.storageAccountId
    appInsightsId: appInsights.outputs.appInsightsId
    logAnalyticsWorkspaceId: appInsights.outputs.logAnalyticsWorkspaceId
  }
}

// Model Deployment
module modelDeployment 'modules/model-deployment.bicep' = {
  name: 'model-deployment'
  params: {
    aiServicesName: foundry.outputs.aiServicesName
    modelName: modelName
  }
}

// Container Registry
module acr 'modules/acr.bicep' = {
  name: 'acr'
  params: {
    location: location
    registryName: '${projectName}acr${resourceToken}'
    tags: tags
    identityPrincipalId: identity.outputs.principalId
  }
}

// Content Safety
module contentSafety 'modules/content-safety.bicep' = {
  name: 'content-safety'
  params: {
    location: location
    contentSafetyName: '${projectName}-safety-${resourceToken}'
    tags: tags
    logAnalyticsWorkspaceId: appInsights.outputs.logAnalyticsWorkspaceId
  }
}

// Container Apps (hosts the agents service)
module containerApps 'modules/container-apps.bicep' = {
  name: 'container-apps'
  params: {
    location: location
    environmentName: '${projectName}-cae-${resourceToken}'
    appName: '${abbrev}-app-${resourceToken}'
    tags: tags
    containerRegistryLoginServer: acr.outputs.loginServer
    managedIdentityId: identity.outputs.identityId
    managedIdentityClientId: identity.outputs.clientId
    aiProjectEndpoint: foundry.outputs.aiServicesEndpoint
    modelDeploymentName: modelName
    contentSafetyEndpoint: contentSafety.outputs.endpoint
    appInsightsConnectionString: appInsights.outputs.connectionString
    logAnalyticsWorkspaceId: appInsights.outputs.logAnalyticsWorkspaceId
  }
}

// Outputs for azd and local development
output AZURE_AI_PROJECT_ENDPOINT string = foundry.outputs.aiServicesEndpoint
output AZURE_AI_MODEL_DEPLOYMENT_NAME string = modelName
output AZURE_CONTENT_SAFETY_ENDPOINT string = contentSafety.outputs.endpoint
output APPLICATIONINSIGHTS_CONNECTION_STRING string = appInsights.outputs.connectionString
output AZURE_CONTAINER_REGISTRY_ENDPOINT string = acr.outputs.loginServer
output MANAGED_IDENTITY_CLIENT_ID string = identity.outputs.clientId
output AZURE_KEY_VAULT_URI string = keyVault.outputs.keyVaultUri
output AI_HUB_NAME string = foundry.outputs.hubName
output AI_PROJECT_NAME string = foundry.outputs.projectName
output CONTAINER_APP_URL string = containerApps.outputs.appUrl
