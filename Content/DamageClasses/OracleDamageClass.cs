using Terraria.ModLoader;

namespace OracleClass.Content.DamageClasses;

// For a more indepth explanation of all the stuff here see example damage class
// https://github.com/tModLoader/tModLoader/blob/daa239205ab69743d44f7bb64c9cd3e080024a51/ExampleMod/Content/DamageClasses/ExampleDamageClass.cs
public class OracleDamageClass : DamageClass
{
	public static OracleDamageClass Instance => ModContent.GetInstance<OracleDamageClass>();

	public override StatInheritanceData GetModifierInheritance(DamageClass damageClass) {
		if (damageClass == Generic) {
			return StatInheritanceData.Full;
		}

		return new StatInheritanceData(1f, 1f, 1f, 1f, 1f);
	}

	public override bool UseStandardCritCalcs => true;
}