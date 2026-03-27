param name string
param planName string
param location string = resourceGroup().location
param tags object = {}

@secure()
param acsConnectionString string
@secure()
param azureVoiceLiveApiKey string
param azureVoiceLiveEndpoint string
param voiceLiveModel string
param transferPhoneNumber string

param logAnalyticsWorkspaceName string
param applicationInsightsName string

resource logAnalyticsWorkspace 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: logAnalyticsWorkspaceName
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

resource applicationInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: applicationInsightsName
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalyticsWorkspace.id
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: planName
  location: location
  tags: tags
  sku: {
    name: 'B1'
    tier: 'Basic'
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource web 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|8.0'
      alwaysOn: true
      webSocketsEnabled: true
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        { name: 'AcsConnectionString', value: acsConnectionString }
        { name: 'AzureVoiceLiveApiKey', value: azureVoiceLiveApiKey }
        { name: 'AzureVoiceLiveEndpoint', value: azureVoiceLiveEndpoint }
        { name: 'VoiceLiveModel', value: voiceLiveModel }
        { name: 'TransferPhoneNumber', value: transferPhoneNumber }
        { name: 'DevTunnelUri', value: 'https://${name}.azurewebsites.net' }
        { name: 'APPLICATIONINSIGHTS_CONNECTION_STRING', value: applicationInsights.properties.ConnectionString }
        { name: 'ApplicationInsightsAgent_EXTENSION_VERSION', value: '~3' }
      ]
    }
  }
}

output name string = web.name
output uri string = 'https://${web.properties.defaultHostName}'
output applicationInsightsConnectionString string = applicationInsights.properties.ConnectionString
output applicationInsightsName string = applicationInsights.name
