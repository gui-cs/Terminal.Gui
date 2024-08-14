#!/bin/bash

dotnet clean
dotnet build
dotnet publish -c Release -r osx-x64 --self-contained
