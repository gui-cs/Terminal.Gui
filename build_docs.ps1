# Builds the Terminal.Gui API documetation. See docfx/README.md
rm -R .\docs
dotnet build --configuration Release
cd docfx/
docfx --metadata
docfx --serve
cd ..