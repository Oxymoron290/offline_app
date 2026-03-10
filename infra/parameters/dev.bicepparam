using '../main.bicep'

param environment = 'dev'
param baseName = 'blazorpwa'
param sqlAdminLogin = 'sqladmin'
param appServicePlanSku = 'B1'
param sqlDatabaseSku = 'Basic'
param serviceBusSku = 'Standard'
param storageSku = 'Standard_LRS'
