using System;
using System.Collections.Generic;
using System.IO;

namespace TRM.Core;

public static class WorkspaceFileLocator
{
    public static string GetFilePath(string fileName)
    {
        var candidates = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void AddCandidate(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return;
            }

            string fullPath;
            try
            {
                fullPath = Path.GetFullPath(path);
            }
            catch
            {
                return;
            }

            if (seen.Add(fullPath))
            {
                candidates.Add(fullPath);
            }
        }

        var currentDirectory = Directory.GetCurrentDirectory();
        var baseDirectory = AppContext.BaseDirectory;

        AddCandidate(Path.Combine(currentDirectory, fileName));
        AddCandidate(Path.Combine(currentDirectory, "Data", fileName));
        AddCandidate(Path.Combine(baseDirectory, fileName));
        AddCandidate(Path.Combine(baseDirectory, "Data", fileName));

        AddParentCandidates(currentDirectory, fileName, AddCandidate);
        AddParentCandidates(baseDirectory, fileName, AddCandidate);

        foreach (var candidate in candidates)
        {
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        throw new FileNotFoundException(
            $"Could not locate '{fileName}'. Searched: {string.Join("; ", candidates)}");
    }

    private static void AddParentCandidates(string startDirectory, string fileName, Action<string> addCandidate)
    {
        if (string.IsNullOrWhiteSpace(startDirectory))
        {
            return;
        }

        var directory = new DirectoryInfo(startDirectory);
        while (directory != null)
        {
            addCandidate(Path.Combine(directory.FullName, fileName));
            addCandidate(Path.Combine(directory.FullName, "Data", fileName));
            addCandidate(Path.Combine(directory.FullName, "TRM.Core", "Data", fileName));

            if (File.Exists(Path.Combine(directory.FullName, "TRM_Cosmology.slnx")))
            {
                addCandidate(Path.Combine(directory.FullName, "Data", fileName));
                addCandidate(Path.Combine(directory.FullName, "TRM.Core", "Data", fileName));
            }

            directory = directory.Parent;
        }
    }
}
