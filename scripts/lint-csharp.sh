#!/usr/bin/env bash
set -euo pipefail

if ! dotnet tool update --global dotnet-format >/dev/null 2>&1; then
  dotnet tool install --global dotnet-format
fi
export PATH="$PATH:$HOME/.dotnet/tools"

mkdir -p .lint
cat > .lint/Lint.csproj <<'CSEND'
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
  </PropertyGroup>
  <ItemGroup>
    <Compile Include="../Assets/**/*.cs" />
  </ItemGroup>
</Project>
CSEND

dotnet format whitespace .lint/Lint.csproj --verify-no-changes --no-restore --include ../Assets
