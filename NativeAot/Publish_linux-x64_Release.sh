#!/bin/bash

dotnet clean
dotnet build
dotnet publish -c Release -r linux-x64 --self-contained
