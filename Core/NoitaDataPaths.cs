#nullable enable
using System;
using System.IO;

namespace terrarianoita.Core;

public static class NoitaDataPaths
{
    public static string ResolveRoot(string configuredPath)
    {
        if (string.IsNullOrWhiteSpace(configuredPath))
            throw new InvalidOperationException("Set the extracted Noita folder in this mod's settings first.");
        string path = configuredPath.Trim();
        if (path.Length >= 2 && path[0] == '"' && path[^1] == '"')
            path = path[1..^1];
        path = Path.GetFullPath(Environment.ExpandEnvironmentVariables(path));
        if (File.Exists(path))
        {
            if (!Path.GetFileName(path).Equals("gun.lua", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Select an extracted Noita folder or its gun.lua file.");
            path = Path.GetDirectoryName(path)!;
        }
        if (!Directory.Exists(path))
            throw new DirectoryNotFoundException("The configured Noita folder does not exist: " + path);

        // Accept the extracted root, data, scripts, gun, or gun.lua itself.
        DirectoryInfo? directory = new DirectoryInfo(path);
        for (int level = 0; level <= 3 && directory != null; level++, directory = directory.Parent)
        {
            string root = directory.FullName;
            if (!File.Exists(Path.Combine(root, "data", "scripts", "gun", "gun.lua"))) continue;
            foreach (string relative in Lua51Runtime.SourcePaths)
            {
                string file = Path.Combine(root, relative);
                if (!File.Exists(file))
                    throw new FileNotFoundException("The extracted Noita data is incomplete. Missing file: " + file, file);
            }
            return root;
        }
        throw new FileNotFoundException("No extracted gun.lua found for: " + path +
            ". Select the extracted root, data folder, gun folder, or gun.lua file. " +
            "If you only have data.wak, extract it first.");
    }
}
