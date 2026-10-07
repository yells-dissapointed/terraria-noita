#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using Terraria;
using Terraria.ModLoader;
using terrarianoita.Core;
using Microsoft.Xna.Framework;

namespace terrarianoita.Common;

public sealed class AdapterSystem : ModSystem
{
    private readonly HashSet<Lua51Runtime> runtimes = new();
    public override void OnWorldLoad()
    {
        if (!Main.dedServ) Main.NewText(BuildIdentity.Label(Mod), Color.LightPink);
    }
    public Lua51Runtime CreateRuntime()
    {
        string root = ModContent.GetInstance<NoitaConfig>().ExtractedDataRoot;
        if (string.IsNullOrWhiteSpace(root))
            throw new InvalidOperationException("Set the extracted Noita folder in this mod's settings first.");
        string name = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "terrarianoita_lua51.dll" :
            RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "libterrarianoita_lua51.dylib" : "libterrarianoita_lua51.so";
        byte[] bytes;
        try { bytes = Mod.GetFileBytes("Native/" + name); }
        catch (Exception e) { throw new InvalidOperationException("The mod is missing its native Lua runtime. Build the platform library and rebuild the mod.", e); }
        string hash = Convert.ToHexString(SHA256.HashData(bytes));
        string directory = Path.Combine(Main.SavePath, "terrarianoita", "native", hash);
        Directory.CreateDirectory(directory);
        string path = Path.Combine(directory, name);
        if (!File.Exists(path)) File.WriteAllBytes(path, bytes);
        string bridge = Encoding.UTF8.GetString(Mod.GetFileBytes("Compatibility/bridge.lua"));
        var runtime = new Lua51Runtime(path, root, bridge);
        runtimes.Add(runtime);
        return runtime;
    }
    public void Release(Lua51Runtime runtime)
    {
        runtimes.Remove(runtime);
        runtime.Dispose();
    }
    private void DisposeAll()
    {
        foreach (var runtime in runtimes) runtime.Dispose();
        runtimes.Clear();
    }
    public override void OnWorldUnload() => DisposeAll();
    public override void Unload() => DisposeAll();
}
