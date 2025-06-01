
# Cross-Platform Driver Model

[!IMPORTANT]
> In v1, the driver model was a source of pain and confusion. In v2, our goal is to make the driver model a source of pride and joy. It is still a work in progress. We will update this document as we add more information.

## Overview

The driver model is the mechanism by which Terminal.Gui can support multiple platforms. Windows, Mac, Linux, and even (eventually) web browsers are supported.

## Drivers

### Legacy

- `WindowsDriver` - A driver that uses the Windows API to draw to the console.
- `NetDriver` - A driver that uses the .NET `System.Console` to draw to the console.
- `CursesDriver` - A driver that uses the ncurses library to draw to the console.

### In Development for v2

- `v2win` - A driver optimized for Windows.
- `v2net` - A driver that uses the .NET `System.Console` to draw to the console and works on all platforms.


