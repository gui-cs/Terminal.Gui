# Builds the Terminal.gui API documentation using docfx

dotnet build --configuration Release ../Terminal.sln

rm ../docs -Recurse -Force

$env:DOCFX_SOURCE_BRANCH_NAME="main"

docfx --metadata

docfx --serve --force