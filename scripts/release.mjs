#!/usr/bin/env node
// @ts-check
import { execSync } from 'child_process';

execSync('cd ./gaa && dotnet publish --runtime win-x64 -o ../releases/win-x64', { stdio: 'inherit' });
execSync('cd ./gaa && dotnet publish --runtime linux-x64 -o ../releases/linux-x64', { stdio: 'inherit' });
execSync('cd ./gaa && dotnet publish --runtime osx-arm64 -o ../releases/osx-arm64', { stdio: 'inherit' });

execSync('zip -r ./releases/win-x64-gaa.zip ./releases/win-x64', { stdio: 'inherit' });
execSync('zip -r ./releases/linux-x64-gaa.zip ./releases/linux-x64', { stdio: 'inherit' });
execSync('zip -r ./releases/osx-arm64-gaa.zip ./releases/osx-arm64', { stdio: 'inherit' });
