@description('Name of the App Service Plan')
param name string

@description('Location for the resource')
param location string = resourceGroup().location

@description('SKU name for the App Service Plan')
@allowed(['B1', 'B2', 'S1', 'S2', 'P1v3', 'P2v3'])
param skuName string = 'B1'

@description('Tags for the resource')
param tags object = {}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

output id string = appServicePlan.id
output name string = appServicePlan.name
