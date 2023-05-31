using Microsoft.Xna.Framework;
using OracleClass.Content.DamageClasses;
using Terraria.ModLoader;

namespace OracleClass.Common.Systems;

public class ModCalls : ModSystem
{
	public override void PostSetupContent() {
		// Mod calls
		// This adds a colour to our damage type
		if (ModLoader.TryGetMod("ColoredDamageTypes", out Mod coloredDamageTypes)) {
			coloredDamageTypes.Call("AddDamageType", ModContent.GetInstance<OracleDamageClass>(), new Color(162, 32, 88), new Color(162, 32, 88), new Color(112, 22, 60));
		}

		base.PostSetupContent();
	}
}