# Terminal.Gui C# SelfContained

This project aims to test the `Terminal.Gui` library to create a simple `self-contained` `single-file` GUI application in C#, ensuring that all its features are available.

With `Debug` the `.csproj` is used and with `Release` the latest `nuget package` is used, either in `Solution Configurations` or in `Profile Publish`.

To publish the self-contained single file in `Debug` or `Release` mode, it is not necessary to select it in the `Solution Configurations`, just choose the `Debug` or `Release` configuration in the `Publish Profile`.

When executing the file directly from the self-contained single file and needing to debug it, it will be necessary to attach it to the debugger, just like any other standalone application. However, when trying to attach the file running on `Linux` or `macOS` to the debugger, it will issue the error "`Failed to attach to process: Unknown Error: 0x80131c3c`". This issue has already been reported on [Developer Community](https://developercommunity.visualstudio.com/t/Failed-to-attach-to-process:-Unknown-Err/10694351). Maybe it would be a good idea to vote in favor of this fix because I think `Visual Studio for macOS` is going to be discontinued and we need this fix to remotely attach a process running on `Linux` or `macOS` to `Windows 11`.