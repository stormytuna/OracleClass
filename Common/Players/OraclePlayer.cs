using Terraria.ModLoader;

namespace OracleClass.Common.Players {
    public class OraclePlayer : ModPlayer {
        /// <summary>The multiplier to base soul capacity from the players accessories and armor</summary>
        public float SoulCapacityMultiplier { get; set; }

        public override void ResetEffects() {
            SoulCapacityMultiplier = 1f;
        }
    }
}
