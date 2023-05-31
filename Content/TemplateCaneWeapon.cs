using Microsoft.Xna.Framework;
using OracleClass.Content.DamageClasses;
using OracleClass.Helpers;
using OracleClass.Helpers.Abstracts;
using Terraria;
using Terraria.DataStructures;
using Terraria.ID;
using Terraria.ModLoader;

namespace OracleClass.Content {
    public class TemplateCaneWeapon : OracleWeapon {
        public override void SetDefaults() {
            // Item stats
            Item.width = 42;
            Item.height = 40;
            Item.rare = ItemRarityID.Pink;
            Item.value = Item.sellPrice(gold: 1);

            // Use stats
            Item.useTime = 8;
            Item.useAnimation = 8;
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
            BaseSoulCapacity = 60;
            SoulRecoveryFrames = 5 * 60;
            HandleSoulConsumption = false;

            base.SetDefaults();
        }

        // Using a static property to refer to this items shot projectile, you don't have to do this but it might make code a little more readable
        private static int HeldProjectile => ModContent.ProjectileType<TemplateCaneWeapon_HeldProj>();

        public override bool Shoot(Player player, EntitySource_ItemUse_WithAmmo source, Vector2 position, Vector2 velocity, int type, int damage, float knockback) {
            // This just makes sure the projectile spawns in the correct place
            Vector2 spawnPos = player.RotatedRelativePoint(player.MountedCenter, true);
            Projectile.NewProjectile(source, player.Center, spawnPos, HeldProjectile, damage, knockback, player.whoAmI);

            return false;
        }
    }

    // Held Projectiles should be in the same .cs and namespace as the items that use them, appended with "_HeldProj"
    public class TemplateCaneWeapon_HeldProj : HeldProjectile {
        public override void SetDefaults() {
            Projectile.width = 42;
            Projectile.height = 40;
            Projectile.aiStyle = -1;
            Projectile.friendly = true;
            Projectile.penetrate = -1;
            Projectile.tileCollide = false;
            Projectile.hide = true;
            Projectile.DamageType = ModContent.GetInstance<OracleDamageClass>();
            Projectile.ignoreWater = true;

            HoldOutOffset = 20f;
            RotationOffset = MathHelper.PiOver4;

            base.SetDefaults();
        }

        // Treat this as a "UseItem" hook, we just manually spawn our projectile
        // You'll usually only need to use this for cane weapons
        public override void OnUseItem() {
            Vector2 toMouse = Main.MouseWorld - Projectile.Center;
            toMouse.Normalize();
            Projectile.NewProjectile(Projectile.GetSource_ItemUse(Owner.HeldItem), Projectile.Center, toMouse * 10f, ProjectileID.WoodenArrowFriendly, Projectile.damage, Projectile.knockBack, Projectile.owner);

            base.OnUseItem();
        }

        public override void AI() {
            base.AI();
            // You might have noticed we've called base in all of these overrides
            // When using our own abstract classes it's extremely important to make sure to call base, so the class we inherit can do anything it might want to
            // Even when we're overriding a method the abstract class doesn't, it might be updated in the future to override that method and would break everything we've written so far
            // In this case, HeldProjectile.AI handles a lot of important stuff that we do not want to stop
            // In the case that you do want to stop it happening, make a comment explicitly saying why you aren't calling base

            // Probs just want to use this formatting for most cane weapons
            // Every X frames we want to do something, and we should check if its equal to X - 1 so it has 20 frames at the start of base stats
            if (AI_FrameCount % 60 == 59) {
                Projectile.damage = (int)((float)Projectile.damage * 1.5f);
            }
        }
    }
}
