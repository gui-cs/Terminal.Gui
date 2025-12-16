#!/bin/bash

dotnet clean -c Release
dotnet build -c Release
dotnet publish -c Release -r linux-x64 --self-contained
