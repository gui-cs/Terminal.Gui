#!/bin/bash

dotnet clean
dotnet build
dotnet publish -c Debug -r linux-x64 --self-contained
