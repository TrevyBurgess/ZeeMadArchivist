using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Text;

namespace CyberFeedForward.TheMadArchivist.ShellExtension;

/// <summary>
/// Provides helper methods for interacting with mapped drives from the shell extension.
/// </summary>
public static partial class DriveTools
{
    /// <summary>
    /// Renames a mapped drive by setting its volume label.
    /// </summary>
    /// <param name="driveLetter">The drive letter to rename (for example, 'Z').</param>
    /// <param name="newName">The new display name for the drive.</param>
    /// <param name="errorMessage">When the method returns <c>false</c>, contains a description of the error.</param>
    /// <returns><c>true</c> if the drive was renamed; otherwise <c>false</c>.</returns>
    public static bool RenameMappedDrive(char driveLetter, string newName, out string errorMessage)
    {
        errorMessage = string.Empty;

        try
        {
            if (!char.IsLetter(driveLetter))
            {
                errorMessage = $"Invalid drive letter '{driveLetter}'.";
                return false;
            }

            if (string.IsNullOrWhiteSpace(newName))
            {
                errorMessage = "The new drive name cannot be empty.";
                return false;
            }

            var rootPath = char.ToUpperInvariant(driveLetter) + @":\";
#if NET8_0_OR_GREATER
            if (SetVolumeLabelNative(rootPath, newName) == 0)
            {
                var lastError = Marshal.GetLastWin32Error();
                errorMessage = new Win32Exception(lastError).Message;
                return false;
            }
#else
            if (!SetVolumeLabel(rootPath, newName))
            {
                var lastError = Marshal.GetLastWin32Error();
                errorMessage = new Win32Exception(lastError).Message;
                return false;
            }
#endif
            return true;
        }
        catch (Exception ex)
        {
            errorMessage = ex.Message;
            System.Diagnostics.Trace.TraceError($"RenameMappedDrive failed: {ex}");
            return false;
        }
    }

#if NET8_0_OR_GREATER
    [LibraryImport("kernel32.dll", StringMarshalling = StringMarshalling.Utf16, SetLastError = true, EntryPoint = "SetVolumeLabelW")]
    private static partial int SetVolumeLabelNative(string lpRootPathName, string lpVolumeName);
#else
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool SetVolumeLabel(string lpRootPathName, string lpVolumeName);
#endif
}
