using OracleClass.Helpers;
using Terraria;
using Terraria.ModLoader;

namespace OracleClass.Content.Prefixes;

public abstract class OraclePrefix : ModPrefix
{
	public override PrefixCategory Category => PrefixCategory.Custom;

	public override bool CanRoll(Item item) => false;

	public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus) {
		damageMult = 1f;
		knockbackMult = 1f;
		useTimeMult = 1f;
		scaleMult = 1f;
		shootSpeedMult = 1f;
		manaMult = 1f;
		critBonus = 0;
	}

	public override void Apply(Item item) {
		item.GetOracleWeapon().SoulCapacityMultiplierFromPrefix = 1f;
	}
}