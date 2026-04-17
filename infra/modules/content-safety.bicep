@description('Location for Content Safety resource')
param location string

@description('Content Safety resource name')
@minLength(2)
@maxLength(64)
param contentSafetyName string

@description('Resource tags')
param tags object

@description('Log Analytics workspace ID for diagnostic settings (optional)')
param logAnalyticsWorkspaceId string = ''

resource contentSafety 'Microsoft.CognitiveServices/accounts@2024-10-01' = {
  name: contentSafetyName
  location: location
  tags: tags
  kind: 'ContentSafety'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: contentSafetyName
    publicNetworkAccess: 'Enabled'
  }
}

// Diagnostic settings — forward platform logs and metrics to Log Analytics
resource diagnosticSettings 'Microsoft.Insights/diagnosticSettings@2021-05-01-preview' = if (!empty(logAnalyticsWorkspaceId)) {
  name: '${contentSafetyName}-diagnostics'
  scope: contentSafety
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

output endpoint string = contentSafety.properties.endpoint
output name string = contentSafety.name
