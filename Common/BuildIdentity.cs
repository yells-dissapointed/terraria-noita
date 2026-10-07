#nullable enable
using System.Reflection;
using Terraria.ModLoader;
using terrarianoita.Core;

namespace terrarianoita.Common;

public static class BuildIdentity
{
    public static string Label(Mod mod) => $"Terraria Noita v{mod.Version} | {BuildStamp.Id}";
    public static string LoadedPath(Mod mod)
    {
        // Mod.File is internal in the current tModLoader build; read metadata only.
        var file = typeof(Mod).GetProperty("File", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)?.GetValue(mod);
        return file?.GetType().GetField("path")?.GetValue(file) as string ?? "Loaded path unavailable; inspect client.log";
    }
}
