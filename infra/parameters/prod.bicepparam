using '../main.bicep'

param environment = 'prod'
param baseName = 'blazorpwa'
param sqlAdminLogin = 'sqladmin'
param appServicePlanSku = 'P1v3'
param sqlDatabaseSku = 'S2'
param serviceBusSku = 'Premium'
param storageSku = 'Standard_RAGRS'
