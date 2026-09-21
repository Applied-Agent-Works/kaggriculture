#!/usr/bin/env bash
# Start Codex CLI with a fresh Microsoft Entra token for Foundry MCP.
#
# The token stays only in this process environment; it is not written to disk.
# Azure CLI refreshes it from the Azure sign-in already present on this machine.

set -euo pipefail

export FOUNDRY_MCP_TOKEN="$(
  az account get-access-token \
    --resource https://mcp.ai.azure.com \
    --query accessToken \
    --output tsv
)"

exec codex "$@"
