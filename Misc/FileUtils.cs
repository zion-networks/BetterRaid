using System;
using System.IO;

namespace BetterRaid.Misc;

internal static class FileUtils
{
    public static void EnsureDirectoryExists(string directoryPath)
    {
        if (string.IsNullOrWhiteSpace(directoryPath))
            throw new ArgumentException("Directory path cannot be null or empty.", nameof(directoryPath));

        if (Path.HasExtension(directoryPath))
            directoryPath = Path.GetDirectoryName(directoryPath) ?? throw new ArgumentNullException(nameof(directoryPath));
        
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }
    }
}
