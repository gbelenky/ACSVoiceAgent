targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (used for resource naming)')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
param location string

@description('ACS connection string (from existing ACS resource)')
@secure()
param acsConnectionString string

@description('Voice Live API key (from existing AI Services resource)')
@secure()
param voiceLiveApiKey string

@description('Voice Live endpoint (from existing AI Services resource)')
param voiceLiveEndpoint string

@description('Voice Live model deployment name')
param voiceLiveModel string = 'gpt-realtime-mini'

@description('Phone number for call transfers (E.164 format)')
@secure()
param transferPhoneNumber string

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

module web './app/web.bicep' = {
  name: 'web'
  scope: rg
  params: {
    name: '${abbrs.webSitesAppService}${resourceToken}'
    planName: '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    acsConnectionString: acsConnectionString
    voiceLiveApiKey: voiceLiveApiKey
    voiceLiveEndpoint: voiceLiveEndpoint
    voiceLiveModel: voiceLiveModel
    transferPhoneNumber: transferPhoneNumber
    logAnalyticsWorkspaceName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output SERVICE_WEB_NAME string = web.outputs.name
output SERVICE_WEB_URI string = web.outputs.uri
output APPLICATIONINSIGHTS_CONNECTION_STRING string = web.outputs.applicationInsightsConnectionString
output APPLICATIONINSIGHTS_NAME string = web.outputs.applicationInsightsName
output AZURE_PORTAL_DASHBOARD_NAME string = web.outputs.dashboardName
