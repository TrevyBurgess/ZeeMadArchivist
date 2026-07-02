using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Win32;

#nullable enable

namespace CyberFeedForward.Tools.ZeeFileSystem.Services
{
    public static class ShellServices
    {
        private const string TagsPropertySheetClsid = "{F4A9C6E2-7B5D-4B2E-9F1C-8D3E2A6B5C4D}";

        /// <summary>
        /// Renames a drive in the file system.
        /// </summary>
        /// <returns></returns>
        public static bool RenameDrive()
        {
            return true;
        }

        /// <summary>
        /// Removes the custom "Tags" property sheet tab from the registry so it no longer
        /// appears on file, folder, or drive properties dialogs.
        /// </summary>
        /// <returns><c>true</c> if the registry keys were removed successfully; otherwise <c>false</c>.</returns>
        public static bool RemoveTagsPropertyPage()
        {
            return RemoveTagsPropertyPageCore(null);
        }

        /// <summary>
        /// Removes the custom "Tags" property sheet tab from the registry using regasm.exe
        /// /unregister. This is the preferred way to remove the tab when the shell extension
        /// DLL is available, because it requests UAC elevation if the calling process is not
        /// already elevated.
        /// </summary>
        /// <param name="dllPath">Path to the <c>ZeeMadArchivist.ShellExtension.dll</c> assembly.</param>
        /// <returns><c>true</c> if unregistration was started successfully; otherwise <c>false</c>.</returns>
        public static bool RemoveTagsPropertyPage(string dllPath)
        {
            if (string.IsNullOrWhiteSpace(dllPath))
            {
                Trace.TraceError("Tags property page DLL path is empty.");
                return false;
            }

            return RemoveTagsPropertyPageCore(dllPath);
        }

        private static bool RemoveTagsPropertyPageCore(string? dllPath)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(dllPath))
                {
                    var fullPath = Path.GetFullPath(dllPath);
                    if (!File.Exists(fullPath))
                    {
                        Trace.TraceError($"Tags property page DLL not found: {fullPath}");
                        return false;
                    }

                    var regasmPath = GetRegasmPath(fullPath);
                    if (regasmPath is null)
                    {
                        Trace.TraceError("Could not determine the correct regasm.exe for the tags property page DLL.");
                        return false;
                    }

                    var startInfo = new ProcessStartInfo
                    {
                        FileName = regasmPath,
                        Arguments = $"\"{fullPath}\" /unregister",
                        UseShellExecute = true,
                        Verb = "runas",
                        WindowStyle = ProcessWindowStyle.Normal,
                    };

                    using var process = Process.Start(startInfo);
                    if (process is null)
                    {
                        Trace.TraceError("Failed to start regasm.exe for Tags property page unregistration.");
                        return false;
                    }

                    process.WaitForExit();
                    return process.ExitCode == 0;
                }

                Registry.ClassesRoot.DeleteSubKeyTree(@$"CLSID\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                Registry.ClassesRoot.DeleteSubKeyTree(@$"*\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                Registry.ClassesRoot.DeleteSubKeyTree(@$"Directory\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                Registry.ClassesRoot.DeleteSubKeyTree(@$"Drive\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}", throwOnMissingSubKey: false);
                return true;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }

        /// <summary>
        /// Registers the custom "Tags" property sheet tab using regasm.exe so it appears on the
        /// properties dialog of any file, folder, or drive.
        /// </summary>
        /// <param name="dllPath">Path to the <c>ZeeMadArchivist.ShellExtension.dll</c> assembly.</param>
        /// <returns><c>true</c> if registration started successfully; otherwise <c>false</c>.</returns>
        /// <remarks>
        /// This method requires administrator privileges. If the calling process is not elevated,
        /// regasm.exe is launched with the "runas" verb to request UAC elevation.
        /// </remarks>
        public static bool RegisterTagsPropertyPage(string dllPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dllPath))
                {
                    Trace.TraceError("Tags property page DLL path is empty.");
                    return false;
                }

                var fullPath = Path.GetFullPath(dllPath);
                if (!File.Exists(fullPath))
                {
                    Trace.TraceError($"Tags property page DLL not found: {fullPath}");
                    return false;
                }

                var regasmPath = GetRegasmPath(fullPath);
                if (regasmPath is null)
                {
                    Trace.TraceError("Could not determine the correct regasm.exe for the tags property page DLL.");
                    return false;
                }

                var startInfo = new ProcessStartInfo
                {
                    FileName = regasmPath,
                    Arguments = $"\"{fullPath}\" /codebase",
                    UseShellExecute = true,
                    Verb = "runas",
                    WindowStyle = ProcessWindowStyle.Normal,
                };

                using var process = Process.Start(startInfo);
                if (process is null)
                {
                    Trace.TraceError("Failed to start regasm.exe for Tags property page registration.");
                    return false;
                }

                process.WaitForExit();
                return process.ExitCode == 0;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }

        private static string? GetRegasmPath(string dllPath)
        {
            var architecture = GetDllArchitecture(dllPath);
            return architecture switch
            {
                ImageFileMachine.AMD64 => @"C:\Windows\Microsoft.NET\Framework64\v4.0.30319\regasm.exe",
                ImageFileMachine.I386 => @"C:\Windows\Microsoft.NET\Framework\v4.0.30319\regasm.exe",
                _ => null,
            };
        }

        private static ImageFileMachine GetDllArchitecture(string dllPath)
        {
            try
            {
                using var stream = File.OpenRead(dllPath);
                using var reader = new BinaryReader(stream);

                stream.Position = 0x3C;
                var peOffset = reader.ReadInt32();
                stream.Position = peOffset;

                var signature = reader.ReadUInt32();
                if (signature != 0x00004550) // "PE\0\0"
                {
                    return ImageFileMachine.Unknown;
                }

                var machine = reader.ReadUInt16();
                return (ImageFileMachine)machine;
            }
            catch (Exception ex)
            {
                Trace.TraceError($"Failed to read DLL architecture: {ex}");
                return ImageFileMachine.Unknown;
            }
        }

        /// <summary>
        /// Checks whether the Tags property page is registered in the registry.
        /// </summary>
        /// <returns><c>true</c> if all expected registry keys are present; otherwise <c>false</c>.</returns>
        public static bool IsTagsPropertyPageRegistered()
        {
            try
            {
                using var clsid = Registry.ClassesRoot.OpenSubKey(@$"CLSID\{TagsPropertySheetClsid}");
                using var fileHandler = Registry.ClassesRoot.OpenSubKey(@$"*\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}");
                using var directoryHandler = Registry.ClassesRoot.OpenSubKey(@$"Directory\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}");
                using var driveHandler = Registry.ClassesRoot.OpenSubKey(@$"Drive\shellex\PropertySheetHandlers\{TagsPropertySheetClsid}");

                return clsid is not null && fileHandler is not null && directoryHandler is not null && driveHandler is not null;
            }
            catch (Exception ex)
            {
                Trace.TraceError(ex.ToString());
                return false;
            }
        }

        private enum ImageFileMachine : ushort
        {
            Unknown = 0,
            I386 = 0x014c,
            AMD64 = 0x8664,
        }
    }
}
