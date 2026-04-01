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

resource dashboard 'Microsoft.Portal/dashboards@2020-09-01-preview' = {
  name: '${name}-dashboard'
  location: location
  tags: union(tags, { 'hidden-title': 'ACS Voice Agent Dashboard' })
  properties: {
    lenses: [
      {
        order: 0
        parts: [
          {
            position: { x: 0, y: 0, rowSpan: 4, colSpan: 6 }
            metadata: {
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
              inputs: [
                {
                  name: 'options'
                  value: {
                    chart: {
                      title: 'Server Requests'
                      metrics: [
                        {
                          resourceMetadata: { id: applicationInsights.id }
                          name: 'requests/count'
                          aggregationType: 7
                          metricVisualization: { displayName: 'Server requests' }
                        }
                      ]
                      timespan: { relative: { duration: 86400000 } }
                      visualization: { chartType: 2 }
                    }
                  }
                }
              ]
            }
          }
          {
            position: { x: 6, y: 0, rowSpan: 4, colSpan: 6 }
            metadata: {
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
              inputs: [
                {
                  name: 'options'
                  value: {
                    chart: {
                      title: 'Server Response Time'
                      metrics: [
                        {
                          resourceMetadata: { id: applicationInsights.id }
                          name: 'requests/duration'
                          aggregationType: 4
                          metricVisualization: { displayName: 'Server response time' }
                        }
                      ]
                      timespan: { relative: { duration: 86400000 } }
                      visualization: { chartType: 2 }
                    }
                  }
                }
              ]
            }
          }
          {
            position: { x: 0, y: 4, rowSpan: 4, colSpan: 6 }
            metadata: {
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
              inputs: [
                {
                  name: 'options'
                  value: {
                    chart: {
                      title: 'Failed Requests'
                      metrics: [
                        {
                          resourceMetadata: { id: applicationInsights.id }
                          name: 'requests/failed'
                          aggregationType: 7
                          metricVisualization: { displayName: 'Failed requests' }
                        }
                      ]
                      timespan: { relative: { duration: 86400000 } }
                      visualization: { chartType: 2 }
                    }
                  }
                }
              ]
            }
          }
          {
            position: { x: 6, y: 4, rowSpan: 4, colSpan: 6 }
            metadata: {
              type: 'Extension/HubsExtension/PartType/MonitorChartPart'
              inputs: [
                {
                  name: 'options'
                  value: {
                    chart: {
                      title: 'Server Exceptions'
                      metrics: [
                        {
                          resourceMetadata: { id: applicationInsights.id }
                          name: 'exceptions/count'
                          aggregationType: 7
                          metricVisualization: { displayName: 'Exceptions' }
                        }
                      ]
                      timespan: { relative: { duration: 86400000 } }
                      visualization: { chartType: 2 }
                    }
                  }
                }
              ]
            }
          }
        ]
      }
    ]
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
output dashboardName string = dashboard.name
