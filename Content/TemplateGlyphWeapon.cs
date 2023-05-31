using OracleClass.Content.DamageClasses;
using OracleClass.Helpers;
using OracleClass.Helpers.Abstracts;
using Terraria;
using Terraria.ID;

namespace OracleClass.Content {
    // Glyph weapons usually won't need a held projectile
    public class TemplateGlyphWeapon : OracleWeapon {
        public override void SetDefaults() {
            // Item stats
            Item.width = 24;
            Item.height = 32;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 1);

            // Use stats
            Item.useTime = 15;
            Item.useAnimation = 15;
            Item.useStyle = ItemUseStyleID.Shoot;

            // Weapon stats
            Item.DamageType = OracleDamageClass.Instance;
            Item.damage = 100;
            Item.knockBack = 5f;
            Item.noMelee = true;
            Item.shoot = ProjectileID.IceSickle;

            // Oracle weapon stats
            OracleType = Enums.OracleWeaponType.Glyph;
            BaseSoulCapacity = 10;
            // Use either of these formats to write frame counts
            SoulRecoveryFrames = 5 * 60; // No need to comment as it's obviously 5 seconds
            // SoulRecoveryFrames = 270; // 4.5 secs   // Should comment how many seconds it is as it isn't immediately obvious
        }
    }
}
