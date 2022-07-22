terraform {
  required_version = ">= 0.14.9"

  required_providers {
    azurerm = {
      source  = "hashicorp/azurerm"
      version = "~> 2.65"
    }
  }

  backend "local" {
    path = "local.tfstate"
  }
}

provider "azurerm" {
  features {}
}

resource "azurerm_resource_group" "rg" {
  name     = "rg-${var.environment_name}-weu-envencdemo"
  location = "westeurope"
  tags     = {
      "owner" = var.environment_owner
  }
}

resource "azurerm_storage_account" "storage" {
  name                     = "sa${var.environment_name}weuenvencdemo"
  resource_group_name      = azurerm_resource_group.rg.name
  location                 = azurerm_resource_group.rg.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  is_hns_enabled           =  false
  tags      = {
      "owner" = var.environment_owner
  }
}

data "azurerm_client_config" "current" {}

resource "azurerm_key_vault" "keyvault" {
  name                = "kv-${var.environment_name}-weu-envencdemo"
  location            = azurerm_resource_group.rg.location
  resource_group_name = azurerm_resource_group.rg.name
  tenant_id           = data.azurerm_client_config.current.tenant_id
  sku_name            = "standard"
  tags      = {
      "owner" = var.environment_owner
  }
}

resource "azurerm_key_vault_access_policy" "client" {
  key_vault_id = azurerm_key_vault.keyvault.id
  tenant_id = data.azurerm_client_config.current.tenant_id
  object_id = data.azurerm_client_config.current.object_id

  key_permissions = [
    "create",
    "get",
    "purge",
    "recover",
    "delete"
  ]

  secret_permissions = [
    "set",
    "get",
    "purge",
    "recover",
    "delete"
  ]

  lifecycle {
    create_before_destroy = true
  }
}