using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.IO;

namespace CyberFeedForward.TheMadArchivist.Services;

public sealed class StartupSettingsService(
    Func<string>? getExecutablePath = null,
    Func<(string? value, bool exists)>? tryReadRunValue = null,
    Action<string>? writeRunValue = null,
    Action? deleteRunValue = null)
{
    private const string RunKeyPath = "Software\\Microsoft\\Windows\\CurrentVersion\\Run";
    private const string RunValueName = "ZeeMadArchivist";

    private readonly Func<string> _getExecutablePath = getExecutablePath ?? GetDefaultExecutablePath;
    private readonly Func<(string? value, bool exists)> _tryReadRunValue = tryReadRunValue ?? TryReadRunValue;
    private readonly Action<string> _writeRunValue = writeRunValue ?? WriteRunValue;
    private readonly Action _deleteRunValue = deleteRunValue ?? DeleteRunValue;

    public bool MinimizeToTray()
    {
        try
        {
            var (value, exists) = _tryReadRunValue();
            if (!exists)
            {
                return false;
            }

            var exePath = _getExecutablePath();
            if (string.IsNullOrWhiteSpace(exePath))
            {
                return false;
            }

            var normalizedValue = (value ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(normalizedValue))
            {
                return false;
            }

            var normalizedExe = exePath.Trim();
            return normalizedValue.Contains(normalizedExe, StringComparison.OrdinalIgnoreCase);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            throw;
        }
    }

    public void SetStartupEnabled(bool enabled)
    {
        try
        {
            if (!enabled)
            {
                _deleteRunValue();
                return;
            }

            var exePath = _getExecutablePath();
            if (string.IsNullOrWhiteSpace(exePath))
            {
                throw new InvalidOperationException("Unable to determine the application executable path.");
            }

            var quotedExe = exePath.Contains(' ', StringComparison.Ordinal) ? $"\"{exePath}\"" : exePath;
            _writeRunValue(quotedExe);
        }
        catch (Exception ex)
        {
            Trace.TraceError(ex.ToString());
            throw;
        }
    }

    private static string GetDefaultExecutablePath()
    {
        var exePath = Process.GetCurrentProcess().MainModule?.FileName;
        if (string.IsNullOrWhiteSpace(exePath))
        {
            exePath = Environment.ProcessPath;
        }

        if (string.IsNullOrWhiteSpace(exePath))
        {
            throw new InvalidOperationException("Unable to determine the application executable path.");
        }

        return Path.GetFullPath(exePath);
    }

    private static (string? value, bool exists) TryReadRunValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: false);
        if (key is null)
        {
            return (null, false);
        }

        var value = key.GetValue(RunValueName) as string;
        return (value, value is not null);
    }

    private static void WriteRunValue(string value)
    {
        using var key = Registry.CurrentUser.CreateSubKey(RunKeyPath, writable: true) ?? throw new InvalidOperationException("Unable to open the Windows startup registry key.");
        
        key.SetValue(RunValueName, value, RegistryValueKind.String);
    }

    private static void DeleteRunValue()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, writable: true);
        if (key is null)
        {
            return;
        }

        if (Array.Exists(key.GetValueNames(), n => string.Equals(n, RunValueName, StringComparison.OrdinalIgnoreCase)))
        {
            key.DeleteValue(RunValueName, throwOnMissingValue: false);
        }
    }
}
