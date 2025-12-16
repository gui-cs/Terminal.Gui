#!/bin/bash

dotnet clean -c Debug
dotnet build -c Debug
dotnet publish -c Debug -r linux-x64 --self-contained
