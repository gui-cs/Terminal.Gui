#!/bin/bash

dotnet clean -c Debug
dotnet build -c Debug
dotnet publish -c Debug -r osx-x64 --self-contained
