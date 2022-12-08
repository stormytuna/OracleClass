using Terraria;

namespace OracleClass.Content.Prefixes {
    public class Uncanny : OraclePrefix {
        public override bool CanRoll(Item item) => item.IsOracleWeapon();

        public override void SetStats(ref float damageMult, ref float knockbackMult, ref float useTimeMult, ref float scaleMult, ref float shootSpeedMult, ref float manaMult, ref int critBonus) {
            base.SetStats(ref damageMult, ref knockbackMult, ref useTimeMult, ref scaleMult, ref shootSpeedMult, ref manaMult, ref critBonus);

            damageMult = 0.9f;
            useTimeMult = 0.85f;
            critBonus = 3;
        }

        public override void ModifyValue(ref float valueMult) {
            valueMult = 1f * 0.9f * (2f - 0.85f) * (1f + 3 * 0.02f);
        }

        public override void Apply(Item item) {
            base.Apply(item);
        }
    }
}
