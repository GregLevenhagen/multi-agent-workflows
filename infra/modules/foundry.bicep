@description('Location for the AI Foundry resources')
param location string

@description('Project name')
param projectName string

@description('Abbreviated project name for length-constrained resources (max 6 chars)')
@maxLength(6)
param abbrev string

@description('Unique resource token')
param resourceToken string

@description('Resource tags')
param tags object

@description('Managed identity principal ID for RBAC')
param identityPrincipalId string

@description('Key Vault resource ID (required dependency for AI Hub)')
param keyVaultId string

@description('Storage Account resource ID (required dependency for AI Hub)')
param storageAccountId string

@description('Application Insights resource ID for Hub telemetry')
param appInsightsId string

@description('Log Analytics workspace ID for diagnostic settings (optional)')
param logAnalyticsWorkspaceId string = ''

// AI Services account (provides the model endpoint for agent orchestration)
resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: '${projectName}-ai-${resourceToken}'
  location: location
  tags: tags
  kind: 'AIServices'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: '${projectName}-ai-${resourceToken}'
    publicNetworkAccess: 'Enabled'
  }
}

// Diagnostic settings for AI Services — forward API logs and metrics to Log Analytics
resource aiServicesDiagnostics 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${projectName}-ai-diagnostics'
  scope: aiServices
  properties: {
    workspaceId: logAnalyticsWorkspaceId
    logs: [
      {
        categoryGroup: 'allLogs'
        enabled: true
      }
    ]
    metrics: [
      {
        category: 'AllMetrics'
        enabled: true
      }
    ]
  }
}

// AI Hub — the top-level Foundry workspace that organizes projects, connections, and compute
resource aiHub 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: '${abbrev}-hub-${resourceToken}'
  location: location
  tags: tags
  kind: 'Hub'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    friendlyName: '${projectName} AI Hub'
    description: 'AI Foundry Hub for the Sales-to-Signature multi-agent pipeline'
    keyVault: keyVaultId
    storageAccount: storageAccountId
    applicationInsights: appInsightsId
    publicNetworkAccess: 'Enabled'
  }
}

// AI Project — scoped workspace for the agent pipeline, connected to the Hub
resource aiProject 'Microsoft.MachineLearningServices/workspaces@2024-10-01' = {
  name: '${abbrev}-proj-${resourceToken}'
  location: location
  tags: tags
  kind: 'Project'
  identity: {
    type: 'SystemAssigned'
  }
  sku: {
    name: 'Basic'
    tier: 'Basic'
  }
  properties: {
    friendlyName: 'Sales-to-Signature Pipeline'
    description: 'Multi-agent workflow: RFP intake through contract approval'
    hubResourceId: aiHub.id
    publicNetworkAccess: 'Enabled'
  }
}

// Connection from AI Project to the AI Services endpoint for model access
resource aiServicesConnection 'Microsoft.MachineLearningServices/workspaces/connections@2024-10-01' = {
  parent: aiHub
  name: '${projectName}-ai-connection'
  properties: {
    category: 'AzureOpenAI'
    target: aiServices.properties.endpoint
    authType: 'AAD'
    metadata: {
      ApiType: 'Azure'
      ResourceId: aiServices.id
    }
  }
}

// Azure AI User role assignment on AI Services for managed identity
resource aiUserRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiServices.id, identityPrincipalId, 'Azure AI User')
  scope: aiServices
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'a97b65f3-24c7-4388-baec-2e87135dc908')
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Contributor role on AI Project for managed identity (agent registration)
resource projectContributorRole 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(aiProject.id, identityPrincipalId, 'Contributor')
  scope: aiProject
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', 'b24988ac-6180-42a0-ab88-20f7382dd24c')
    principalId: identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output aiServicesName string = aiServices.name
output aiServicesEndpoint string = aiServices.properties.endpoint
output hubName string = aiHub.name
output projectName string = aiProject.name
output projectEndpoint string = aiProject.properties.discoveryUrl
