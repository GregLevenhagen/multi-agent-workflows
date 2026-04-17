@description('Location for the managed identity')
param location string

@description('Managed identity name')
param identityName string

@description('Resource tags')
param tags object

resource managedIdentity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: identityName
  location: location
  tags: tags
}

output principalId string = managedIdentity.properties.principalId
output clientId string = managedIdentity.properties.clientId
output identityId string = managedIdentity.id
