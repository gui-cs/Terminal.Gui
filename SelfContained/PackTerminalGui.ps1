# Step 1: Build and pack Terminal.Gui
dotnet build ../Terminal.Gui/Terminal.Gui.csproj --configuration Release
dotnet pack ../Terminal.Gui/Terminal.Gui.csproj --configuration Release --output ../local_packages

# Step 2: Restore SelfContained with the new package
dotnet restore ./SelfContained.csproj --source ./local_packages

# Step 3: Build SelfContained
dotnet build ./SelfContained.csproj --configuration Release
