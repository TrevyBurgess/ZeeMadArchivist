using Microsoft.Win32;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

#nullable enable

namespace CyberFeedForward.Tools.ZeeFileSystem.Utilities;

public static partial class FolderTools
{
    public static bool TryOpenFolderInExplorer(string folderPath, out string errorMessage, Func<ProcessStartInfo, Process?>? processStart = null)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(folderPath))
        {
            errorMessage = "Folder path cannot be empty.";
            return false;
        }

        string fullPath;
        try
        {
            fullPath = Path.GetFullPath(folderPath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Trace.TraceError(ex.ToString());
            return false;
        }

        if (!Directory.Exists(fullPath))
        {
            errorMessage = "Folder does not exist.";
            return false;
        }

        try
        {
            var start = processStart ?? (psi => Process.Start(psi));
            var psi = new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = string.Concat('"', fullPath, '"'),
                UseShellExecute = true,
            };

            var proc = start(psi);
            if (proc is null)
            {
                errorMessage = "Unable to launch File Explorer.";
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Trace.TraceError(ex.ToString());
            return false;
        }
    }

    public static bool TryRenameIconFile(string customIconsFolderPath, string originalFilePath, string newBaseName, out string errorMessage)
    {
        errorMessage = string.Empty;

        if (string.IsNullOrWhiteSpace(customIconsFolderPath))
        {
            errorMessage = "Custom icons folder path cannot be empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(originalFilePath))
        {
            errorMessage = "Original file path cannot be empty.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(newBaseName))
        {
            errorMessage = "New icon name cannot be empty.";
            return false;
        }

        var trimmedNewBaseName = newBaseName.Trim();
        if (trimmedNewBaseName.Length == 0)
        {
            errorMessage = "New icon name cannot be empty.";
            return false;
        }

        if (trimmedNewBaseName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            errorMessage = "New icon name contains invalid characters.";
            return false;
        }

        try
        {
            var folderFullPath = Path.GetFullPath(customIconsFolderPath);
            var originalFullPath = Path.GetFullPath(originalFilePath);

            if (!Directory.Exists(folderFullPath))
            {
                errorMessage = "Custom icons folder does not exist.";
                return false;
            }

            if (!File.Exists(originalFullPath))
            {
                errorMessage = "Original icon file does not exist.";
                return false;
            }

            if (!folderFullPath.EndsWith(Path.DirectorySeparatorChar))
            {
                folderFullPath += Path.DirectorySeparatorChar;
            }

            if (!originalFullPath.StartsWith(folderFullPath, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "Icon file is not in the custom icons folder.";
                return false;
            }

            var extension = Path.GetExtension(originalFullPath);
            if (string.IsNullOrWhiteSpace(extension))
            {
                extension = ".ico";
            }

            var destFullPath = Path.Combine(folderFullPath, trimmedNewBaseName + extension);
            destFullPath = Path.GetFullPath(destFullPath);

            if (!destFullPath.StartsWith(folderFullPath, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = "New icon name resolves outside the custom icons folder.";
                return false;
            }

            if (File.Exists(destFullPath))
            {
                errorMessage = "A file with the new name already exists.";
                return false;
            }

            File.Move(originalFullPath, destFullPath);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Trace.TraceError(ex.ToString());
            return false;
        }
    }

    public static int LoadDefaultIcons(string customIconsFolderPath, string? iconsFolderPath = null)
    {
        if (string.IsNullOrWhiteSpace(customIconsFolderPath))
        {
            return 0;
        }

        var sourceFolder = iconsFolderPath;
        if (string.IsNullOrWhiteSpace(sourceFolder))
        {
            var baseDirectory = AppContext.BaseDirectory;

            sourceFolder = Path.Combine(baseDirectory, "Icons");
            if (!Directory.Exists(sourceFolder))
            {
                sourceFolder = Path.Combine(baseDirectory, "AppTools", "Icons");
            }

            if (!Directory.Exists(sourceFolder))
            {
                sourceFolder = Path.Combine(baseDirectory, "AppX", "Icons");
                if (!Directory.Exists(sourceFolder))
                {
                    sourceFolder = Path.Combine(baseDirectory, "AppX", "AppTools", "Icons");
                }
            }

            if (!Directory.Exists(sourceFolder))
            {
                var assemblyFolder = Path.GetDirectoryName(typeof(FolderTools).Assembly.Location);
                if (!string.IsNullOrWhiteSpace(assemblyFolder))
                {
                    sourceFolder = Path.Combine(assemblyFolder, "Icons");
                    if (!Directory.Exists(sourceFolder))
                    {
                        sourceFolder = Path.Combine(assemblyFolder, "AppTools", "Icons");
                    }

                    if (!Directory.Exists(sourceFolder))
                    {
                        sourceFolder = Path.Combine(assemblyFolder, "AppX", "Icons");
                        if (!Directory.Exists(sourceFolder))
                        {
                            sourceFolder = Path.Combine(assemblyFolder, "AppX", "AppTools", "Icons");
                        }
                    }
                }
            }
        }

        if (string.IsNullOrWhiteSpace(sourceFolder) || !Directory.Exists(sourceFolder))
        {
            return 0;
        }

        try
        {
            Directory.CreateDirectory(customIconsFolderPath);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return 0;
        }

        var copied = 0;
        System.Collections.Generic.IEnumerable<string> files;
        try
        {
            files = Directory.EnumerateFiles(sourceFolder);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return 0;
        }

        foreach (var sourceFile in files)
        {
            if (string.IsNullOrWhiteSpace(sourceFile))
            {
                continue;
            }

            var fileName = Path.GetFileName(sourceFile);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                continue;
            }

            var destFile = Path.Combine(customIconsFolderPath, fileName);
            try
            {
                if (File.Exists(destFile))
                {
                    continue;
                }

                File.Copy(sourceFile, destFile, overwrite: false);
                copied++;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
            }
        }

        return copied;
    }

    public static bool UpdateFolderIcon(string iconFilePath, string folderPath)
    {
        if (string.IsNullOrWhiteSpace(iconFilePath) || string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }

        if (!File.Exists(iconFilePath) || !Directory.Exists(folderPath))
        {
            return false;
        }

        try
        {
            var desktopIniPath = Path.Combine(folderPath, "desktop.ini");

            var desktopIniContents = new StringBuilder();
            desktopIniContents.AppendLine("[.ShellClassInfo]");
            desktopIniContents.AppendLine($"IconResource={iconFilePath},0");

            File.WriteAllText(desktopIniPath, desktopIniContents.ToString(), Encoding.Unicode);

            var iniAttributes = File.GetAttributes(desktopIniPath);
            File.SetAttributes(desktopIniPath, iniAttributes | FileAttributes.Hidden | FileAttributes.System);

            var folderAttributes = File.GetAttributes(folderPath);
            File.SetAttributes(folderPath, folderAttributes | FileAttributes.ReadOnly);

            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW, folderPath, null);
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return false;
        }
    }

    public static string? GetDefaultAppIconPath()
    {
        var baseDirectory = AppContext.BaseDirectory;
        if (string.IsNullOrWhiteSpace(baseDirectory))
        {
            return null;
        }

        var candidate = Path.Combine(baseDirectory, "Assets", "AppIcon.ico");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        candidate = Path.Combine(baseDirectory, "AppX", "Assets", "AppIcon.ico");
        if (File.Exists(candidate))
        {
            return candidate;
        }

        return null;
    }

    public static bool TrySetDriveIcon(char driveLetter, string iconFilePath, out string errorMessage)
    {
        errorMessage = string.Empty;

        var normalizedLetter = char.ToUpperInvariant(driveLetter);
        if (normalizedLetter is < 'A' or > 'Z')
        {
            errorMessage = "Drive letter is invalid.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(iconFilePath))
        {
            errorMessage = "Icon file path cannot be empty.";
            return false;
        }

        string fullIconPath;
        try
        {
            fullIconPath = Path.GetFullPath(iconFilePath);
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            return false;
        }

        if (!File.Exists(fullIconPath))
        {
            errorMessage = "Icon file does not exist.";
            return false;
        }

        try
        {
            using var iconKey = Registry.CurrentUser.CreateSubKey(
                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\{normalizedLetter}\DefaultIcon");
            if (iconKey is null)
            {
                errorMessage = "Unable to open drive icon registry key.";
                return false;
            }

            iconKey.SetValue(string.Empty, fullIconPath, RegistryValueKind.String);
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW, $"{normalizedLetter}:\\", null);
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, null, null);
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            Trace.TraceError(ex.ToString());
            return false;
        }
    }

    public static bool TryGetDriveIconPath(char driveLetter, out string? iconPath)
    {
        iconPath = null;
        var normalizedLetter = char.ToUpperInvariant(driveLetter);
        if (normalizedLetter is < 'A' or > 'Z')
        {
            return false;
        }

        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                $@"Software\Microsoft\Windows\CurrentVersion\Explorer\DriveIcons\{normalizedLetter}\DefaultIcon");
            if (key is null)
            {
                return false;
            }

            iconPath = key.GetValue(string.Empty) as string;
            return !string.IsNullOrWhiteSpace(iconPath);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return false;
        }
    }

    public static bool TrySetDefaultAppDriveIcon(char driveLetter, out string errorMessage)
    {
        var iconPath = GetDefaultAppIconPath();
        if (string.IsNullOrWhiteSpace(iconPath))
        {
            errorMessage = "AppIcon.ico was not found.";
            return false;
        }

        return TrySetDriveIcon(driveLetter, iconPath, out errorMessage);
    }

    public static int MapDrive(string folderPath, char driveLetter, string name)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return Win32ErrorInvalidParameter;
        }

        var normalizedLetter = char.ToUpperInvariant(driveLetter);
        if (normalizedLetter is < 'A' or > 'Z')
        {
            return Win32ErrorInvalidParameter;
        }

        if (string.IsNullOrWhiteSpace(name))
        {
            return Win32ErrorInvalidParameter;
        }

        var localName = normalizedLetter + ":";
        var trimmedPath = folderPath.Trim();

        if (IsUncPath(trimmedPath))
        {
            var nr = new NetResource
            {
                dwType = ResourceTypeDisk,
                lpLocalName = Marshal.StringToCoTaskMemUni(localName),
                lpRemoteName = Marshal.StringToCoTaskMemUni(folderPath),
                lpProvider = IntPtr.Zero,
                lpComment = Marshal.StringToCoTaskMemUni(name),
            };

            var size = Marshal.SizeOf<NetResource>();
            var ptr = Marshal.AllocHGlobal(size);
            Marshal.StructureToPtr(nr, ptr, false);

            try
            {
                return WNetAddConnection2(ptr, IntPtr.Zero, IntPtr.Zero, ConnectUpdateProfile);
            }
            finally
            {
                Marshal.DestroyStructure<NetResource>(ptr);
                Marshal.FreeHGlobal(ptr);
                Marshal.FreeCoTaskMem(nr.lpLocalName);
                Marshal.FreeCoTaskMem(nr.lpRemoteName);
                Marshal.FreeCoTaskMem(nr.lpComment);
            }
        }

        return MapLocalDrive(normalizedLetter, trimmedPath);
    }

    /// <summary>
    /// Renames a drive by setting its volume label and Explorer MountPoints2 label.
    /// </summary>
    /// <param name="driveLetter">The drive letter to rename.</param>
    /// <param name="newName">The new display name for the drive.</param>
    /// <param name="mappedPath">Optional path the drive is mapped to; used to set the network-share label.</param>
    /// <returns><c>true</c> if any label was changed; otherwise <c>false</c>.</returns>
    public static bool RenameDrive(char driveLetter, string newName, string? mappedPath = null)
    {
        try
        {
            var normalizedLetter = char.ToUpperInvariant(driveLetter);
            if (normalizedLetter is < 'A' or > 'Z')
            {
                Trace.TraceWarning($"RenameDrive: invalid drive letter '{driveLetter}'.");
                return false;
            }

            var trimmedName = (newName ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(trimmedName))
            {
                Trace.TraceWarning("RenameDrive: new name is empty.");
                return false;
            }

            Trace.TraceInformation($"RenameDrive: drive={normalizedLetter}, name={trimmedName}, path={mappedPath ?? "(null)"}");

            var rootPath = normalizedLetter + @":\";
            var drivePath = normalizedLetter + ":";
            var isUncPath = !string.IsNullOrWhiteSpace(mappedPath) && IsUncPath(mappedPath);
            var anySuccess = false;

            // Use the Shell.Application API for network drives; it is the same mechanism
            // Explorer uses when you right-click a drive and choose Rename. Avoid it for
            // local/SUBST drives because it can report "The volume is not valid".
            if (isUncPath && TryRenameViaShellApi(normalizedLetter, trimmedName))
            {
                anySuccess = true;
            }

            // Set the volume label; this works for real local drives.
            if (SetVolumeLabelNative(rootPath, trimmedName) != 0)
            {
                anySuccess = true;
            }
            else
            {
                var lastError = Marshal.GetLastWin32Error();
                Trace.TraceError($"SetVolumeLabel failed for {rootPath} with error {lastError}.");
            }

            var driveLetterSubKey = normalizedLetter.ToString();

            // For network drives, set the label keyed by the UNC path so the share name follows the label.
            if (isUncPath)
            {
                var uncSubKey = "##" + mappedPath!.TrimStart('\\').Replace('\\', '#');
                if (TrySetMountPointLabel(uncSubKey, trimmedName))
                {
                    anySuccess = true;
                }
            }

            // The drive-letter key is what Explorer uses for SUBST drives and also takes
            // precedence for network drives in the local Explorer view.
            if (TrySetMountPointLabel(driveLetterSubKey, trimmedName))
            {
                anySuccess = true;
            }

            // Clear stale keys that could conflict with the drive-letter label.
            if (!isUncPath && !string.IsNullOrWhiteSpace(mappedPath))
            {
                var fullLocalPath = Path.GetFullPath(mappedPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
                if (!string.IsNullOrEmpty(fullLocalPath) && fullLocalPath.Length >= 3 && fullLocalPath[1] == ':')
                {
                    ClearMountPointLabel(fullLocalPath.Replace('\\', '#'));
                }
            }

            ClearMountPointLabel(driveLetterSubKey + "#");

            // Refresh Explorer for the drive and force a global association refresh.
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW, rootPath, null);
            SHChangeNotify(SHCNE_UPDATEITEM, SHCNF_PATHW, drivePath, null);
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, null, null);

            Trace.TraceInformation($"RenameDrive: completed with anySuccess={anySuccess}.");
            return anySuccess;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"RenameDrive: unexpected error: {ex}");
            return false;
        }
    }

    private static bool TryRenameViaShellApi(char driveLetter, string newName)
    {
        try
        {
            var shellType = Type.GetTypeFromProgID("Shell.Application");
            if (shellType is null)
            {
                Trace.TraceError("Shell.Application is not available.");
                return false;
            }

            dynamic shell = Activator.CreateInstance(shellType) ?? throw new InvalidOperationException("Could not create Shell.Application instance.");
            dynamic folder = shell.NameSpace(driveLetter + ":");
            if (folder is null)
            {
                Trace.TraceError($"Shell.Application could not open drive {driveLetter}:");
                return false;
            }

            folder.Self.Name = newName;
            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"TryRenameViaShellApi failed for drive {driveLetter}: {ex}");
            return false;
        }
    }

    private static bool TrySetMountPointLabel(string subKeyName, string label)
    {
        try
        {
            using var key = Registry.CurrentUser.CreateSubKey(
                @$"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\{subKeyName}")
                ?? throw new InvalidOperationException($"Could not create MountPoints2 subkey '{subKeyName}'.");

            key.SetValue("_LabelFromReg", label, RegistryValueKind.String);

            Trace.TraceInformation($"TrySetMountPointLabel: set '{subKeyName}' to '{label}'.");

            return true;
        }
        catch (Exception ex)
        {
            Trace.TraceError($"TrySetMountPointLabel failed for '{subKeyName}': {ex}");
            return false;
        }
    }

    private static void ClearMountPointLabel(string subKeyName)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @$"Software\Microsoft\Windows\CurrentVersion\Explorer\MountPoints2\{subKeyName}", writable: true);

            if (key is not null)
            {
                key.DeleteValue("_LabelFromReg", throwOnMissingValue: false);
                Trace.TraceInformation($"ClearMountPointLabel: cleared '{subKeyName}'.");
            }
        }
        catch (Exception ex)
        {
            Trace.TraceError($"ClearMountPointLabel failed for '{subKeyName}': {ex}");
        }
    }

    private static bool IsUncPath(string path)
    {
        return path.StartsWith("\\\\", StringComparison.Ordinal);
    }

    private static int MapLocalDrive(char driveLetter, string folderPath)
    {
        try
        {
            var fullPath = Path.GetFullPath(folderPath);
            if (!fullPath.EndsWith(Path.DirectorySeparatorChar) && !fullPath.EndsWith(Path.AltDirectorySeparatorChar))
            {
                fullPath += Path.DirectorySeparatorChar;
            }

            if (!Directory.Exists(fullPath))
            {
                return Win32ErrorBadNetName;
            }

            return RunSubst($"{driveLetter}: \"{fullPath}\"");
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return Win32ErrorInvalidParameter;
        }
    }

    private static int RunSubst(string arguments)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "subst.exe",
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return Win32ErrorInvalidParameter;
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode == 0)
            {
                return 0;
            }

            if (!string.IsNullOrWhiteSpace(error) && error.Contains("in use", StringComparison.OrdinalIgnoreCase))
            {
                return Win32ErrorAlreadyAssigned;
            }

            Trace.TraceError($"subst.exe failed with exit code {process.ExitCode}. Output: {output}. Error: {error}.");
            return Win32ErrorBadNetName;
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return Win32ErrorInvalidParameter;
        }
    }

    public static string GetMapDriveErrorMessage(int errorCode, char driveLetter, string folderPath)
    {
        var normalizedLetter = char.ToUpperInvariant(driveLetter);
        var driveName = normalizedLetter is >= 'A' and <= 'Z' ? normalizedLetter + ":" : "the selected drive";
        var targetPath = string.IsNullOrWhiteSpace(folderPath) ? "the selected folder" : folderPath.Trim();

        return errorCode switch
        {
            Win32ErrorAlreadyAssigned => $"{driveName} is already in use. Choose a different drive letter and try again.",
            Win32ErrorBadNetName => $"Windows could not find or access {targetPath}. Check that the folder exists and try again.",
            Win32ErrorAccessDenied => $"Windows denied access while mapping {targetPath}. Check folder permissions and try again.",
            Win32ErrorInvalidParameter => "The selected folder or drive letter is invalid. Check your selection and try again.",
            _ => $"Windows could not map {targetPath} to {driveName}. Try a different drive letter or folder, then try again.",
        };
    }

    public static Exception CreateMapDriveException(int errorCode, char driveLetter, string folderPath)
    {
        return new InvalidOperationException(
            GetMapDriveErrorMessage(errorCode, driveLetter, folderPath),
            new Win32Exception(errorCode));
    }

    public static bool UnmapDrive(char driveLetter)
    {
        var normalizedLetter = char.ToUpperInvariant(driveLetter);
        if (normalizedLetter is < 'A' or > 'Z')
        {
            return false;
        }

        var localName = normalizedLetter + ":";
        var wnetResult = WNetCancelConnection2(localName, CancelUpdateProfile, true);
        if (wnetResult == 0)
        {
            return true;
        }

        return RunSubst($"{localName} /D") == 0;
    }

    public static bool TryFindDriveLetterForPath(string path, out char driveLetter)
    {
        driveLetter = default;
        if (string.IsNullOrWhiteSpace(path))
        {
            return false;
        }

        var normalizedPath = Path.GetFullPath(path.Trim());
        if (!normalizedPath.EndsWith(Path.DirectorySeparatorChar) && !normalizedPath.EndsWith(Path.AltDirectorySeparatorChar))
        {
            normalizedPath += Path.DirectorySeparatorChar;
        }

        foreach (var mapping in GetSubstMappings())
        {
            if (!mapping.Path.EndsWith(Path.DirectorySeparatorChar) && !mapping.Path.EndsWith(Path.AltDirectorySeparatorChar))
            {
                if (string.Equals(normalizedPath, mapping.Path + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase))
                {
                    driveLetter = mapping.DriveLetter;
                    return true;
                }
            }
            else if (string.Equals(normalizedPath, mapping.Path, StringComparison.OrdinalIgnoreCase))
            {
                driveLetter = mapping.DriveLetter;
                return true;
            }
        }

        return false;
    }

    public static bool TryUnmapDriveForPath(string path)
    {
        if (TryFindDriveLetterForPath(path, out var driveLetter))
        {
            return UnmapDrive(driveLetter);
        }

        return true;
    }

    private static IEnumerable<(char DriveLetter, string Path)> GetSubstMappings()
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "subst.exe",
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };

            using var process = Process.Start(psi);
            if (process is null)
            {
                return [];
            }

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (!string.IsNullOrWhiteSpace(error))
            {
                Trace.TraceError($"subst.exe failed to list mappings. Error: {error}.");
            }

            return ParseSubstMappings(output);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            return [];
        }
    }

    private static IEnumerable<(char DriveLetter, string Path)> ParseSubstMappings(string output)
    {
        if (string.IsNullOrWhiteSpace(output))
        {
            yield break;
        }

        const string Separator = ":\\: => ";
        foreach (var line in output.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
        {
            var trimmedLine = line.Trim();
            if (trimmedLine.Length < Separator.Length + 1)
            {
                continue;
            }

            var separatorIndex = trimmedLine.IndexOf(Separator, StringComparison.Ordinal);
            if (separatorIndex < 0)
            {
                continue;
            }

            var driveLetter = trimmedLine[0];
            if (driveLetter is < 'A' or > 'Z' and < 'a' or > 'z')
            {
                continue;
            }

            var mappedPath = trimmedLine[(separatorIndex + Separator.Length)..].Trim();
            if (string.IsNullOrWhiteSpace(mappedPath))
            {
                continue;
            }

            yield return (char.ToUpperInvariant(driveLetter), mappedPath);
        }
    }

    private const int Win32ErrorInvalidParameter = 87;
    private const int Win32ErrorAccessDenied = 5;
    private const int Win32ErrorAlreadyAssigned = 85;
    private const int Win32ErrorBadNetName = 67;
    private const int ResourceTypeDisk = 0x00000001;
    private const int ConnectUpdateProfile = 0x00000001;
    private const int CancelUpdateProfile = 0x00000001;

    [StructLayout(LayoutKind.Sequential)]
    private struct NetResource
    {
        public int dwScope;
        public int dwType;
        public int dwDisplayType;
        public int dwUsage;
        public IntPtr lpLocalName;
        public IntPtr lpRemoteName;
        public IntPtr lpComment;
        public IntPtr lpProvider;
    }

    [LibraryImport("mpr.dll")]
    private static partial int WNetAddConnection2(IntPtr lpNetResource, IntPtr lpPassword, IntPtr lpUsername, int dwFlags);

    [LibraryImport("mpr.dll", StringMarshalling = StringMarshalling.Utf16, EntryPoint = "WNetCancelConnection2W")]
    private static partial int WNetCancelConnection2(string lpName, int dwFlags, [MarshalAs(UnmanagedType.Bool)] bool fForce);

    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "SetVolumeLabelW")]
    private static partial int SetVolumeLabelNative(string lpRootPathName, string lpVolumeName);

    private const uint SHCNE_UPDATEITEM = 0x00002000;
    private const uint SHCNE_ASSOCCHANGED = 0x08000000;
    private const uint SHCNF_IDLIST = 0x0000;
    private const uint SHCNF_PATHW = 0x0005;

    [LibraryImport("shell32.dll", StringMarshalling = StringMarshalling.Utf16, EntryPoint = "SHChangeNotify")]
    private static partial void SHChangeNotify(uint wEventId, uint uFlags, string? dwItem1, string? dwItem2);
}
