# KenticoCommunity.TinyPng

[![NuGet package](https://img.shields.io/nuget/v/KenticoCommunity.TinyPng.svg)](https://www.nuget.org/packages/KenticoCommunity.TinyPng/)
[![MyGet package](https://img.shields.io/myget/voidcoredev/vpre/KenticoCommunity.TinyPng.svg?label=myget)](https://www.myget.org/feed/voidcoredev/package/nuget/KenticoCommunity.TinyPng)

[![License](https://img.shields.io/github/license/void-type/KenticoCommunity.TinyPng.svg)](https://github.com/void-type/KenticoCommunity.TinyPng/blob/main/LICENSE.txt)
[![Build Status](https://img.shields.io/azure-devops/build/void-type/VoidCore/23.svg)](https://dev.azure.com/void-type/VoidCore/_build/latest?definitionId=23&branchName=main)
<!-- [![Test Coverage](https://img.shields.io/azure-devops/coverage/void-type/VoidCore/23.svg)](https://dev.azure.com/void-type/VoidCore/_build/latest?definitionId=23&branchName=main) -->

[KenticoCommunity.TinyPng](https://github.com/void-type/KenticoCommunity.TinyPng) is a Kentico Xperience 13 module that intercepts image uploads and compresses them using the [TinyPNG API](https://tinypng.com). This module can save your editors time over manual compression and improve your website performance.

This module was inspired by [DeleteAgency.Kentico12.TinyPng](https://github.com/diger74/DeleteAgency.Kentico12.TinyPng) but with a focus on simple setup and adds error logging.

## Installation and setup

Install the package to your CMS project. Click the NuGet or MyGet badges above for instructions specific to your SDK.

Configure the module. At minimum, you'll need to provide `TinyPng:ApiKey` with an [API key](https://tinypng.com/developers) in your AppSettings.

The [default settings provider](src/KenticoCommunity.TinyPng/TinyPngSettingsFromAppSettingsProvider.cs) has defaults for the other settings, but they can be overridden. Setting `TinyPng:IsEnabled` to false will disable the module. The default provider pulls settings at runtime, so there's no need to restart the app when changing settings.

If you prefer to pull settings from elsewhere, you can create your own implementation of [ITinyPngSettingsProvider](src/KenticoCommunity.TinyPng/ITinyPngSettingsProvider.cs). Register your provider to Kentico's IoC container using one of the following methods:

* Using an [attribute](https://devnet.kentico.com/docs/13_0/api/html/T_CMS_RegisterImplementationAttribute.htm).

```csharp
[assembly: RegisterImplementation(typeof(ITinyPngSettingsProvider), typeof(MyTinyPngSettingsProvider))]
```

* Using [Service.Use](https://devnet.kentico.com/docs/13_0/api/html/Overload_CMS_Core_Service_Use.htm) in a custom module's `OnPreInit()` method.

```csharp
Service.Use<ITinyPngSettingsProvider>(() => new MyTinyPngSettingsProvider());
# or
Service.Use<ITinyPngSettingsProvider, MyTinyPngSettingsProvider>();
```

## Developers

To work on this project, you will need the [.NET SDK](https://dotnet.microsoft.com/download) and [PowerShell](https://github.com/PowerShell/PowerShell/releases/latest).

See the /build folder for scripts used to build this project. Run build.ps1 to make a production build. This will output a NuGet package you can upload to your own feed if desired.

```powershell
./build/build.ps1
```

There are [VSCode](https://code.visualstudio.com/) tasks for each script. The build task (ctrl + shift + b) performs the standard CI build.
