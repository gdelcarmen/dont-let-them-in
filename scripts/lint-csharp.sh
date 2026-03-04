#!/usr/bin/env bash
set -euo pipefail

dotnet tool install --global dotnet-format --version 9.*
export PATH="$PATH:$HOME/.dotnet/tools"

dotnet format whitespace --folder Assets --verify-no-changes
dotnet format style --folder Assets --verify-no-changes --severity warn
