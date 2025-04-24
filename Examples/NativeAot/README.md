# Terminal.Gui C# SelfContained

This project aims to test the `Terminal.Gui` library to create a simple `native AOT` `self-container` GUI application in C#, ensuring that all its features are available.

With `Debug` the `.csproj` is used and with `Release` the latest `nuget package` is used, either in `Solution Configurations` or in `Profile Publish` or in the `Publish_linux-x64` or in the `Publish_osx-x64` files.
Unlike self-contained single-file publishing, native AOT publishing must be generated on the same platform as the target execution version. Therefore, if the target execution is Linux, then the publishing must be generated on a Linux operating system. Attempting to generate on Windows for the Linux target will throw an exception.

To publish the `native AOT` file in `Debug` or `Release` mode, it is not necessary to select it in the `Solution Configurations`, just choose the `Debug` or `Release` configuration in the `Publish Profile` or the `*.sh` files.

When executing the file directly from the `native AOT` file and needing to debug it, it will be necessary to attach it to the debugger, just like any other standalone application and selecting `Native Code`.