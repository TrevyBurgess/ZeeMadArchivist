using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CyberFeedForward.TheMadArchivist.ViewModels;

public static class DriveLetterHelper
{
    public static IEnumerable<char> GetUnusedDriveLetters()
    {
        var used = DriveInfo.GetDrives()
            .Select(d => char.ToUpperInvariant(d.Name[0]))
            .Where(c => c is >= 'A' and <= 'Z');

        return GetUnusedDriveLetters(used);
    }

    public static IEnumerable<char> GetUnusedDriveLetters(IEnumerable<char> usedDriveLetters, char startLetter = 'D')
    {
        ArgumentNullException.ThrowIfNull(usedDriveLetters);

        var used = new HashSet<char>(usedDriveLetters
            .Select(char.ToUpperInvariant)
            .Where(c => c is >= 'A' and <= 'Z'));

        var start = char.ToUpperInvariant(startLetter);
        if (start is < 'A' or > 'Z')
        {
            throw new ArgumentOutOfRangeException(nameof(startLetter));
        }

        for (var c = 'Z'; c >= start; c--)
        {
            if (!used.Contains(c))
            {
                yield return c;
            }
        }
    }

    public static char? ParseDriveLetter(string? value)
    {
        var s = value?.Trim();
        if (string.IsNullOrEmpty(s)) return null;
        var c = char.ToUpperInvariant(s[0]);
        return c is >= 'A' and <= 'Z' ? c : null;
    }
}
