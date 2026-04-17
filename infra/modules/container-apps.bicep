@description('Location for Container Apps resources')
param location string

@description('Container Apps Environment name')
@minLength(2)
@maxLength(60)
param environmentName string

@description('Container App name')
@minLength(2)
@maxLength(32)
param appName string

@description('Resource tags')
param tags object

@description('Container Registry login server (e.g., myregistry.azurecr.io)')
param containerRegistryLoginServer string

@description('Container image name and tag (e.g., sales-to-signature:latest). Defaults to a public placeholder for initial provisioning before first deploy.')
param containerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Managed identity resource ID for ACR pull and Azure auth')
param managedIdentityId string

@description('Managed identity client ID for DefaultAzureCredential')
param managedIdentityClientId string

@description('Azure AI Project endpoint')
param aiProjectEndpoint string

@description('Model deployment name')
param modelDeploymentName string = 'gpt-4o'

@description('Content Safety endpoint')
param contentSafetyEndpoint string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('Log Analytics workspace ID for Container Apps Environment')
param logAnalyticsWorkspaceId string

// Container Apps Environment — shared hosting environment with Log Analytics integration
resource containerAppsEnvironment 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: environmentName
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: reference(logAnalyticsWorkspaceId, '2023-09-01').customerId
        sharedKey: listKeys(logAnalyticsWorkspaceId, '2023-09-01').primarySharedKey
      }
    }
  }
}

// Container App — the hosted agent running as a serverless container
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: appName
  location: location
  tags: union(tags, { 'azd-service-name': 'agents' })
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: {
      '${managedIdentityId}': {}
    }
  }
  properties: {
    managedEnvironmentId: containerAppsEnvironment.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
      }
      registries: [
        {
          server: containerRegistryLoginServer
          identity: managedIdentityId
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'agent'
          image: contains(containerImage, '/') ? containerImage : '${containerRegistryLoginServer}/${containerImage}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            { name: 'AZURE_AI_PROJECT_ENDPOINT', value: aiProjectEndpoint }
            { name: 'AZURE_AI_MODEL_DEPLOYMENT_NAME', value: modelDeploymentName }
            { name: 'AZURE_CONTENT_SAFETY_ENDPOINT', value: contentSafetyEndpoint }
            { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: appInsightsConnectionString }
            { name: 'AZURE_CLIENT_ID', value: managedIdentityClientId }
            { name: 'DOTNET_ENVIRONMENT', value: 'production' }
            { name: 'DATA_DIRECTORY', value: '/app/data' }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: {
                path: '/healthz'
                port: 8080
              }
              periodSeconds: 30
            }
            {
              type: 'Readiness'
              httpGet: {
                path: '/healthz'
                port: 8080
              }
              initialDelaySeconds: 5
              periodSeconds: 10
            }
          ]
        }
      ]
      scale: {
        minReplicas: 0
        maxReplicas: 3
        rules: [
          {
            name: 'http-scaling'
            http: {
              metadata: {
                concurrentRequests: '10'
              }
            }
          }
        ]
      }
    }
  }
}

output fqdn string = containerApp.properties.configuration.ingress.fqdn
output appUrl string = 'https://${containerApp.properties.configuration.ingress.fqdn}'
