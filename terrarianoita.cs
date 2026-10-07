using Terraria.ModLoader;
using terrarianoita.Common;

namespace terrarianoita
{
	public class terrarianoita : Mod
	{
		public override void Load() => Logger.Info(BuildIdentity.Label(this) + " | loaded from " + BuildIdentity.LoadedPath(this));
	}
}
