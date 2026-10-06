#nullable enable
using System.ComponentModel;
using Terraria.ModLoader.Config;

namespace terrarianoita.Common;

public sealed class NoitaConfig : ModConfig
{
    public override ConfigScope Mode => ConfigScope.ClientSide;
    [DefaultValue("")]
    [ReloadRequired]
    public string ExtractedDataRoot = "";
}
