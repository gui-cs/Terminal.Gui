#!/bin/bash

# Step 1: Build and pack Terminal.Gui
dotnet pack ../Terminal.Gui/Terminal.Gui.csproj --configuration Release --output ../local_packages

# Step 2: Restore NativeAot with the new package
dotnet restore ./NativeAot.csproj --source ./local_packages

# Step 3: Build NativeAot
dotnet build ./NativeAot.csproj --configuration Release
