using Microsoft.Xna.Framework;
using OracleClass.Content.DamageClasses;
using OracleClass.Helpers;
using OracleClass.Helpers.Abstracts;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace OracleClass.Content {
    public class TemplateFocusWeapon : OracleWeapon {
        public override void SetDefaults() {
            // Item stats
            Item.width = 18;
            Item.height = 18;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 1);

            // Use stats
            Item.useTime = 10;
            Item.useAnimation = 10;
            Item.useStyle = ItemUseStyleID.Shoot;
            Item.autoReuse = true;
            Item.noUseGraphic = true; // Important for held projectile weapons, prevents our items sprite from showing

            // Weapon stats
            Item.DamageType = OracleDamageClass.Instance;
            Item.damage = 100;
            Item.knockBack = 5f;
            Item.noMelee = true;
            Item.shoot = HeldProjectile; // Very important, Item.shoot should be the held projectile it shoots, not the projectiles that actually hurt enemies
            Item.channel = true; // Also very important, without this held projectile weapons do not work

            // Oracle weapon stats
            OracleType = Enums.OracleWeaponType.Cane;
            BaseSoulCapacity = 30;
            SoulRecoveryFrames = 5 * 60;
            HandleSoulConsumption = false;

            base.SetDefaults();
        }

        // Using a static property to refer to this items shot projectile, you don't have to do this but it might make code a little more readable
        private static int HeldProjectile => ModContent.ProjectileType<TemplateFocusWeapon_HeldProj>();

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            // This just makes sure the projectile spawns in the correct place
            Vector2 spawnPos = player.RotatedRelativePoint(player.MountedCenter, true);
            Projectile.NewProjectile(source, player.Center, spawnPos, HeldProjectile, damage, knockback, player.whoAmI);

            return false;
        }
    }

    // Held Projectiles should be in the same .cs and namespace as the items that use them, appended with "_HeldProj"
    public class TemplateFocusWeapon_HeldProj : HeldProjectile {
        public override void SetDefaults() {
            Projectile.width = 18;
            Projectile.height = 18;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.DamageType = ModContent.GetInstance<OracleDamageClass>();
            Projectile.ignoreWater = true;

            HoldOutOffset = 10f;
            RotationOffset = MathHelper.PiOver4;

            base.SetDefaults();
        }

        public override void OnFinishedCharging(int totalSoulConsumed) {
            for (int i = 0; i < totalSoulConsumed; i++) {
                Vector2 velocity = Vector2.UnitY.RotatedByRandom(MathHelper.PiOver2) * -10f;
                Projectile.NewProjectile(Projectile.GetSource_ItemUse(Owner.HeldItem), Projectile.Center, velocity, ProjectileID.WoodenArrowFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);
            }

            base.OnFinishedCharging(totalSoulConsumed);
        }
    }
}
