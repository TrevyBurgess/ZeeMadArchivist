# ZeeMadArchivist.ShellExtension

A classic COM Shell Property Sheet Extension that adds a **Tags** tab to the Windows properties dialog for any file, folder, or drive.

## Why this is a separate project

The main `ZeeMadArchivist` application is a WinUI 3 / Windows App SDK packaged app. Shell extensions are in-process COM DLLs that Windows Explorer loads directly. A WinUI 3 app cannot be loaded into Explorer, so the extension must be implemented as a separate .NET Framework 4.8 class library.

## Implementation

- `TagsPropertySheet` implements `IShellExtInit` and `IShellPropSheetExt`.
- `TagsPropertyPage` is the WinForms UI shown inside the new tab.
- The tab is registered for:
  - All files (`HKCR\*\shellex\PropertySheetHandlers`)
  - All folders (`HKCR\Directory\shellex\PropertySheetHandlers`)
  - All drives (`HKCR\Drive\shellex\PropertySheetHandlers`)

## Building

Open the solution and build the `ZeeMadArchivist.ShellExtension` project for the same architecture as the target Explorer process (usually `x64` on modern Windows).

```
dotnet build ZeeMadArchivist.ShellExtension.csproj -c Release -p:Platform=x64
```

## Registration

The extension uses `[ComRegisterFunction]` / `[ComUnregisterFunction]`, so registration is performed with `regasm.exe` from an elevated command prompt.

### Register

```powershell
# 64-bit Explorer on 64-bit Windows
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" `
  ZeeMadArchivist.ShellExtension.dll /codebase
```

### Unregister

```powershell
& "C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe" `
  ZeeMadArchivist.ShellExtension.dll /unregister
```

> **Note:** `regasm.exe` must match the DLL architecture. Use the 32-bit `regasm.exe` in `C:\Windows\Microsoft.NET\Framework\v4.0.30319` only if you built the DLL for x86.

## Verification

After registering, right-click any file or folder, choose **Properties**, and a **Tags** tab should appear.

## Testing note

Automated unit tests for this component are not included because the functionality depends on Windows Explorer and COM, which are external system services. Manual registration and verification are required.
