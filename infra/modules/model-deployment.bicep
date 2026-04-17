@description('AI Services account name')
param aiServicesName string

@description('Model name to deploy')
param modelName string = 'gpt-4o'

resource aiServices 'Microsoft.CognitiveServices/accounts@2024-10-01' existing = {
  name: aiServicesName
}

resource modelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2024-10-01' = {
  parent: aiServices
  name: modelName
  sku: {
    name: 'GlobalStandard'
    capacity: 30
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: modelName
      version: '2024-11-20'
    }
  }
}

output deploymentName string = modelDeployment.name
