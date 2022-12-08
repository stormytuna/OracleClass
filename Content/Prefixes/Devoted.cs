using Terraria;

namespace OracleClass.Content.Prefixes {
    public class Devoted : OraclePrefix {
        public override bool CanRoll(Item item) => item.IsOracleWeapon();

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus) {
            base.SetStats(ref damageMult, ref knockbackMult, ref useTimeMult, ref scaleMult, ref shootSpeedMult, ref manaMult, ref critBonus);

            damageMult = 1.05f;
        }

        public override void ModifyValue(ref float valueMult) {
            valueMult = 1f * 1.05f * 1.15f;
        }

        public override void Apply(Item item) {
            base.Apply(item);

            item.GetOracleWeapon().SoulCapacityMultiplierFromPrefix = 1.15f;
        }
    }
}
