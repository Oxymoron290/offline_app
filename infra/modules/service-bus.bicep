@description('Name of the Service Bus namespace')
param namespaceName string

@description('Location for the resource')
param location string = resourceGroup().location

@description('SKU for the Service Bus')
@allowed(['Basic', 'Standard', 'Premium'])
param skuName string = 'Standard'

@description('Tags for the resource')
param tags object = {}

resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2022-10-01-preview' = {
  name: namespaceName
  location: location
  tags: tags
  sku: {
    name: skuName
    tier: skuName
  }
}

resource mediaQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'media-processing'
  properties: {
    lockDuration: 'PT5M'
    maxSizeInMegabytes: 1024
    requiresDuplicateDetection: false
    requiresSession: false
    defaultMessageTimeToLive: 'P14D'
    deadLetteringOnMessageExpiration: true
    maxDeliveryCount: 5
    enablePartitioning: false
  }
}

resource deadLetterQueue 'Microsoft.ServiceBus/namespaces/queues@2022-10-01-preview' = {
  parent: serviceBusNamespace
  name: 'media-processing-dlq'
  properties: {
    lockDuration: 'PT5M'
    maxSizeInMegabytes: 1024
    defaultMessageTimeToLive: 'P30D'
    maxDeliveryCount: 1
    enablePartitioning: false
  }
}

var serviceBusEndpoint = '${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey'

output namespaceName string = serviceBusNamespace.name
output namespaceId string = serviceBusNamespace.id
output connectionString string = listKeys(serviceBusEndpoint, serviceBusNamespace.apiVersion).primaryConnectionString
output queueName string = mediaQueue.name
