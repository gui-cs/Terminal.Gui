# Builds the Terminal.gui API documentation using docfx

dotnet build --configuration Release ../Terminal.sln

rm ../docs -Recurse -Force

#docfx --metadata

docfx --serve --force