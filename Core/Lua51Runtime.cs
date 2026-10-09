#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace terrarianoita.Core;

/// <summary>One native Lua 5.1 state per wand. No reverse P/Invoke callbacks.</summary>
public sealed class Lua51Runtime : IDisposable
{
    public static readonly string[] SourcePaths = {
        "data/scripts/gun/gun.lua", "data/scripts/gun/gun_enums.lua",
        "data/scripts/gun/gunaction_generated.lua", "data/scripts/gun/gun_generated.lua",
        "data/scripts/gun/gunshoteffects_generated.lua", "data/scripts/gun/gun_actions.lua",
        "data/scripts/gun/gun_extra_modifiers.lua", "data/scripts/gun/procedural/gun_action_utils.lua",
        "data/scripts/lib/utilities.lua"
    };
    private IntPtr library, state;
    private readonly int ownerThread = Environment.CurrentManagedThreadId;
    private readonly object stateLock = new();
    private bool configured, faulted;
    public string RuntimeVersion { get; private set; } = "";
    public bool IsDisposed => state == IntPtr.Zero;
    private NewState newState = null!;
    private StateOperation close = null!, openLibraries = null!;
    private LoadBuffer loadBuffer = null!;
    private ProtectedCall protectedCall = null!;
    private LuaToString toString = null!;
    private GetTop getTop = null!;
    private SetTop setTop = null!;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr NewState();
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void StateOperation(IntPtr l);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int LoadBuffer(IntPtr l, byte[] bytes, UIntPtr size, [MarshalAs(UnmanagedType.LPUTF8Str)] string name);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int ProtectedCall(IntPtr l, int args, int results, int handler);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate IntPtr LuaToString(IntPtr l, int index, out UIntPtr size);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate int GetTop(IntPtr l);
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)] private delegate void SetTop(IntPtr l, int top);

    public Lua51Runtime(string libraryPath, string dataRoot, string bridgeSource)
    {
        if (!Environment.Is64BitProcess) throw new PlatformNotSupportedException("The prototype needs a 64-bit host.");
        dataRoot = NoitaDataPaths.ResolveRoot(dataRoot);
        try
        {
            library = NativeLibrary.Load(Path.GetFullPath(libraryPath));
            newState = Export<NewState>("luaL_newstate");
            close = Export<StateOperation>("lua_close");
            openLibraries = Export<StateOperation>("luaL_openlibs");
            loadBuffer = Export<LoadBuffer>("luaL_loadbuffer");
            protectedCall = Export<ProtectedCall>("lua_pcall");
            toString = Export<LuaToString>("lua_tolstring");
            getTop = Export<GetTop>("lua_gettop");
            setTop = Export<SetTop>("lua_settop");
            state = newState();
            if (state == IntPtr.Zero) throw new OutOfMemoryException("Lua state creation failed.");
            openLibraries(state);
            if (Evaluate("return _VERSION") != "Lua 5.1")
                throw new NotSupportedException("The compatibility adapter requires Lua 5.1.");
            RuntimeVersion = Evaluate("return jit and jit.version or _VERSION");
            var sources = new StringBuilder("noita_sources = {\n");
            foreach (string relative in SourcePaths)
            {
                string text = File.ReadAllText(Path.Combine(dataRoot, relative), Encoding.UTF8);
                if (text.Length > 2_000_000) throw new InvalidDataException("Noita source exceeds import limit.");
                sources.Append('[').Append(Quote(relative)).Append("]=").Append(Quote(text.TrimStart('\uFEFF'))).Append(",\n");
            }
            sources.Append("}\n").Append(bridgeSource);
            Execute(sources.ToString());
        }
        catch { Dispose(); throw; }
    }

    private T Export<T>(string name) where T : Delegate =>
        Marshal.GetDelegateForFunctionPointer<T>(NativeLibrary.GetExport(library, name));

    public string[] SpellIds() => Evaluate("local ids = {}; for _, a in ipairs(actions) do ids[#ids+1] = a.id end; table.sort(ids); return table.concat(ids, '\\n')").Split('\n', StringSplitOptions.RemoveEmptyEntries);

    public SpellCard[] SpellCards() => JsonSerializer.Deserialize<SpellCard[]>(Evaluate("return bridge_catalog()"))!;

    public Dictionary<string, JsonElement> DefaultConfiguration() =>
        JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Evaluate("return bridge_defaults()"))!;

    public void Configure(WandConfiguration configuration)
    {
        Check();
        if (configuration.Slots.Count > 64 || configuration.ActionsPerRound < 1 || configuration.ActionsPerRound > 64 ||
            !double.IsFinite(configuration.CastDelay) || !double.IsFinite(configuration.ReloadTime))
            throw new ArgumentException("Invalid wand configuration.");
        var slots = configuration.Slots.Select(s => new { id = s.Id, uses_remaining = s.UsesRemaining, identified = s.Identified }).ToArray();
        Execute("bridge_configure(" + LuaLiteral(JsonSerializer.SerializeToElement(new {
            slots, cast_delay = configuration.CastDelay, reload_time = configuration.ReloadTime,
            actions_per_round = configuration.ActionsPerRound, shuffle = configuration.Shuffle
        })) + ")");
        configured = true;
    }

    public CastPlan Cast(double mana, IReadOnlyList<string>? permanent = null, CastHostContext? context = null)
    {
        Check();
        if (!configured) throw new InvalidOperationException("Configure the wand before casting.");
        if (!double.IsFinite(mana) || mana < 0) throw new ArgumentOutOfRangeException(nameof(mana));
        if (context != null) Execute("bridge_context(" + LuaLiteral(JsonSerializer.SerializeToElement(context)) + ")");
        string request = LuaLiteral(JsonSerializer.SerializeToElement(new { mana, permanent = permanent ?? Array.Empty<string>() }));
        try { return CastPlan.Parse(Evaluate("return bridge_cast(" + request + ")")); }
        catch { faulted = true; throw; }
    }

    // UTF-8 decimal escapes are valid in Lua 5.1; JSON's \u escapes are not.
    private static string Quote(string text)
    {
        var result = new StringBuilder("\"");
        foreach (byte b in Encoding.UTF8.GetBytes(text))
        {
            if (b is >= 32 and <= 126 && b != (byte)'"' && b != (byte)'\\') result.Append((char)b);
            else result.Append('\\').Append(b.ToString("D3", CultureInfo.InvariantCulture));
        }
        return result.Append('"').ToString();
    }
    private static string LuaLiteral(JsonElement value) => value.ValueKind switch {
        JsonValueKind.String => Quote(value.GetString() ?? ""),
        JsonValueKind.Number => value.GetRawText(),
        JsonValueKind.True => "true", JsonValueKind.False => "false", JsonValueKind.Null => "nil",
        JsonValueKind.Array => "{" + string.Join(",", value.EnumerateArray().Select(LuaLiteral)) + "}",
        JsonValueKind.Object => "{" + string.Join(",", value.EnumerateObject().Select(p => "[" + Quote(p.Name) + "]=" + LuaLiteral(p.Value))) + "}",
        _ => throw new ArgumentException("Unsupported Lua input")
    };

    public void Execute(string source) => Run(source, false);
    public string Evaluate(string source) => Run(source, true);
    private string Run(string source, bool result)
    {
        lock (stateLock) return RunLocked(source, result);
    }
    private string RunLocked(string source, bool result)
    {
        Check();
        int top = getTop(state);
        try
        {
            byte[] bytes = Encoding.UTF8.GetBytes(source);
            int status = loadBuffer(state, bytes, (UIntPtr)bytes.Length, "@terrarianoita-host");
            if (status == 0) status = protectedCall(state, 0, result ? 1 : 0, 0);
            if (status != 0)
            {
                faulted = true;
                throw new InvalidOperationException("Noita Lua: " + ReadString(-1));
            }
            return result ? ReadString(-1) : "";
        }
        finally { setTop(state, top); }
    }
    private string ReadString(int index)
    {
        IntPtr pointer = toString(state, index, out var length);
        ulong size = length.ToUInt64();
        if (pointer == IntPtr.Zero) throw new InvalidOperationException("Lua did not return a string.");
        if (size > 4_000_000) throw new InvalidOperationException("Lua output exceeds host limit.");
        byte[] bytes = new byte[(int)size];
        Marshal.Copy(pointer, bytes, 0, bytes.Length);
        return Encoding.UTF8.GetString(bytes);
    }
    private void Check()
    {
        if (Environment.CurrentManagedThreadId != ownerThread) throw new InvalidOperationException("Lua wand states are bound to their creating thread.");
        if (state == IntPtr.Zero) throw new ObjectDisposedException(nameof(Lua51Runtime));
        if (faulted) throw new InvalidOperationException("This wand runtime failed; dispose and recreate it.");
    }
    public void Dispose()
    {
        // Mod unloading may run on a loader thread. Serialize it with active calls.
        lock (stateLock)
        {
            if (state != IntPtr.Zero) { close(state); state = IntPtr.Zero; }
            if (library != IntPtr.Zero) { NativeLibrary.Free(library); library = IntPtr.Zero; }
        }
    }
}
