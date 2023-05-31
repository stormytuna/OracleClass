using Terraria;
using Terraria.ModLoader;

namespace OracleClass.Common.Players {
    public class OraclePlayer : ModPlayer {
        /// <summary>The multiplier to base soul capacity from the players accessories and armor</summary>
        public float SoulCapacityMultiplier { get; set; }

        public override void ResetEffects() {
            SoulCapacityMultiplier = 1f;
        }

        /// <summary>
        /// This goes through your inventory and reduces the soul capacity of each weapon with that type. Call this whenever you reduce an items soul capacity<para />
        /// Weapons will exhaust from this if it reduces them to 0
        /// </summary>
        public void ReduceSoulOnAllWeapons(Item itemToIgnore) {
            foreach (var item in Player.inventory) {
                if (item.type == itemToIgnore.type && item != itemToIgnore) {
                    var oracleWeapon = item.GetOracleWeapon();
                    oracleWeapon.CurSoulCapacity--;
                    oracleWeapon.SoulRecoveryCooldown = 5 * 60;
                    if (oracleWeapon.CurSoulCapacity <= 0) {
                        oracleWeapon.Exhausted = true;
                    }
                }
            }
        }

        // This just sets all our oracle weapons to max charge when we load in
        public override void OnEnterWorld() {
            foreach (var item in Player.inventory) {
                if (item.TryGetOracleWeapon(out var ow)) {
                    ow.CurSoulCapacity = ow.MaxSoulCapacity;
                }
            }
        }
    }
}
