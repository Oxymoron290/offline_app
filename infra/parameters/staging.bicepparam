using '../main.bicep'

param environment = 'staging'
param baseName = 'blazorpwa'
param sqlAdminLogin = 'sqladmin'
param appServicePlanSku = 'S1'
param sqlDatabaseSku = 'S0'
param serviceBusSku = 'Standard'
param storageSku = 'Standard_GRS'
