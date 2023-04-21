# KenticoCommunity.TinyPng

[![License](https://img.shields.io/github/license/void-type/KenticoCommunity.TinyPng.svg)](https://github.com/void-type/KenticoCommunity.TinyPng/blob/main/LICENSE.txt)
[![NuGet package](https://img.shields.io/nuget/v/KenticoCommunity.TinyPng.svg)](https://www.nuget.org/packages/KenticoCommunity.TinyPng/)
[![MyGet package](https://img.shields.io/myget/voidcoredev/vpre/KenticoCommunity.TinyPng.svg?label=myget)](https://www.myget.org/feed/voidcoredev/package/nuget/KenticoCommunity.TinyPng)
[![Build Status](https://img.shields.io/azure-devops/build/void-type/VoidCore/23/main)](https://dev.azure.com/void-type/VoidCore/_build/latest?definitionId=23&branchName=main)
<!-- [![Test Coverage](https://img.shields.io/azure-devops/coverage/void-type/VoidCore/23/main)](https://dev.azure.com/void-type/VoidCore/_build/latest?definitionId=23&branchName=main) -->

[KenticoCommunity.TinyPng](https://github.com/void-type/KenticoCommunity.TinyPng) is a Kentico Xperience 13 module that intercepts image uploads and compresses them using the [TinyPNG API](https://tinypng.com). This module can save your editors time over manual compression and improve your website performance.

This module was inspired by [DeleteAgency.Kentico12.TinyPng](https://github.com/diger74/DeleteAgency.Kentico12.TinyPng) but with a focus on simple setup and adds error logging.

## Installation and setup

**Note**
This package is still a work in progress.

Install the package to your CMS project. Click the NuGet or MyGet badges above for instructions specific to your SDK.

Configure the module. At minimum, you'll need to provide `TinyPng:ApiKey` with an [API key](https://tinypng.com/developers) in your AppSettings. See the configuration section for more details.

```xml
<appSettings>
    <add key="TinyPng:ApiKey" value="your-api-key" />
<appSettings>
```

TODO: Install as NuGet and test if these are needed.

You may need to add the following binding redirects in your CMS's web.config if they aren't there.

You may also need to install System.Text.Json v6.0.0.

```xml
<assemblyBinding>
  <dependentAssembly>
    <assemblyIdentity name="CMS.Base" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
  <dependentAssembly>
    <assemblyIdentity name="CMS.Core" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
  <dependentAssembly>
    <assemblyIdentity name="CMS.DataEngine" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
  <dependentAssembly>
    <assemblyIdentity name="CMS.DocumentEngine" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
  <dependentAssembly>
    <assemblyIdentity name="CMS.Helpers" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
  <dependentAssembly>
    <assemblyIdentity name="CMS.MediaLibrary" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
  <dependentAssembly>
    <assemblyIdentity name="CMS.SiteProvider" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
  <dependentAssembly>
    <assemblyIdentity name="CMS.WorkflowEngine" publicKeyToken="834b12a258f213f9" culture="neutral" />
    <bindingRedirect oldVersion="0.0.0.0-13.0.13.0" newVersion="13.0.13.0" />
  </dependentAssembly>
</assemblyBinding>
```

## Configuration

The [default settings provider](src/KenticoCommunity.TinyPng/TinyPngSettingsFromAppSettingsProvider.cs) has defaults for the other settings, but they can be overridden. Setting `TinyPng:IsEnabled` to false will disable the module. The default provider pulls settings at runtime, so there's no need to restart the app when changing settings.

If you prefer to pull settings from elsewhere, you can create your own implementation of [ITinyPngSettingsProvider](src/KenticoCommunity.TinyPng/ITinyPngSettingsProvider.cs). Register your provider to Kentico's IoC container using one of the following methods:

- Using an [attribute](https://devnet.kentico.com/docs/13_0/api/html/T_CMS_RegisterImplementationAttribute.htm).

```csharp
[assembly: RegisterImplementation(typeof(ITinyPngSettingsProvider), typeof(MyTinyPngSettingsProvider))]
```

- Using [Service.Use](https://devnet.kentico.com/docs/13_0/api/html/Overload_CMS_Core_Service_Use.htm) in a custom module's `OnPreInit()` method.

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

## Notes

AttachmentHashes don't appear to be used at all OOB. We have code in case they are present to know more accurately if the image is the same. Otherwise we'll fallback to size in bytes.

Copying pages/media and using the image editor will result in re-shrinking and possibly deep-fried images. TODO: can we detect these events and bail?

I recommend installing this on your authoring environment (where the content is edited). If you install on an environment that accepts staging tasks, you might re-shrink images (my assumption, I have not tested this).

### No page workflow

Attachment and Media uploads/edits (media doesn't care if the page has a workflow or not):

- The event is hit once and the image is shrunk.
- WARNING: Editing images in the media library can result in re-compression of an image. It's better to edit the raw image externally and re-upload. There doesn't seem to be a way to detect an edit vs update (upload replacement).

Page is saved:

- No event hit.

Page is copied:

- The Attachment event is hit **TWICE** and the image is shrank twice, the second event's binary is discarded.
- TODO: detect second event and bail.

### Workflow

Attachment uploads and edits under page workflow results in the following:

- First, AttachmentHistory event is hit. We look up the last version of the history. If they aren't the same binary, we'll shrink and save.
- Second, Attachment event is hit. Since WorkflowStep is not null, we bail. This second binary seems to be ignored by the CMS and not saved on the record, the record actually takes our shrunk AttachmentHistory binary.
- TODO: Verify that the attachment binary really isn't used anywhere.

When the attachment's page is saved under workflow:

- Then Attachment event is hit, but WorkflowStep is not null, so we bail. This results in a no-op. The attachment history exists from when it was uploaded before the page was saved.

When attachment's page is copied under workflow:

- First, Attachment event hits and bails due to workflow.
- Second, AttachmentHistory event hits and shrinks.
- Third, Attachment event is hit again, but workflow is **NULL** and the image is shrunk again (This second binary is discarded).
- TODO: Detect third event (second Attachment) and bail.

