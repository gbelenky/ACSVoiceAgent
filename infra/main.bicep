targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment (used for resource naming)')
param environmentName string

@minLength(1)
@description('Primary location for all resources')
@allowed(['eastus2', 'swedencentral', 'australiaeast', 'northcentralus'])
@metadata({
  azd: {
    type: 'location'
  }
})
param location string

@description('ACS connection string (from existing ACS resource)')
@secure()
param acsConnectionString string

@description('Phone number for call transfers (E.164 format)')
param transferPhoneNumber string = '+4922180102503'

@description('Id of the user or app to assign application roles')
param principalId string = ''

@allowed(['gpt-4o-mini', 'gpt-4.1-mini', 'gpt-4o'])
param chatModelName string = 'gpt-4.1-mini'

// Hardcoded agent name — must match what agent_manager.py creates
var foundryAgentName = 'VoiceLiveAgent'

var abbrs = loadJsonContent('./abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = { 'azd-env-name': environmentName }

resource rg 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: '${abbrs.resourcesResourceGroups}${environmentName}'
  location: location
  tags: tags
}

// Azure AI Services with Foundry Project
module aiServices './app/ai/cognitive-services.bicep' = {
  name: 'aiServices'
  scope: rg
  params: {
    location: location
    tags: tags
    chatModelName: chatModelName
    aiServicesName: 'ai-${resourceToken}'
  }
}

// Web app
module web './app/web.bicep' = {
  name: 'web'
  scope: rg
  params: {
    name: '${abbrs.webSitesAppService}${resourceToken}'
    planName: '${abbrs.webServerFarms}${resourceToken}'
    location: location
    tags: tags
    acsConnectionString: acsConnectionString
    voiceLiveEndpoint: aiServices.outputs.aiServicesEndpoint
    foundryAgentName: foundryAgentName
    foundryProjectName: aiServices.outputs.aiFoundryProjectName
    transferPhoneNumber: transferPhoneNumber
    logAnalyticsWorkspaceName: '${abbrs.operationalInsightsWorkspaces}${resourceToken}'
    applicationInsightsName: '${abbrs.insightsComponents}${resourceToken}'
  }
}

// Assign Cognitive Services OpenAI User role to the developer (for local development and agent management)
var CognitiveServicesOpenAIUser = '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd'
module openaiRoleAssignmentDeveloper 'app/rbac/openai-access.bicep' = if (!empty(principalId)) {
  name: 'openaiRoleAssignmentDeveloper'
  scope: rg
  params: {
    openAIAccountName: aiServices.outputs.aiServicesName
    roleDefinitionId: CognitiveServicesOpenAIUser
    principalId: principalId
    principalType: 'User'
  }
}

// Assign Cognitive Services OpenAI User role to the App Service managed identity
module openaiRoleAssignmentApp 'app/rbac/openai-access.bicep' = {
  name: 'openaiRoleAssignmentApp'
  scope: rg
  params: {
    openAIAccountName: aiServices.outputs.aiServicesName
    roleDefinitionId: CognitiveServicesOpenAIUser
    principalId: web.outputs.identityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output AZURE_LOCATION string = location
output AZURE_RESOURCE_GROUP string = rg.name
output SERVICE_WEB_NAME string = web.outputs.name
output SERVICE_WEB_URI string = web.outputs.uri
output APPLICATIONINSIGHTS_CONNECTION_STRING string = web.outputs.applicationInsightsConnectionString
output APPLICATIONINSIGHTS_NAME string = web.outputs.applicationInsightsName
output AZURE_PORTAL_DASHBOARD_NAME string = web.outputs.dashboardName
output PROJECT_ENDPOINT string = aiServices.outputs.aiFoundryProjectEndpoint
output PROJECT_NAME string = aiServices.outputs.aiFoundryProjectName
output AI_SERVICES_NAME string = aiServices.outputs.aiServicesName
output CHAT_MODEL_DEPLOYMENT string = aiServices.outputs.chatDeploymentName
output FOUNDRY_AGENT_NAME string = foundryAgentName
