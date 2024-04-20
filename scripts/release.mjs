#!/usr/bin/env node
// @ts-check
import { execSync } from 'child_process';

execSync('cd ./gaa && dotnet publish --runtime win-x64 -o ../releases/win-x64');
execSync('cd ./gaa && dotnet publish --runtime linux-x64 -o ../releases/linux-x64');
execSync('cd ./gaa && dotnet publish --runtime osx-arm64 -o ../releases/osx-arm64');