#!/bin/bash

dotnet clean
dotnet build
dotnet publish -c Debug -r osx-x64 --self-contained
